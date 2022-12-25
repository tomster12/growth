
using System.Collections.Generic;
using UnityEngine;


public class GridMeshGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material meshMaterial;

    [Header("Config")]
    [SerializeField] private Vector2Int gridSize = new Vector2Int(8, 8);
    [Range(0.0f, 1.0f)]
    [SerializeField] private float offsetRange = 0.6f;

    private Mesh mesh;
    private bool[,] grid;
    private Vector2[,] offsets;


    [ContextMenu("Run Procedures")]
    public void RunProcedures()
    {
        // Run all procedures
        GenerateMap();
        RandomizeOffsets();
        GenerateMesh();
    }

    private void GenerateMap()
    {
        // Initialize all grid points
        grid = new bool[gridSize.x, gridSize.y];
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                grid[x, y] = true;
            }
        }
    }

    private void RandomizeOffsets()
    {
        // Initialize all offsets
        offsets = new Vector2[gridSize.x, gridSize.y];
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                offsets[x, y] = new Vector2(
                    (0.5f + offsetRange * (Random.value - 0.5f)) / gridSize.x,
                    (0.5f + offsetRange * (Random.value - 0.5f)) / gridSize.y
                );
            }
        }
    }

    private void GenerateMesh()
    {
        // Setup all mesh data variables
        mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color32> colors = new List<Color32>();
        List<int> triangles = new List<int>();

        // Convert grid into mesh
        Vector2 half = Vector2.one * 0.5f;
        for (int x = 0; x < gridSize.x - 1; x++)
        {
            for (int y = 0; y < gridSize.y - 1; y++)
            {
                if (grid[x,y] && grid[x+1,y] && grid[x,y+1] && grid[x+1,y+1])
                {
                    int startIndex = vertices.Count;

                    uvs.Add(new Vector2(x + 0, y + 0) / gridSize + offsets[x + 0, y + 0]);
                    uvs.Add(new Vector2(x + 0, y + 1) / gridSize + offsets[x + 0, y + 1]);
                    uvs.Add(new Vector2(x + 1, y + 0) / gridSize + offsets[x + 1, y + 0]);

                    uvs.Add(new Vector2(x + 1, y + 0) / gridSize + offsets[x + 1, y + 0]);
                    uvs.Add(new Vector2(x + 0, y + 1) / gridSize + offsets[x + 0, y + 1]);
                    uvs.Add(new Vector2(x + 1, y + 1) / gridSize + offsets[x + 1, y + 1]);

                    vertices.Add(uvs[startIndex + 0] - half);
                    vertices.Add(uvs[startIndex + 1] - half);
                    vertices.Add(uvs[startIndex + 2] - half);

                    vertices.Add(uvs[startIndex + 3] - half);
                    vertices.Add(uvs[startIndex + 4] - half);
                    vertices.Add(uvs[startIndex + 5] - half);

                    float col1Val = uvs[startIndex + 0].x;
                    float col2Val = uvs[startIndex + 3].x;
                    Color col1 = new Color(col1Val, col1Val, col1Val);
                    Color col2 = new Color(col2Val, col2Val, col2Val);

                    colors.Add(col1);
                    colors.Add(col1);
                    colors.Add(col1);
                    
                    colors.Add(col2);
                    colors.Add(col2);
                    colors.Add(col2);

                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);

                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);

                    triangles.Add(startIndex + 0);
                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 2);

                    triangles.Add(startIndex + 3);
                    triangles.Add(startIndex + 4);
                    triangles.Add(startIndex + 5);
                }
            }
        }

        // Convert to arrays and assign to mesh
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors32 = colors.ToArray();
        mesh.triangles = triangles.ToArray();
        meshRenderer.material = meshMaterial;
        meshFilter.mesh = mesh;
    }
}
