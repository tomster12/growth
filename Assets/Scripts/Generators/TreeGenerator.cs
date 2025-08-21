using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TreeNode
{
    // Configuration
    public float width;
    public float angle;
    public float localAngle;
    public float length;
    public float area;
    public Color color;
    public int groundDistance;
    public int endDistance;

    // References
    public TreeNode parentNode;
    public TreeNode childNode;
    public TreeNode branchNode;

    // Runtime
    public Transform transform;
    public Matrix4x4[] leafMatrices;
}

public class TreeGenerator : Generator
{
    public override string Name => "Tree";
    public float BaseWidth => TreeNodes[0].width;
    public TreeData TreeData => treeData;
    public List<TreeNode> TreeNodes => treeNodes;

    public override void Generate()
    {
        Clear();
        GenerateTreeNodes();
        GenerateMesh();
        GenerateLeaves();
        IsGenerated = true;
    }

    public override void Clear()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        IsGenerated = false;
    }

    [Header("References")]
    [SerializeField] private Material material;

    [Header("Parameters")]
    [SerializeField] private TreeData treeData;

    [SerializeField] private List<TreeNode> treeNodes; // Temporary serialized field for debugging
    private Mesh leafMesh;

    private void GenerateTreeNodes()
    {
        int endDistance = Mathf.RoundToInt(treeData.Count.GetSample());

        // Start queue initialized with base node
        TreeNode baseNode = new()
        {
            width = treeData.WidthInitial.GetSample(),
            angle = treeData.AngleInitial.GetSample(),
            length = treeData.LengthInitial.GetSample(),
            color = Color.Lerp(treeData.ColorMin, treeData.ColorMax, UnityEngine.Random.value),
            endDistance = endDistance - 1,
            groundDistance = 0
        };

        treeNodes = new List<TreeNode> { baseNode };
        Queue<TreeNode> processQueue = new();
        processQueue.Enqueue(baseNode);

        // While there are nodes to process
        while (processQueue.Count > 0)
        {
            TreeNode node = processQueue.Dequeue();

            // Update nodes local angle based on parent
            node.localAngle = node.parentNode != null ? node.angle - node.parentNode.angle : node.angle;

            if (node.endDistance == 0) continue;

            // Create the 1 guaranteed child node
            node.childNode = new TreeNode
            {
                parentNode = node,
                width = (node.width + treeData.WidthAdd.GetSample()) * treeData.WidthDecay,
                angle = (node.angle + treeData.AngleAdd.GetSample()) * treeData.AngleDecay,
                length = (node.length + treeData.LengthAdd.GetSample()) * treeData.LengthDecay,
                color = node.color,
                endDistance = node.endDistance - 1,
                groundDistance = node.groundDistance + 1
            };

            treeNodes.Add(node.childNode);
            processQueue.Enqueue(node.childNode);

            // Potentially Create the 1 branch node at an offset
            if (UnityEngine.Random.value < treeData.BranchChance)
            {
                node.branchNode = new TreeNode
                {
                    parentNode = node,
                    width = (node.width + treeData.WidthAdd.GetSample()) * treeData.WidthDecay,
                    angle = node.angle + Mathf.Sign(node.childNode.angle) * treeData.BranchAngleAdd.GetSample() * Mathf.Sign(UnityEngine.Random.value - 0.5f),
                    length = (node.length + treeData.LengthAdd.GetSample()) * treeData.LengthDecay,
                    color = node.color,
                    endDistance = node.endDistance - 1,
                    groundDistance = node.groundDistance + 1
                };

                // Branch node explicitly causes tree to grow less
                node.childNode.width *= 0.9f;
                node.branchNode.width *= 0.6f;
                node.branchNode.endDistance = (int)Mathf.Min(node.branchNode.endDistance * 0.4f, 2);

                treeNodes.Add(node.branchNode);
                processQueue.Enqueue(node.branchNode);
            }
        }
    }

    private void GenerateMesh()
    {
        // Create the tree as a series of parented segments
        foreach (TreeNode node in treeNodes)
        {
            float bottomWidth = node.width;
            float topWidth = node.childNode != null ? node.childNode.width : bottomWidth * 0.65f;
            node.area = node.length * (bottomWidth + topWidth) / 2.0f;

            // Create a new child gameobject with a mesh
            GameObject nodeObject = new("Segment");
            MeshFilter meshFilter = nodeObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = nodeObject.AddComponent<MeshRenderer>();

            nodeObject.transform.SetParent(node.parentNode != null ? node.parentNode.transform : transform);
            nodeObject.transform.SetLocalPositionAndRotation(
                node.parentNode != null ? (Vector3.up * node.parentNode.length) : Vector3.zero,
                Quaternion.Euler(0.0f, 0.0f, node.localAngle));

            node.transform = nodeObject.transform;

            // Create the mesh as a quad with a circular bottom
            int endCapVertexCount = 8;
            Vector3[] vertices = new Vector3[endCapVertexCount + 4];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[endCapVertexCount * 3 + 6];
            Color[] colors = new Color[vertices.Length];

            // Create the quad
            vertices[0] = new Vector3(-bottomWidth / 2.0f, 0.0f, 0.0f);
            vertices[1] = new Vector3(bottomWidth / 2.0f, 0.0f, 0.0f);
            vertices[2] = new Vector3(-topWidth / 2.0f, node.length, 0.0f);
            vertices[3] = new Vector3(topWidth / 2.0f, node.length, 0.0f);
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0, 0);
            uvs[2] = new Vector2(0, 0);
            uvs[3] = new Vector2(0, 0);
            triangles[0] = 0;
            triangles[1] = 2;
            triangles[2] = 1;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 1;
            colors[0] = colors[1] = colors[2] = colors[3] = node.color;

            // Create the circular bottom
            for (int j = 0; j < endCapVertexCount; j++)
            {
                float anglePct = (j + 1) / (float)(endCapVertexCount + 1);
                float angle = anglePct * Mathf.PI;
                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);

                vertices[4 + j] = new Vector3(x * bottomWidth / 2.0f, -y * bottomWidth / 2.0f, 0.0f);
                uvs[4 + j] = new Vector2(0, 0);
                triangles[6 + j * 3 + 0] = (j == endCapVertexCount - 1) ? 0 : (4 + j + 1);
                triangles[6 + j * 3 + 1] = 1;
                triangles[6 + j * 3 + 2] = 4 + j;
                colors[4 + j] = node.color;
            }

            Mesh mesh = new()
            {
                vertices = vertices,
                uv = uvs,
                triangles = triangles,
                colors = colors
            };

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            meshRenderer.material = material;
        }
    }

    private void GenerateLeaves()
    {
        // Generate simple leaf mesh
        leafMesh = new Mesh
        {
            vertices = new Vector3[]
            {
                new(-0.1f, 0.0f, 0.0f),
                new(0.1f, 0.0f, 0.0f),
                new(0.0f, 0.2f, 0.0f)
            },
            triangles = new int[] { 0, 2, 1 },
            uv = new Vector2[]
            {
                new(0, 0),
                new(1, 0),
                new(0.5f, 1)
            }
        };
        leafMesh.RecalculateBounds();
        leafMesh.RecalculateNormals();

        // Generate leaf matrices for each leaf
        foreach (TreeNode node in treeNodes)
        {
            if (node.childNode != null) continue;

            List<Matrix4x4> leafMatrices = new();

            Matrix4x4 branchMatrix = node.transform.localToWorldMatrix;

            int leafCount = UnityEngine.Random.Range(80, 100);

            for (int i = 0; i < leafCount; i++)
            {
                Vector3 position = new(0.0f, node.length, -0.5f);

                position.x += UnityEngine.Random.Range(-node.width, node.width);
                position.y += UnityEngine.Random.Range(-node.length * 0.3f, node.length * 0.3f);

                Quaternion rotation = Quaternion.Euler(0.0f, 0.0f, UnityEngine.Random.Range(0.0f, 360.0f));

                Matrix4x4 leafMatrix = branchMatrix * Matrix4x4.TRS(position, rotation, Vector3.one);
                leafMatrices.Add(leafMatrix);
            }

            node.leafMatrices = leafMatrices.ToArray();
        }
    }

    private void OnDrawGizmos()
    {
        if (treeNodes == null) return;

        //foreach (TreeNode node in treeNodes)
        //{
        //    // Draw leaves
        //    if (node.leafMatrices != null)
        //    {
        //        Gizmos.color = new Color(0.4f, 0.9f, 0.5f);
        //        foreach (Matrix4x4 leafMatrix in node.leafMatrices)
        //        {
        //            Gizmos.matrix = leafMatrix;
        //            Gizmos.DrawMesh(leafMesh);
        //        }
        //    }
        //}
    }
}
