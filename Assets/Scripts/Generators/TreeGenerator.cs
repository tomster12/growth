using UnityEngine;

public struct TreeNode
{
    public float width;
    public float angle;
    public float length;
}

public class TreeGenerator : Generator
{
    public override string Name => "Tree";

    public override void Generate()
    {
        Clear();
        GenerateTreeNodes();
        GenerateMesh();
        IsGenerated = true;
    }

    public override void Clear()
    {
        IsGenerated = false;
    }

    [Header("References")]
    [SerializeField] private PolygonCollider2D polygon;
    [SerializeField] private MeshFilter meshFilter;

    [Header("Parameters")]
    [SerializeField] private TreeData treeData;

    private TreeNode[] treeNodes;

    private void GenerateTreeNodes()
    {
        int count = Mathf.RoundToInt(treeData.Count.GetSample());
        treeNodes = new TreeNode[count];

        treeNodes[0] = new TreeNode
        {
            width = treeData.WidthInitial.GetSample(),
            angle = treeData.AngleInitial.GetSample(),
            length = treeData.LengthInitial.GetSample()
        };

        for (int i = 1; i < treeNodes.Length; i++)
        {
            treeNodes[i] = new TreeNode
            {
                width = (treeNodes[i - 1].width + treeData.WidthAdd.GetSample()) * treeData.WidthDecay,
                angle = (treeNodes[i - 1].angle + treeData.AngleAdd.GetSample()) * treeData.AngleDecay,
                length = (treeNodes[i - 1].length + treeData.LengthAdd.GetSample()) * treeData.LengthDecay
            };
        }
    }

    private void GenerateMesh()
    {
        Vector3[] vertices = new Vector3[treeNodes.Length * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(treeNodes.Length - 1) * 6];

        Vector2 pos = Vector2.zero;

        for (int i = 0; i < treeNodes.Length; i++)
        {
            float angle = (treeNodes[i].angle + 90) * Mathf.Deg2Rad;
            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 dirT = new(-dir.y, dir.x);

            vertices[i * 2] = pos - dirT * treeNodes[i].width;
            vertices[i * 2 + 1] = pos + dirT * treeNodes[i].width;

            uvs[i * 2] = new Vector2(0, i);
            uvs[i * 2 + 1] = new Vector2(1, i);

            if (i > 0)
            {
                triangles[(i - 1) * 6] = i * 2 - 2;
                triangles[(i - 1) * 6 + 1] = i * 2 - 1;
                triangles[(i - 1) * 6 + 2] = i * 2;

                triangles[(i - 1) * 6 + 3] = i * 2;
                triangles[(i - 1) * 6 + 4] = i * 2 - 1;
                triangles[(i - 1) * 6 + 5] = i * 2 + 1;
            }

            pos += dir * treeNodes[i].length;
        }

        Mesh mesh = new()
        {
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        Vector2[] path = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length / 2; i++)
        {
            path[i] = vertices[i * 2];
        }
        for (int i = 0; i < vertices.Length / 2; i++)
        {
            path[vertices.Length - 1 - i] = vertices[i * 2 + 1];
        }

        polygon.pathCount = 1;
        polygon.SetPath(0, path);
    }
}
