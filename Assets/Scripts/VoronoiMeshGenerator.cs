
using GK;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GK.VoronoiClipper;


[ExecuteInEditMode]
public class VoronoiMeshGenerator : MonoBehaviour
{
    [Serializable]
    public class MeshSite
    {
        [Serializable]
        public class EdgeInfo
        {
            public int fromIndex = -1, toIndex = -1;
            public bool isOutside = false;
            public int neighbouringSite = -1;
            public int neighbouringEdge = -1;

            public EdgeInfo(int fromIndex, int toIndex)
            {
                // Initialize variables
                this.fromIndex = fromIndex;
                this.toIndex = toIndex;
            }
        }

        public int siteIndex = -1;
        public bool isOutside = false;
        public EdgeInfo[] edgeInfos;
        public HashSet<int> neighbouringSites = new HashSet<int>();

        public List<int> meshVerticesI = new List<int>();
        public int meshCentroidI = -1;
    }


    [Header("References")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private PolygonCollider2D sourceCollider;

    [Header("Config")]
    [SerializeField] private int seedCount = 300;
    [SerializeField] private int seedMaxTries = 50;
    [SerializeField] private float seedMinDistance = 0.02f;
    [SerializeField] private bool showGizmoSeedCentroids = false;
    [SerializeField] private bool showGizmoDelauneyMain = false;
    [SerializeField] private bool showGizmoDelauneyCentres = false;
    [SerializeField] private bool showGizmoVoronoi = false;
    [SerializeField] private bool showGizmoClipped = false;
    [SerializeField] private bool showGizmoMesh = false;
    [SerializeField] private bool cleanPipeline = true;

    [Header("Values")]
    private Vector2[] _voronoiSeedSites;
    private VoronoiCalculator _voronoiCalculator;
    private VoronoiDiagram _voronoiDiagram;
    private List<Vector2> _clipperPolygon;
    private VoronoiClipper _voronoiClipper;

    [SerializeField] public List<MeshSite> meshSites;
    public Mesh mesh;


    [ContextMenu("Full Generate Mesh")]
    public void FullGenerateMesh()
    {
        // Run all procedures
        _ResetPipeline();
        _GenerateSeeds();
        _GenerateVoronoi();
        _ClipSites();
        _GenerateMeshAndSites();
        _ProcessSites();
        if (cleanPipeline) _CleanPipeline();
    }

    private void _ResetPipeline()
    {
        // Null intermdiate values
        _CleanPipeline();
        
        // Null output variables
        meshSites = null;
        mesh = null;
        meshFilter.mesh = null;
    }

    private void _GenerateSeeds()
    {
        // Generate a set number of seeds
        List<Vector2> voronoiSeedSiteList = new List<Vector2>();
        int tries = 0;
        while (voronoiSeedSiteList.Count < seedCount && tries < seedMaxTries)
        {
            // Generate in a random position
            Vector2 seedWorld = Utility.RandomInPolygon(sourceCollider, tries != 0);
            Vector2 seedLocal = transform.InverseTransformPoint(seedWorld);
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
        if (tries == seedMaxTries) Debug.Log("Hit max number of tries (" + voronoiSeedSiteList.Count + "/" + seedCount + ")");

        // Update seed sites
        _voronoiSeedSites = voronoiSeedSiteList.ToArray();
    }

    private void _GenerateVoronoi()
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
        if (badGeneratedVertices > 0) Debug.LogError("Voronoi generated " + badGeneratedVertices + " (NaN, NaN) vertices. This is likely due to LineLineIntersection determinant threshold not being low enough.");
    }

    private void _ClipSites()
    {
        // Initialize variables
        _clipperPolygon = sourceCollider.points.ToList();
        _voronoiClipper = new VoronoiClipper();
        _voronoiClipper.ClipDiagram(_voronoiDiagram, _clipperPolygon);

        // Check for bad clipped sites
        int badClippedSites = 0;
        foreach (var site in _voronoiClipper.clippedSites)
        {
            if (site.clippedVertices.Count == 0) badClippedSites++;
        }
        if (badClippedSites > 0) Debug.LogError("Clipper clipped " + badClippedSites + " sites to nothing. This is likely due to InsideCircumcircle threshold not being low enough.");
    }

    private void _GenerateMeshAndSites()
    {
        // Setup all mesh data variables
        mesh = new Mesh();
        meshSites = new List<MeshSite>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Extract mesh variables
        for (int i = 0; i < _voronoiClipper.clippedSites.Count; i++)
        {
            // Generate MeshSite
            ClippedSite clippedSite = _voronoiClipper.clippedSites[i];
            MeshSite meshSite = new MeshSite();
            meshSite.siteIndex = i;
            meshSite.edgeInfos = new MeshSite.EdgeInfo[clippedSite.clippedVertices.Count];
            for (int o = 0; o < clippedSite.clippedVertices.Count; o++)
            {
                int nextO = (o + 1) % clippedSite.clippedVertices.Count;
                meshSite.edgeInfos[o] = new MeshSite.EdgeInfo(o, nextO);
            }
            meshSites.Add(meshSite);

            // Add centroid vertex / uv / normal / color
            int centroidIndex = vertices.Count;
            vertices.Add(clippedSite.clippedCentroid);
            uvs.Add(Vector2.one * 0.5f);
            normals.Add(Vector3.back);
            meshSite.meshCentroidI = centroidIndex;
            
            // Add vertices vertex / uv / normal / color / triangle
            for (int o = 0; o < clippedSite.clippedVertices.Count; o++)
            {
                vertices.Add(clippedSite.clippedVertices[o].vertex);
                uvs.Add(Vector2.one * 0.5f);
                normals.Add(Vector3.back);

                int vertexIndex = (centroidIndex + 1) + (o);
                int nextVertexIndex = (centroidIndex + 1) + (o + 1) % clippedSite.clippedVertices.Count;
                triangles.Add(centroidIndex);
                triangles.Add(nextVertexIndex);
                triangles.Add(vertexIndex);
                meshSite.meshVerticesI.Add(vertexIndex);
            }
        }

        // Assign mesh variables
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();
        meshFilter.mesh = mesh;
    }

    private void _ProcessSites()
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
        foreach (MeshSite meshSite in meshSites)
        {
            ClippedSite clippedSite = _voronoiClipper.clippedSites[meshSite.siteIndex];
            for (int i = 0; i < clippedSite.clippedVertices.Count; i++)
            {
                // - May have already populated so skip
                MeshSite.EdgeInfo edgeInfo = meshSite.edgeInfos[i];
                if (edgeInfo.isOutside || edgeInfo.neighbouringSite != -1) continue;

                // Check if outside
                int nextI = (i + 1) % clippedSite.clippedVertices.Count;
                ClippedVertex v0 = clippedSite.clippedVertices[i];
                ClippedVertex v1 = clippedSite.clippedVertices[nextI];
                edgeInfo.isOutside = (
                    (v0.type == ClippedVertexType.POLYGON_INTERSECTION || v0.type == ClippedVertexType.POLYGON)
                    && (v1.type == ClippedVertexType.POLYGON_INTERSECTION || v1.type == ClippedVertexType.POLYGON) );
                meshSite.isOutside |= edgeInfo.isOutside;

                // Inside so find touching edge
                if (!edgeInfo.isOutside)
                {
                    // - Loop over each other sites edges
                    foreach (MeshSite oMeshSite in meshSites)
                    {
                        if (meshSite == oMeshSite) continue;
                        bool hasFound = false;
                        ClippedSite oClippedSite = _voronoiClipper.clippedSites[oMeshSite.siteIndex];
                        for (int o = 0; o < oClippedSite.clippedVertices.Count; o++)
                        {
                            MeshSite.EdgeInfo oEdge = oMeshSite.edgeInfos[o];
                            int nextO = (o + 1) % oClippedSite.clippedVertices.Count;
                            ClippedVertex ov0 = oClippedSite.clippedVertices[o];
                            ClippedVertex ov1 = oClippedSite.clippedVertices[nextO];
                            
                            // - Check if match in either direction
                            if (v0.Equals(ov0) && v1.Equals(ov1) || v0.Equals(ov1) && v1.Equals(ov0))
                            {
                                edgeInfo.neighbouringSite = oMeshSite.siteIndex;
                                edgeInfo.neighbouringEdge = o;
                                oMeshSite.edgeInfos[o].neighbouringSite = meshSite.siteIndex;
                                oMeshSite.edgeInfos[o].neighbouringEdge = i;
                                hasFound = true;
                                break;
                            }
                        }
                        if (hasFound) break;
                    }

                    // - Should have found edge
                    Debug.Assert(edgeInfo.neighbouringSite != -1);
                }
            }
        }
    }

