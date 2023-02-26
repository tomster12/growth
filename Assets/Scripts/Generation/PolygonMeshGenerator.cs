
using UnityEngine;


public class PolygonMeshGenerator : Generator
{
    // --- Parameters ---
    [Header("Parameters")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private PolygonCollider2D polygon;
    [SerializeField] private Color[] colorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };
    [SerializeField] private NoiseData colorNoise = new NoiseData(new float[] { 0, 1 });
    
    // --- Internal ---
    private Mesh mesh;


    [ContextMenu("Generate Mesh")]
    public override void Generate()
    {
        // Reset parent temporarily
        Transform parent = transform.parent;
        transform.parent = null;

        // Generate source mesh
        Mesh sourceMesh = polygon.CreateMesh(false, false);
        sourceMesh.RecalculateBounds();

        // Initialize new mesh variables
        int newVertexCount = sourceMesh.triangles.Length;
        Vector3[] vertices = new Vector3[newVertexCount];
        int[] triangles = new int[newVertexCount];
        Color[] colors = new Color[newVertexCount];

        // - For each triangle in the mesh
        for (int i = 0; i < sourceMesh.triangles.Length; i += 3)
        {
            float r = colorNoise.GetNoise((float)i / (sourceMesh.triangles.Length - 1), 0);
            Color col = Color.Lerp(colorRange[0], colorRange[1], r);

            // - For each vertex recreate
            for (int o = i; o < i + 3; o++)
            {
                Vector2 pos = sourceMesh.vertices[sourceMesh.triangles[o]];
                pos = transform.InverseTransformPoint(pos);
                vertices[o] = pos;
                triangles[o] = o;
                colors[o] = col;
            }
        }

        // Center all the vertices
        //Vector3 averagePosition = Vector2.zero;
        //foreach (Vector3 v in vertices) averagePosition += v;
        //averagePosition /= vertices.Length;
        //for (int i = 0; i < vertices.Length; i++) vertices[i] = vertices[i] - averagePosition;
        //polygon.offset -= (Vector2)averagePosition;

        // Create new mesh and set
        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mf.mesh = mesh;
        transform.parent = parent;
    }
}
