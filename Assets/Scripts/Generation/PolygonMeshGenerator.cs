
using UnityEngine;


public class PolygonMeshGenerator : MonoBehaviour
{
    // --- Parameters ---
    [Header("Parameters")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private PolygonCollider2D polygon;
    [SerializeField] private Color[] colorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };
    
    // --- Internal ---
    private Mesh mesh;


    [ContextMenu("Generate Mesh")]
    public void Generate()
    {
        // Generate source mesh
        Mesh sourceMesh = polygon.CreateMesh(false, false);

        // Initialize new mesh variables
        int newVertexCount = sourceMesh.triangles.Length;
        Vector3[] vertices = new Vector3[newVertexCount];
        int[] triangles = new int[newVertexCount];
        Color[] colors = new Color[newVertexCount];

        // Randomize colours
        for (int i = 0; i < sourceMesh.triangles.Length; i += 3)
        {
            float r = UnityEngine.Random.value;
            Color col = Color.Lerp(colorRange[0], colorRange[1], r);
            for (int o = i; o < i + 3; o++)
            {
                Vector3 pos = transform.InverseTransformPoint(sourceMesh.vertices[sourceMesh.triangles[o]]);
                pos = new Vector3(pos.x, pos.y, 0.0f);
                vertices[o] = pos;
                triangles[o] = o;
                colors[o] = col;
            }
        }

        // Create new mesh and set
        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mf.mesh = mesh;
    }
}
