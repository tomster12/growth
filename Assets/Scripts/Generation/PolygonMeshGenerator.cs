
using UnityEngine;

public class PolygonMeshGenerator : MonoBehaviour, IGenerator
{
    [Header("Parameters")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private PolygonCollider2D polygon;
    [SerializeField] private CircleCollider2D circleToFit;
    [SerializeField] private Color[] colorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };
    [SerializeField] private NoiseData colorNoise = new NoiseData(new float[] { 0, 1 });
    
    public bool isGenerated { get; private set; } = false;

    private Mesh mesh;


    public void Clear()
    {
        mesh = null;
        mf.mesh = null;
        isGenerated = false;
    }

    public void Generate()
    {
        Clear();

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
                pos = polygon.transform.InverseTransformPoint(pos);
                vertices[o] = pos;
                triangles[o] = o;
                colors[o] = col;
            }
        }

        // Center all the vertices
        Vector3 averagePosition = Vector2.zero;
        foreach (Vector3 v in vertices) averagePosition += v;
        averagePosition /= vertices.Length;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = vertices[i] - averagePosition;
        polygon.offset -= (Vector2)averagePosition;

        // Fit circle collider
        if (circleToFit != null)
        {
            float smallest = float.PositiveInfinity;
            foreach (Vector3 v in vertices) smallest = Mathf.Min(v.magnitude, smallest);
            circleToFit.radius = smallest;
        }

        // Create new mesh and set
        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mf.mesh = mesh;

        isGenerated = true;
    }


    public bool GetIsGenerated() => isGenerated;
    
    public string GetName() => gameObject.name;    
}
