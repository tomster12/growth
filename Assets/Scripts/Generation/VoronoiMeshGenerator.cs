
using GK;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GK.VoronoiClipper;


public class VoronoiMeshGenerator : MonoBehaviour, IGenerator
{
    [Serializable]
    public class MeshSiteVertex
    {
        public VertexType type;
        public int vertexUID = -1;
        public int intersectionFromUID = -1, intersectionToUID = -1;

        public MeshSiteVertex(ClippedVertex clippedVertex)
        {
            this.type = clippedVertex.type;
            this.vertexUID = clippedVertex.vertexUID;
            this.intersectionFromUID = clippedVertex.intersectionFromUID;
            this.intersectionToUID = clippedVertex.intersectionToUID;
        }
    }

    [Serializable]
    public class MeshSiteEdge
    {
        public int siteIndex, siteFromVertexI = -1, siteToVertexI = -1;
        public bool isOutside = false;
        public int neighbouringSiteIndex = -1;
        public int neighbouringEdgeIndex = -1;

        public MeshSiteEdge(int siteIndex, int siteFromVertexI, int siteToVertexI)
        {
            this.siteIndex = siteIndex;
            this.siteFromVertexI = siteFromVertexI;
            this.siteToVertexI = siteToVertexI;
        }
    }

    [Serializable]
    public class MeshSite
    {
        public MeshSiteVertex[] vertices;
        public MeshSiteEdge[] edges;
        public int[] meshVerticesI;
        public int meshCentroidI = -1;

        public int siteIndex = -1;
        public bool isOutside = false;
        public HashSet<int> neighbouringSites;
    }


    [Header("Gizmos")]
    [SerializeField] private bool showGizmoSeedCentroids = false;
    [SerializeField] private bool showGizmoDelauneyMain = false;
    [SerializeField] private bool showGizmoDelauneyCentres = false;
    [SerializeField] private bool showGizmoVoronoi = false;
    [SerializeField] private bool showGizmoClipped = false;
    [SerializeField] private bool showGizmoMesh = false;

