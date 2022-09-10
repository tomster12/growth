
using PixelsForGlory.VoronoiDiagram;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class VoronoiCircleMeshGenerator : MonoBehaviour
{
    public class VoronoiMeshSite
    {
        public int centreVertex;
        public int[] edgeVertices;
    }


    // Declare references, config, variables
    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material meshMaterial;
    [SerializeField] private PolygonCollider2DGenerator colliderGenerator;

    [Header("Config")]
    [SerializeField] private int seedCount = 300;
    [SerializeField] private float seedRingCount = 50;
    [SerializeField] private float seedRingGap = 0.05f;
    [SerializeField] private Vector2 voronoiGridSize = new Vector2(100, 100);
    [SerializeField] private int voronoiRelaxationCycles = 1;
    [SerializeField] private bool showGizmos = false;

    private List<Vector2> seedPositions;
    private VoronoiDiagram<Color> voronoi;
    private Mesh mesh;
    private VoronoiMeshSite[] meshSites;


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
        // Generat a voronoi diagram, whose points are in the range (0, seedGridResolution - 1) inclusive
        // - The points are generated as floats, but should be converted to integers inside the algorithm

        // Setup voronoi and requird variables
        seedPositions = new List<Vector2>();
        var vSites = new List<VoronoiDiagramSite<Color>>();
        voronoi = new VoronoiDiagram<Color>(new Rect(Vector2.zero, voronoiGridSize + Vector2.one));

        // Generate a list of sites with position within a circle, including a gap around the outside
        Vector2 halfSize = 0.5f * voronoiGridSize;
        for (int i = 0; i < seedCount; i++)
        {
            // Keep looping until found a new unique point
            while (true)
            {
                Vector2 unitPos = Random.insideUnitCircle;
                Vector2 gridPos = unitPos * halfSize * (1.0f - seedRingGap) + halfSize;
                if (!vSites.Any(site => site.Coordinate == gridPos))
                {
                    Color col = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
                    seedPositions.Add(gridPos);
                    vSites.Add(new VoronoiDiagramSite<Color>(gridPos, col));
                    break;
                }
            }
        }

        // Genrat a list of sites along a path around a circle
        for (int i = 0; i < seedRingCount; i++)
        {
            while (true)
            {
                float angle = 2.0f * Mathf.PI * i / seedRingCount;
                Vector2 unitPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 gridPos = halfSize + unitPos * halfSize;
                if (!vSites.Any(site => site.Coordinate == gridPos))
                {
                    Color col = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
                    seedPositions.Add(gridPos);
                    vSites.Add(new VoronoiDiagramSite<Color>(gridPos, col));
                    break;
                }
            }
        }

        // Add sites and run algorithms
        voronoi.AddSites(vSites);
        voronoi.GenerateSites(voronoiRelaxationCycles);
    }

    private void GenerateMesh()
    {
        // Setup all mesh data variables
        mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color32> colors = new List<Color32>();
        List<VoronoiMeshSite> meshSiteList = new List<VoronoiMeshSite>();

        // Loop over all sites and setup mesh data
        for (int i = 0; i < voronoi.GeneratedSites.Count; i++)
        {
            // Skip over any site with a vertex on the outside
            bool isEdge = false;
            var vSite = voronoi.GeneratedSites[i];
            for (int o = 0; o < vSite.Vertices.Count; o++)
            {
                if (
                    vSite.Vertices[o].x == 0 || vSite.Vertices[o].x >= voronoiGridSize.x
                    || vSite.Vertices[o].y == 0 || vSite.Vertices[o].y >= voronoiGridSize.y
                ) isEdge = true;
            }
            if (isEdge) continue;

            // Setup mesh and site variables
            int startIndex = vertices.Count;
            meshSiteList.Add(new VoronoiMeshSite());
            meshSiteList[meshSiteList.Count - 1].edgeVertices = new int[vSite.Vertices.Count];

            // Add centroid to data
            Vector2 centroidUV = vSite.Centroid / voronoiGridSize;
            vertices.Add(centroidUV - Vector2.one * 0.5f);
            normals.Add(Vector3.back);
            uvs.Add(centroidUV);
            colors.Add(vSite.SiteData);
            meshSiteList[meshSiteList.Count - 1].centreVertex = startIndex;

            // Add edge vertices to data
            for (int o = 0; o < vSite.Vertices.Count; o++)
            {
                Vector2 vertexUV = vSite.Vertices[o] / voronoiGridSize;
                vertices.Add(vertexUV - Vector2.one * 0.5f);
                normals.Add(Vector3.back);
                uvs.Add(vertexUV);
                colors.Add(vSite.SiteData);
                meshSiteList[meshSiteList.Count - 1].edgeVertices[o] = startIndex + 1 + o;

                // Add triangles to data
                triangles.Add(startIndex + 0);
                triangles.Add(startIndex + 1 + (o + 1) % vSite.Vertices.Count);
                triangles.Add(startIndex + 1 + (o) % vSite.Vertices.Count);
            }
        }

        // Convert to arrays and assign to mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors32 = colors.ToArray();
        meshSites = meshSiteList.ToArray();
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
            float pct = 2.0f * Mathf.Sqrt(dx * dx + dy * dy) / (1.0f - seedRingGap);
            
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
        if (seedPositions != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector2 pos in seedPositions)
            {
                Vector3 drawUV = pos / voronoiGridSize - Vector2.one * 0.5f;
                Gizmos.DrawSphere(transform.TransformPoint(drawUV), 0.05f);
            }
        }
    }
}