    private void _CleanPipeline()
    {
        // Null all intermediate variables
        _voronoiSeedSites = null;
        _voronoiCalculator = null;
        _voronoiDiagram = null;
        _clipperPolygon = null;
        _voronoiClipper = null;
    }


    private void OnDrawGizmos()
    {
        if (cleanPipeline) return;

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
                    if (vertex.type == VoronoiClipper.ClippedVertexType.POLYGON) Gizmos.color = Color.black;
                    else if (vertex.type == VoronoiClipper.ClippedVertexType.POLYGON_INTERSECTION) Gizmos.color = Color.magenta;
                    else if (vertex.type == VoronoiClipper.ClippedVertexType.SITE_VERTEX) Gizmos.color = Color.green;
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
                foreach (MeshSite.EdgeInfo edgeInfo in meshSite.edgeInfos)
                {
                    if (edgeInfo.isOutside)
                    {
                        Vector2 edge0World = transform.TransformPoint(mesh.vertices[meshSite.meshVerticesI[edgeInfo.fromIndex]]);
                        Vector2 edge1World = transform.TransformPoint(mesh.vertices[meshSite.meshVerticesI[edgeInfo.toIndex]]);
                        Gizmos.DrawCube((edge0World + edge1World) / 2, Vector3.one * 0.1f);
                    }
                }
            }
        }
    }
}