    [Header("Parameters")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private int seedCount;
    [SerializeField] private float seedMinDistance;
    [SerializeField] private int pipelineMaxTries = 10;
    [SerializeField] private int seedMaxTries = 50;
    [SerializeField] private bool clearInternal = true;

    public MeshSite[] meshSites { get; private set; }
    public Mesh mesh { get; private set; }
    public bool isGenerated { get; private set; } = false;

    private Vector2[] _voronoiSeedSites;
    private VoronoiCalculator _voronoiCalculator;
    private VoronoiDiagram _voronoiDiagram;
    private List<Vector2> _clipperPolygon;
    private VoronoiClipper _voronoiClipper;


    public void Clear()
    {
        ClearInternal();
        isGenerated = false;
        ClearOutput();
    }

    public void ClearInternal()
    {
        // Clear internal variables
        _voronoiSeedSites = null;
        _voronoiCalculator = null;
        _voronoiDiagram = null;
        _clipperPolygon = null;
        _voronoiClipper = null;
    }

    public void ClearOutput()
    {
        // Clear external variables
        meshSites = null;
        mesh = null;
    }

    public void SafeGenerate(MeshFilter meshFilter, PolygonCollider2D outsidePolygon, int seedCount, float seedMinDistance)
    {
        // Keep trying Generate and catch errors
        int tries = 0;
        while (tries < pipelineMaxTries)
        {
            try
            {
                Generate(meshFilter, outsidePolygon, seedCount, seedMinDistance);
                break;
            }
            catch (Exception e)
            {
                tries++;
                Debug.LogException(e);
                Debug.LogWarning("Pipeline threw error " + tries + "/" + pipelineMaxTries + ".");
            }
        }

        // Hit max number of tries
        if (tries == pipelineMaxTries)
        {
            throw new Exception("Voronoi Mesh Pipeline hit maximum number of tries (" + tries + "/" + pipelineMaxTries + ").");
        }
    }

    public void Generate(MeshFilter meshFilter, PolygonCollider2D outsidePolygon, int seedCount, float seedMinDistance)
    {
        this.meshFilter = meshFilter;
        this.outsidePolygon = outsidePolygon;
        this.seedCount = seedCount;
        this.seedMinDistance = seedMinDistance;
        Generate();
    }

    public void Generate()
    {
        Clear();
        Step_GenerateSeeds();
        Step_GenerateVoronoi();
        Step_ClipSites();
        Step_GenerateMeshAndSites();
        Step_ProcessSites();
        if (clearInternal) ClearInternal();
        isGenerated = true;
    }

    private void Step_GenerateSeeds()
    {
        // Generate a set number of seeds
        List<Vector2> voronoiSeedSiteList = new List<Vector2>();
        int tries = 0;
        while (voronoiSeedSiteList.Count < seedCount && tries < seedMaxTries)
        {
            // Generate in a random position
            Vector2 seedWorld = Utility.RandomInPolygon(outsidePolygon, tries != 0);
            Vector2 seedLocal = outsidePolygon.transform.InverseTransformPoint(seedWorld);
            bool isValid = true;
            tries++;

            // Check minimum distance to each other seed
            foreach (Vector2 seed in voronoiSeedSiteList)
            {
                float dst = Vector3.Distance(seed, seedLocal);
                if (dst < seedMinDistance) { isValid = false; break; }
            }

            // If found a valid seed then add to list
            if (isValid)
            {
                tries = 0;
                voronoiSeedSiteList.Add(seedLocal);
                continue;
            }
        }

        // If hit max number of tries
        if (tries == seedMaxTries) Debug.LogWarning("Hit max number of tries (" + voronoiSeedSiteList.Count + "/" + seedCount + ")");

        // Update seed sites
        _voronoiSeedSites = voronoiSeedSiteList.ToArray();
    }

    private void Step_GenerateVoronoi()
    {
        // Perform voronoi calculation
        _voronoiCalculator = new VoronoiCalculator();
        _voronoiDiagram = _voronoiCalculator.CalculateDiagram(_voronoiSeedSites);

        // Check if generated any NaN vertices
        int badGeneratedVertices = 0;
        foreach (Vector2 v in _voronoiDiagram.Vertices)
        {
            if (!VectorExtensions.IsReal(v)) badGeneratedVertices++;
        }
        if (badGeneratedVertices > 0)
        {
            throw new Exception("Voronoi generated " + badGeneratedVertices + " (NaN, NaN) vertices. This is likely due to LineLineIntersection determinant threshold not being low enough.");
        }
    }

    private void Step_ClipSites()
    {
        // Initialize variables
        _clipperPolygon = outsidePolygon.points.ToList();
        _voronoiClipper = new VoronoiClipper();
        _voronoiClipper.ClipDiagram(_voronoiDiagram, _clipperPolygon);

        // Check for bad clipped sites
        int badClippedSites = 0;
        foreach (var site in _voronoiClipper.clippedSites)
        {
            if (site.clippedVertices.Count == 0) badClippedSites++;
        }
        if (badClippedSites > 0)
        {
            throw new Exception("Clipper clipped " + badClippedSites + " sites to nothing. This is likely due to InsideCircumcircle threshold not being low enough.");
        }
    }

    private void Step_GenerateMeshAndSites()
    {
        // Setup all mesh data variables
        mesh = new Mesh();
        meshSites = new MeshSite[_voronoiClipper.clippedSites.Count];
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        Vector2[] polygonPoints = outsidePolygon.points;

        // Extract mesh variables
        for (int i = 0; i < _voronoiClipper.clippedSites.Count; i++)
        {
            // Add centroid vertex / uv / normal / color
            ClippedSite clippedSite = _voronoiClipper.clippedSites[i];
            int centroidVertexI = vertices.Count;
            Vector2 centroidLocal = clippedSite.clippedCentroid;
            vertices.Add(centroidLocal);
            normals.Add(Vector3.back);
            float centroidAngle = Vector2.Angle(centroidLocal, Vector2.up);
            uvs.Add(new Vector3(Utility.DistanceToPoints(centroidLocal, polygonPoints), centroidAngle, 0));

            // Generate MeshSite
            MeshSite meshSite = new MeshSite();
            meshSite.siteIndex = i;
            meshSite.vertices = new MeshSiteVertex[clippedSite.clippedVertices.Count];
            meshSite.edges = new MeshSiteEdge[clippedSite.clippedVertices.Count];
            meshSite.meshVerticesI = new int[clippedSite.clippedVertices.Count];
            meshSite.meshCentroidI = centroidVertexI;
            meshSite.neighbouringSites = new HashSet<int>();
            meshSites[i] = meshSite;

            // Add vertices vertex / uv / normal / color
            for (int o = 0; o < clippedSite.clippedVertices.Count; o++)
            {
                Vector2 vertexLocal = clippedSite.clippedVertices[o].vertex;
                vertices.Add(vertexLocal);
                normals.Add(Vector3.back);
                float vertexAngle = Vector2.Angle(vertexLocal, Vector2.up);
                uvs.Add(new Vector3(Utility.DistanceToPoints(vertexLocal, polygonPoints), vertexAngle, 0));

                // Add triangle
                int siteVertexI = (o);
                int nextSiteVertexI = (o + 1) % clippedSite.clippedVertices.Count;
                int meshVertexI = (centroidVertexI + 1) + siteVertexI;
                int nextMeshVertexI = (centroidVertexI + 1) + nextSiteVertexI;
                triangles.Add(centroidVertexI);
                triangles.Add(nextMeshVertexI);
                triangles.Add(meshVertexI);

                // Populate MeshSite
                meshSite.vertices[o] = new MeshSiteVertex(clippedSite.clippedVertices[o]);
                meshSite.edges[o] = new MeshSiteEdge(i, siteVertexI, nextSiteVertexI);
                meshSite.meshVerticesI[o] = meshVertexI;
            }
        }

        // Assign mesh variables
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        meshFilter.mesh = mesh;
    }

    private void Step_ProcessSites()
    {
        // Add neighbours using delauney
        Func<int, int, bool> addNeighbours = (s0, s1) =>
        {
            meshSites[s0].neighbouringSites.Add(s1);
            meshSites[s1].neighbouringSites.Add(s0);
            return true;
        };
        var tris = _voronoiDiagram.Triangulation.Triangles;
        for (int ti = 0; ti < tris.Count; ti += 3)
        {
            var v0 = tris[ti];
            var v1 = tris[ti + 1];
            var v2 = tris[ti + 2];
            addNeighbours(v0, v1);
            addNeighbours(v1, v2);
            addNeighbours(v2, v0);
        }


        // Populate each sites edge infos
        HashSet<MeshSiteEdge> surfaceEdges = new HashSet<MeshSiteEdge>();
        foreach (MeshSite meshSite in meshSites)
        {
            for (int i = 0; i < meshSite.vertices.Length; i++)
            {
                // - May have already populated so skip
                MeshSiteEdge edge = meshSite.edges[i];
                if (edge.isOutside || edge.neighbouringSiteIndex != -1) continue;

                // Check if outside
                int nextI = (i + 1) % meshSite.vertices.Length;
                MeshSiteVertex v0 = meshSite.vertices[i];
                MeshSiteVertex v1 = meshSite.vertices[nextI];
                edge.isOutside = (
                    (v0.type == VertexType.POLYGON && v1.type == VertexType.POLYGON)
                    || (v0.type == VertexType.POLYGON_INTERSECTION && v1.type == VertexType.POLYGON && v0.intersectionToUID == v1.vertexUID)
                    || (v0.type == VertexType.POLYGON && v1.type == VertexType.POLYGON_INTERSECTION && v0.vertexUID == v1.intersectionFromUID)
                    || (v0.type == VertexType.POLYGON_INTERSECTION && v1.type == VertexType.POLYGON_INTERSECTION && v0.intersectionFromUID == v1.intersectionFromUID) );
                meshSite.isOutside |= edge.isOutside;
                if (edge.isOutside) surfaceEdges.Add(edge);

                // Inside so find touching edge
                if (!edge.isOutside)
                {
                    // - Loop over each other sites edges
                    foreach (MeshSite oMeshSite in meshSites)
                    {
                        if (meshSite == oMeshSite) continue;
                        bool hasFound = false;
                        ClippedSite oClippedSite = _voronoiClipper.clippedSites[oMeshSite.siteIndex];
                        for (int o = 0; o < oClippedSite.clippedVertices.Count; o++)
                        {
                            int nextO = (o + 1) % oClippedSite.clippedVertices.Count;
                            ClippedVertex ov0 = oClippedSite.clippedVertices[o];
                            ClippedVertex ov1 = oClippedSite.clippedVertices[nextO];
                            
                            // - Check if match in either direction
                            if (
                                v0.vertexUID == ov0.vertexUID && v1.vertexUID == ov1.vertexUID
                                || v0.vertexUID == ov1.vertexUID && v1.vertexUID == ov0.vertexUID
                            ) {
                                edge.neighbouringSiteIndex = oMeshSite.siteIndex;
                                edge.neighbouringEdgeIndex = o;
                                oMeshSite.edges[o].neighbouringSiteIndex = meshSite.siteIndex;
                                oMeshSite.edges[o].neighbouringEdgeIndex = i;
                                hasFound = true;
                                break;
                            }
                        }
                        if (hasFound) break;
                    }

                    // - Should have found edge
                    if (edge.neighbouringSiteIndex == -1)
                    {
                        throw new Exception("Site " + meshSite.siteIndex + " edge " + i + " Could not find any neighbouring sites.");
                    }
                }
            }
        }
    }


    public bool GetIsGenerated() => isGenerated;

    public string GetName() => gameObject.name;    


    private void OnDrawGizmos()
    {
        // Draw all the seed points
        if (showGizmoSeedCentroids && _voronoiSeedSites != null)
        {
            Gizmos.color = Color.red;
            foreach (var seed in _voronoiSeedSites)
            {
                Vector2 seedWorld = transform.TransformPoint(seed);
                Gizmos.DrawSphere(seedWorld, 0.02f);
            }
        }

        // Draw delauney triangulation
        if (showGizmoDelauneyMain && _voronoiDiagram != null && _voronoiDiagram.Triangulation != null)
        {
            var tris = _voronoiDiagram.Triangulation.Triangles;
            var verts = _voronoiDiagram.Triangulation.Vertices;
            Gizmos.color = Color.red;
            foreach (var site in verts)
            {
                Vector2 siteWorld = transform.TransformPoint(site);
                Gizmos.DrawSphere(siteWorld, 0.035f);
            }
            for (int ti = 0; ti < tris.Count; ti += 3)
            {
                var p0 = verts[tris[ti]];
                var p1 = verts[tris[ti + 1]];
                var p2 = verts[tris[ti + 2]];
                var p0w = transform.TransformPoint(p0);
                var p1w = transform.TransformPoint(p1);
                var p2w = transform.TransformPoint(p2);
                Gizmos.color = Color.black;
                Gizmos.DrawLine(p0w, p1w);
                Gizmos.DrawLine(p1w, p2w);
                Gizmos.DrawLine(p2w, p0w);

                if (showGizmoDelauneyCentres)
                {
                    var cw = transform.TransformPoint(Geom.CircumcircleCenter(p0, p1, p2));
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(cw, Vector3.one * 0.02f);
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(p0w, cw);
                    Gizmos.DrawLine(p1w, cw);
                    Gizmos.DrawLine(p2w, cw);
                }
            }
        }

        // Draw unclipped voronoi points
        if (showGizmoVoronoi && _voronoiDiagram != null)
        {
            for (int i = 0; i < _voronoiDiagram.Sites.Count; i++)
            {
                // Draw sphere
                Gizmos.color = Color.red;
                Vector2 siteWorld = transform.TransformPoint(_voronoiDiagram.Sites[i]);
                Gizmos.DrawSphere(siteWorld, 0.05f);

                // Find first / last edge of site
                int firstEdge = firstEdge = _voronoiDiagram.FirstEdgeBySite[i];
                int lastEdge;
                if (i == _voronoiDiagram.Sites.Count - 1) lastEdge = _voronoiDiagram.Edges.Count - 1;
                else lastEdge = _voronoiDiagram.FirstEdgeBySite[i + 1] - 1;

                // Loop over each edge and extract start / direction
                for (int ei = firstEdge; ei <= lastEdge; ei++)
                {
                    var edge = _voronoiDiagram.Edges[ei];
                    Vector2 lv, ld;
                    
                    // - Edge is ray so take direction
                    if (edge.Type == VoronoiDiagram.EdgeType.RayCCW || edge.Type == VoronoiDiagram.EdgeType.RayCW)
                    {
                        lv = transform.TransformPoint(_voronoiDiagram.Vertices[edge.Vert0]);
                        ld = edge.Direction;
                        if (edge.Type == VoronoiDiagram.EdgeType.RayCW) ld *= -1;
                        Gizmos.color = Color.blue;
                        Gizmos.DrawCube(lv, Vector3.one * 0.04f);
                        Gizmos.color = Color.grey;
                        Gizmos.DrawLine(lv, lv + ld.normalized);
                    }

                    // - Edge is segment so create direction
                    else if (edge.Type == VoronoiDiagram.EdgeType.Segment)
                    {
                        var lcv0 = transform.TransformPoint(_voronoiDiagram.Vertices[edge.Vert0]);
                        var lcv1 = transform.TransformPoint(_voronoiDiagram.Vertices[edge.Vert1]);
                        Gizmos.color = Color.blue;
                        Gizmos.DrawCube(lcv0, Vector3.one * 0.04f);
                        Gizmos.DrawCube(lcv1, Vector3.one * 0.04f);
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(lcv0, lcv1);
                    }
                }
            }
        }

        // Draw clipped points
        if (showGizmoClipped && _voronoiClipper != null)
        {
            foreach (var clippedSite in _voronoiClipper.clippedSites)
            {
                Vector2 siteWorld = transform.TransformPoint(clippedSite.clippedCentroid);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(siteWorld, 0.075f);
                
                foreach (var vertex in clippedSite.clippedVertices)
                {
                    Vector2 vertexWorld = transform.TransformPoint(vertex.vertex);
                    if (vertex.type == VoronoiClipper.VertexType.POLYGON) Gizmos.color = Color.black;
                    else if (vertex.type == VoronoiClipper.VertexType.POLYGON_INTERSECTION) Gizmos.color = Color.magenta;
                    else if (vertex.type == VoronoiClipper.VertexType.SITE_VERTEX) Gizmos.color = Color.green;
                    Gizmos.DrawCube(vertexWorld, Vector3.one * 0.04f);
                }
            }
        }
    
        // Draw mesh sites
        if (showGizmoMesh && meshSites != null)
        {
            foreach (MeshSite meshSite in meshSites)
            {
                Vector2 centroidWorld = transform.TransformPoint(mesh.vertices[meshSite.meshCentroidI]);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(centroidWorld, 0.1f);

                Gizmos.color = Color.blue;
                foreach (MeshSiteEdge edge in meshSite.edges)
                {
                    if (edge.isOutside)
                    {
                        Vector2 edge0World = transform.TransformPoint(mesh.vertices[meshSites[edge.siteIndex].meshVerticesI[edge.siteFromVertexI]]);
                        Vector2 edge1World = transform.TransformPoint(mesh.vertices[meshSites[edge.siteIndex].meshVerticesI[edge.siteToVertexI]]);
                        Gizmos.DrawCube((edge0World + edge1World) / 2, Vector3.one * 0.1f);
                    }
                }
            }
        }
    }
}
