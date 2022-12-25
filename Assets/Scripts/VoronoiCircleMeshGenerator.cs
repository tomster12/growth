
using PixelsForGlory.VoronoiDiagram;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[ExecuteInEditMode]
public class VoronoiCircleMeshGenerator : MonoBehaviour
{
    public class MeshSite
    {
        public int centreVertex;
        public int[] edgeVertices;

        public MeshSite(int edgeCount)
        {
            edgeVertices = new int[edgeCount];
        }
    }


    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material meshMaterial;
    [SerializeField] private PolygonCollider2DGenerator colliderGenerator;

    [Header("Config")]
    [SerializeField] private int seedCount = 300;
    [SerializeField] private float seedRingCount = 50;
    [SerializeField] private float seedRingGap = 0.05f;
    [SerializeField] private int seedPlacementTries = 100;
    [SerializeField] private int voronoiGridHeight = 100;
    [SerializeField] private int voronoiRelaxationCycles = 1;
    [SerializeField] private bool showGizmos = false;

    [SerializeField] private bool toUpdate = false;

    private Vector2Int gridSize;
    private List<VoronoiDiagramSite<Color>> seeds;
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
        GenerateVoronoi();
        GenerateMesh();
        GradientColors();
        colliderGenerator.CreatePolygon2DColliderPoints();
    }

    private void GenerateVoronoi()
    {
        // Setup variables for voronoi generation
        gridSize = new Vector2Int(voronoiGridHeight, voronoiGridHeight);
        Vector2 halfSize = (gridSize - Vector2.one) * 0.5f;
        bool[,] seedFlags = new bool[voronoiGridHeight, voronoiGridHeight];
        seeds = new List<VoronoiDiagramSite<Color>>();

        // Generate seed positions in a ring
        for (int i = 0; i < seedRingCount; i++)
        {
            float angle = 2.0f * Mathf.PI * i / seedRingCount;
            Vector2 unitPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2Int gridPos = Vector2Int.FloorToInt(halfSize + unitPos * halfSize);

            if (seedFlags[gridPos.x, gridPos.y] == false)
            {
                Color col = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
                seeds.Add(new VoronoiDiagramSite<Color>(gridPos, col));
                seedFlags[gridPos.x, gridPos.y] = true;

            } else Debug.Log("ERROR: Could not place ring seed " + i);
        }

        // Generate seed positions inside the unit circle with a gap
        for (int i = 0; i < seedCount; i++)
        {
            int attempts = 0;
            while (attempts < seedPlacementTries)
            {
                Vector2 unitPos = UnityEngine.Random.insideUnitCircle;
                Vector2Int gridPos = Vector2Int.FloorToInt(unitPos * halfSize * (1.0f - seedRingGap) + halfSize);

                if (seedFlags[gridPos.x, gridPos.y] == false)
                {
                    Color col = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
                    seeds.Add(new VoronoiDiagramSite<Color>(gridPos, col));
                    seedFlags[gridPos.x, gridPos.y] = true;
                    break;

                } else attempts++;
            }
            if (attempts == seedPlacementTries) Debug.Log("ERROR: Could not place seed " + i);
        }

        // Run seeds through voronoi (with exclusive upper bounds)
        voronoi = new VoronoiDiagram<Color>(new Rect(Vector2.zero, gridSize + Vector2.one));
        voronoi.AddSites(seeds);
        voronoi.GenerateSites(voronoiRelaxationCycles);
    }

    private void GenerateMesh()
    {
        // Setup all mesh data variables
        mesh = new Mesh();
        List<MeshSite> meshSiteList = new List<MeshSite>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Color32> colors = new List<Color32>();
        List<int> triangles = new List<int>();

        // Loop over all sites and setup mesh data
        for (int i = 0; i < voronoi.GeneratedSites.Count; i++)
        {
            // Setup site and ensure is not on an edge
            var voronoiSite = voronoi.GeneratedSites[i];
            meshSiteList.Add(new MeshSite(voronoiSite.Vertices.Count));
            bool isEdgeSite = voronoiSite.Vertices.Any(v =>
                v.x == 0 || v.x >= voronoiGridHeight
                || v.y == 0 || v.y >= voronoiGridHeight);
            if (isEdgeSite) continue;

            // Add centroid to data
            int centreIndex = vertices.Count;
            uvs.Add(voronoiSite.Centroid / (gridSize - Vector2.one));
            vertices.Add(uvs[centreIndex] - Vector2.one * 0.5f);
            normals.Add(Vector3.back);
            colors.Add(voronoiSite.SiteData);
            meshSiteList[meshSiteList.Count - 1].centreVertex = centreIndex;

            // Add edge vertices to data
            for (int o = 0; o < voronoiSite.Vertices.Count; o++)
            {
                int vertexIndex = (centreIndex + 1) + (o);
                uvs.Add(voronoiSite.Vertices[o] / (gridSize - Vector2.one));
                vertices.Add(uvs[vertexIndex] - Vector2.one * 0.5f);
                normals.Add(Vector3.back);
                colors.Add(voronoiSite.SiteData);
                meshSiteList[meshSiteList.Count - 1].edgeVertices[o] = vertexIndex;

                // Add triangles to data
                triangles.Add(centreIndex);
                triangles.Add((centreIndex + 1) + (o + 1) % voronoiSite.Vertices.Count);
                triangles.Add((centreIndex + 1) + (o) % voronoiSite.Vertices.Count);
            }
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
            float pct = Mathf.Sqrt(dx * dx + dy * dy) / (0.5f - seedRingGap);
            
            Color col = new Color(pct, pct, pct, 1.0f);
            meshColors[meshSite.centreVertex] = col;
            foreach (int v in meshSite.edgeVertices) meshColors[v] = col;
        }
        mesh.colors = meshColors;
    }


    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw all the input centroid points
        if (meshSites != null)
        {
            Gizmos.color = Color.red;
            foreach (var site in meshSites)
            {
                Vector3 drawPos = mesh.vertices[site.centreVertex];
                Gizmos.DrawSphere(transform.TransformPoint(drawPos), 0.05f);
                foreach (int vertex in site.edgeVertices)
                {
                    Gizmos.DrawLine(transform.TransformPoint(drawPos), transform.TransformPoint(mesh.vertices[vertex]));

                }
            }
        }
    }
}
