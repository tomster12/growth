using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TreeNode
{
    public float width;
    public float angle;
    public float localAngle;
    public float length;
    public float area;
    public Color color;

    public TreeNode parentNode;
    public TreeNode childNode;
    public TreeNode branchNode;
    public Transform transform;
    public int groundDistance;
    public int endDistance;
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

    [SerializeField] private List<TreeNode> treeNodes;

    private void GenerateTreeNodes()
    {
        int endDistance = Mathf.RoundToInt(treeData.Count.GetSample());

        TreeNode baseNode = new TreeNode
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

        while (processQueue.Count > 0)
        {
            TreeNode node = processQueue.Dequeue();

            node.localAngle = node.parentNode != null ? node.angle - node.parentNode.angle : node.angle;

            if (node.endDistance == 0) continue;

            // Create guaranteed child node
            node.childNode = new TreeNode
            {
                parentNode = node,
                width = (node.width + treeData.WidthAdd.GetSample()) * treeData.WidthDecay,
                angle = (node.angle + treeData.AngleAdd.GetSample()) * treeData.AngleDecay,
                length = (node.length + treeData.LengthAdd.GetSample()) * treeData.LengthDecay,
                color = Color.Lerp(treeData.ColorMin, treeData.ColorMax, UnityEngine.Random.value),
                endDistance = node.endDistance - 1,
                groundDistance = node.groundDistance + 1
            };

            treeNodes.Add(node.childNode);
            processQueue.Enqueue(node.childNode);

            // Create branch node at an offset
            if (UnityEngine.Random.value < treeData.BranchChance)
            {
                node.branchNode = new TreeNode
                {
                    parentNode = node,
                    width = (node.width + treeData.WidthAdd.GetSample()) * treeData.WidthDecay,
                    angle = node.angle + Mathf.Sign(node.childNode.angle) * treeData.BranchAngleAdd.GetSample() * Mathf.Sign(UnityEngine.Random.value - 0.5f),
                    length = (node.length + treeData.LengthAdd.GetSample()) * treeData.LengthDecay,
                    color = Color.Lerp(treeData.ColorMin, treeData.ColorMax, UnityEngine.Random.value),
                    endDistance = node.endDistance - 1,
                    groundDistance = node.groundDistance + 1
                };

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
            GameObject nodeObject = new GameObject("Segment");
            MeshFilter meshFilter = nodeObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = nodeObject.AddComponent<MeshRenderer>();

            nodeObject.transform.SetParent(node.parentNode != null ? node.parentNode.transform : transform);
            nodeObject.transform.localPosition = node.parentNode != null ? (Vector3.up * node.parentNode.length) : Vector3.zero;
            nodeObject.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, node.localAngle);
            node.transform = nodeObject.transform;

            // Create the mesh as a quad with a circular bottom
            int endCapVertexCount = 8;
            Vector3[] vertices = new Vector3[endCapVertexCount + 4];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[endCapVertexCount * 3 + 6];

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
            }

            Mesh mesh = new();

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            meshRenderer.material = material;
        }
    }
}
