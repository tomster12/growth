
using PixelsForGlory.VoronoiDiagram;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class VoronoiColliderMeshGenerator : MonoBehaviour
{
    //public class VoronoiDiagramGeneratedSite<T> where T : new()
    //{
    //    public int Index;
    //    public T SiteData;
    //    public Vector2 Coordinate;
    //    public Vector2 Centroid;
    //    public List<VoronoiDiagramGeneratedEdge> Edges;
    //    public List<Vector2> Vertices;
    //    public List<int> NeighborSites;
    //    public bool IsCorner;
    //    public bool IsEdge;

    //    public VoronoiDiagramGeneratedSite(int index, Vector2 coordinate, Vector2 centroid, T siteData, bool isCorner, bool isEdge);
    //}


    public class MeshSite
    {
        public int centreVertex;
        public List<int> edgeVertices;
        public bool isEdge;
        public int index;
        public List<int> neighbours;


        public MeshSite(int centreVertex_, List<int> edgeVertices_, bool isEdge_, int index_, List<int> neighbours_)
        {
            centreVertex = centreVertex_;
            edgeVertices = edgeVertices_;
            isEdge = isEdge_;
            index = index_;
            neighbours = neighbours_;
        }
    }


    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material meshMaterial;
    [SerializeField] private PolygonCollider2D sourceCollider;

    [Header("Config")]
    [SerializeField] private int seedCount = 300;
    [SerializeField] private int seedPlacementTries = 100;
    [SerializeField] private int voronoiGridHeight = 100;
    [SerializeField] private int voronoiRelaxationCycles = 1;
    [SerializeField] private bool showGizmos = false;
    [SerializeField] private bool constrainPoints = false;
    [SerializeField] private bool toUpdate = false;

    private Vector2Int voronoiGridSize;
    List<VoronoiDiagramSite<Color>> voronoiSeeds;
    private VoronoiDiagram<Color> voronoi;
    private Mesh mesh;
    private MeshSite[] meshSites;


    private void Update()
    {
        if (toUpdate)
        {
            RunProcedures();
            toUpdate = false;
        }
    }


    [ContextMenu("Run Procedures")]
    public void RunProcedures()
    {
        // Run all procedures
        GenerateSeeds();
        GenerateVoronoi();
        GenerateMesh();
        GradientColors();
    }

    private void GenerateSeeds()
    {
        // Setup variables for voronoi generation
        voronoiGridSize = new Vector2Int((int)(voronoiGridHeight * sourceCollider.bounds.size.x / sourceCollider.bounds.size.y), voronoiGridHeight);
        bool[,] seedFlags = new bool[voronoiGridSize.x, voronoiGridSize.y];
        voronoiSeeds = new List<VoronoiDiagramSite<Color>>();

        // Generate seed positions inside the unit circle with a gap
        for (int i = 0; i < seedCount; i++)
        {
            int attempts = 0;
            while (attempts < seedPlacementTries)
            {
                // Pick a random point within the collider
                Vector2 seedWorld = GeometryUtility.RandomInPolygon(sourceCollider, i != 0);
                Vector2 seedUV = (seedWorld - (Vector2)sourceCollider.bounds.min) / sourceCollider.bounds.size;
                Vector2Int seedGrid = Vector2Int.FloorToInt(seedUV * (voronoiGridSize - Vector2.one));

                if (seedFlags[seedGrid.x, seedGrid.y] == false)
                {
                    Color col = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
                    voronoiSeeds.Add(new VoronoiDiagramSite<Color>(seedGrid, col));
                    seedFlags[seedGrid.x, seedGrid.y] = true;
                    break;
                }
                else attempts++;
            }
            if (attempts == seedPlacementTries) Debug.Log("ERROR: Could not place seed " + i);
        }
    }

    private void GenerateVoronoi()
    {
        // Run seeds through voronoi (with exclusive upper bounds)
        voronoi = new VoronoiDiagram<Color>(new Rect(Vector2.zero, voronoiGridSize + Vector2.one));
        voronoi.AddSites(voronoiSeeds);
        voronoi.GenerateSites(voronoiRelaxationCycles);
    }

    private void GenerateMesh()
    {
        // Setup all mesh data variables
        mesh = new Mesh();
        List<MeshSite> meshSiteList = new List<MeshSite>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<Color32> colors = new List<Color32>();
        List<int> triangles = new List<int>();

        // Loop over all sites and setup mesh data
        for (int i = 0; i < voronoi.GeneratedSites.Count; i++)
        {
            VoronoiDiagramGeneratedSite<Color> voronoiSite = voronoi.GeneratedSites[i];
            bool isEdge = false;

            // Calculate world position for all vertices and constrain to polygon
            List<Vector2> siteVerticesGrid = voronoiSite.Vertices;
            List<Vector2> siteVerticesUV = new List<Vector2>();
            List<Vector2> siteVerticesWorldBase = new List<Vector2>();
            List<Vector2> siteVerticesWorld = new List<Vector2>();
            for (int o = 0; o < voronoiSite.Vertices.Count; o++)
            {
                siteVerticesUV.Add(siteVerticesGrid[o] / (voronoiGridSize - Vector2.one));
                siteVerticesWorld.Add((Vector2)sourceCollider.bounds.min + siteVerticesUV[o] * sourceCollider.bounds.size);
                siteVerticesWorldBase.Add(siteVerticesWorld[o]);
                if (constrainPoints && !sourceCollider.OverlapPoint(siteVerticesWorld[o]))
                {
                    siteVerticesWorld[o] = sourceCollider.ClosestPoint(siteVerticesWorld[o]);
                    siteVerticesUV[o] = (siteVerticesWorld[o] - (Vector2)sourceCollider.bounds.min) / sourceCollider.bounds.size;
                    isEdge = true;
                }
            }

            // Find all collider points within the site
            Vector2 averageWorld = GeometryUtility.GetAveragePoint(siteVerticesWorld);
            List<Vector2> sitePolygonPoints = new List<Vector2>();
            bool toLog = false;
            foreach (Vector2 p in sourceCollider.points)
            {
                Vector3 tp = transform.TransformPoint(p);
                if (GeometryUtility.PointInside(tp, siteVerticesWorldBase))
                {
                    sitePolygonPoints.Add(tp);
                    toLog = true;
                    Debug.Log("--------------------------------------------------------Polygon point " + tp);
                }
            }

            // Add vertices to shapes
            List<int> siteEdgeVertices = new List<int>();
            int siteEdgeVertexStart = vertices.Count;
            for (int o = 0; o < siteVerticesWorld.Count; o++)
            {
                // Chec1k whether collider point fits inbetween
                float angle = (Vector2.SignedAngle(siteVerticesWorld[0] - averageWorld, siteVerticesWorld[o] - averageWorld) + 360) % 360;
                if (toLog) Debug.Log("Checking point " + siteVerticesWorld[o] + " with angle " + angle);
                if (sitePolygonPoints.Count != 0 && toLog) Debug.Log("Checking if any of " + sitePolygonPoints.Count + " points are inside");
                for (int p = 0; p < sitePolygonPoints.Count; p++)
                {
                    float cAngle = (Vector2.SignedAngle(siteVerticesWorld[0] - averageWorld, sitePolygonPoints[p] - averageWorld) + 360) % 360;
                    if (cAngle <= angle)
                    {
                        siteVerticesWorld.Insert(o, sitePolygonPoints[p]);
                        siteVerticesUV.Insert(o, (siteVerticesWorld[o] - (Vector2)sourceCollider.bounds.min) / sourceCollider.bounds.size);
                        if (toLog) Debug.Log("Point at " + sitePolygonPoints[p] + " is inside with angle " + cAngle);
                        sitePolygonPoints.RemoveAt(p);
                        p = 0;
                    }
                    if (o == siteVerticesWorld.Count - 1 && cAngle > angle)
                    {
                        siteVerticesWorld.Insert(o + 1, sitePolygonPoints[p]);
                        siteVerticesUV.Insert(o + 1, (siteVerticesWorld[o + 1] - (Vector2)sourceCollider.bounds.min) / sourceCollider.bounds.size);
                        if (toLog) Debug.Log("Point at " + sitePolygonPoints[p] + " is inside end with angle " + cAngle);
                        sitePolygonPoints.RemoveAt(p);
                        p = 0;
                    }
                }

                if (toLog) Debug.Log("Adding vertex at world " + siteVerticesWorld[o]);
                Vector2 vertexLocal = transform.InverseTransformPoint(siteVerticesWorld[o]);
                uvs.Add(siteVerticesUV[o]);
                vertices.Add(vertexLocal);
                normals.Add(Vector3.back);
                colors.Add(voronoiSite.SiteData);
                siteEdgeVertices.Add(siteEdgeVertexStart + o);
            }

            // Add centroid vertices to data using average
            int siteCentreVertex = vertices.Count;
            Vector2 centroidWorld = GeometryUtility.GetAveragePoint(siteVerticesWorld);
            Vector2 centroidUV = (centroidWorld - (Vector2)sourceCollider.bounds.min) / sourceCollider.bounds.size;
            Vector2 centroidLocal = transform.InverseTransformPoint(centroidWorld);
            if (toLog) Debug.Log(centroidWorld);

            uvs.Add(centroidUV);
            vertices.Add(centroidLocal);
            normals.Add(Vector3.back);
            colors.Add(voronoiSite.SiteData);

            // Calculate fanned triangles
            for (int o = 0; o < siteVerticesWorld.Count; o++)
            {
                triangles.Add(siteCentreVertex);
                triangles.Add(siteEdgeVertexStart + (o + 1) % siteVerticesWorld.Count);
                triangles.Add(siteEdgeVertexStart + (o) % siteVerticesWorld.Count);
            }

            // Create mesh sites
            meshSiteList.Add(new MeshSite(siteCentreVertex, siteEdgeVertices, isEdge, voronoiSite.Index, voronoiSite.NeighborSites));
        }

        // Convert to arrays and assign to mesh
        meshSites = meshSiteList.ToArray();
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors32 = colors.ToArray();
        mesh.triangles = triangles.ToArray();
        meshFilter.mesh = mesh;
        meshRenderer.material = meshMaterial;
    }

    private void GradientColors()
    {
        // Change colours to a gradient
        Color[] meshColors = mesh.colors;
        foreach (var meshSite in meshSites)
        {
            Vector2 centroidUV = mesh.uv[meshSite.centreVertex];
            float dx = centroidUV.x - 0.5f;
            float dy = centroidUV.y - 0.5f;
            float pct = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
            Color col = new Color(Random.value * 0.3f + 0.7f, Random.value * 0.3f + 0.7f, Random.value * 0.3f + 0.7f);
            meshColors[meshSite.centreVertex] = col;
            foreach (int v in meshSite.edgeVertices) meshColors[v] = col;
        }
        mesh.colors = meshColors;
    }


    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Loop over all sites and setup mesh data
        //if (voronoi != null)
        //{
        //    Gizmos.color = Color.green;
        //    for (int i = 0; i < voronoi.GeneratedSites.Count; i++)
        //    {
        //        var voronoiSite = voronoi.GeneratedSites[i];
        //        for (int o = 0; o < voronoiSite.Vertices.Count; o++)
        //        {
        //            Vector2 vertexGrid = voronoiSite.Vertices[o];
        //            Vector2 vertexUV = vertexGrid / (voronoiGridSize - Vector2.one);
        //            Vector2 vertexWorld = (Vector2)collider.bounds.min + vertexUV * collider.bounds.size;
        //            Gizmos.DrawSphere(vertexWorld, 0.05f);
        //            if (!collider.OverlapPoint(vertexWorld))
        //                Debug.DrawLine(vertexWorld, collider.ClosestPoint(vertexWorld));
        //        }
        //    }
        //}

        // Draw mouse
        //if (Camera.main != null)
        //{
        //    Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //    if (!sourceCollider.OverlapPoint(mousePos))
        //    {
        //        mousePos = sourceCollider.ClosestPoint(mousePos);
        //    }
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawSphere(mousePos, 0.05f);
        //}

        //Draw all the input centroid points
        //if (voronoiSeeds != null)
        //{
        //    Gizmos.color = Color.red;
        //    foreach (var seed in voronoiSeeds)
        //    {
        //        // Pick a random point within the collider
        //        Vector2 seedGrid = seed.Coordinate;
        //        Vector2 seedUV = seedGrid / (voronoiGridSize - Vector2.one);
        //        Vector2 seedWorld = seedUV * sourceCollider.bounds.size + (Vector2)sourceCollider.bounds.min;

        //        Gizmos.DrawSphere(seedWorld, 0.05f);
        //    }
        //}

        // Draw all mesh sites
        if (meshSites != null)
        {
            foreach (var meshSite in meshSites)
            {
                Vector3 centroid = transform.TransformPoint(mesh.vertices[meshSite.centreVertex]);
                Gizmos.DrawSphere(centroid, 0.05f);
            }
        }
    }
}
