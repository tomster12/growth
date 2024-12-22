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
                angle = treeNodes[i - 1].angle + treeData.AngleAdd.GetSample(),
                length = treeNodes[i - 1].length + treeData.LengthAdd.GetSample()
            };
        }
    }

    private void GenerateMesh()
    {

    }
}
