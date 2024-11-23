using System.Collections.Generic;
using UnityEngine;

public interface IBVHElement
{
    Bounds GetBounds();

    float Distance(Vector2 point);

    Vector2 ClosestPoint(Vector2 point);
}

public class BVHNode<T> where T : IBVHElement
{
    public Bounds Bounds;
    public BVHNode<T> Left;
    public BVHNode<T> Right;
    public List<T> Elements;
    public bool IsLeaf => Left == null && Right == null;
}

public class BVH<T> where T : IBVHElement
{
    public BVHNode<T> Root;

    public BVH(List<T> elements, int maxElementsPerNode = 2)
    {
        Root = BuildBVH(elements, maxElementsPerNode);
    }

    private BVHNode<T> BuildBVH(List<T> elements, int maxElementsPerNode)
    {
        BVHNode<T> node = new BVHNode<T>();

        // Compute bounds for all elements in this node
        node.Bounds = ComputeBounds(elements);

        // Finish as a leaf node
        if (elements.Count <= maxElementsPerNode)
        {
            node.Elements = elements;
            return node;
        }

        // Split elements along the longest axis of the bounding box
        Vector3 size = node.Bounds.size;
        int axis = size.x > size.y ? 0 : 1;

        elements.Sort((a, b) =>
        {
            float aMid = a.GetBounds().center[axis];
            float bMid = b.GetBounds().center[axis];
            return aMid.CompareTo(bMid);
        });

        int mid = elements.Count / 2;
        var leftElements = elements.GetRange(0, mid);
        var rightElements = elements.GetRange(mid, elements.Count - mid);

        // Create child nodes
        node.Left = BuildBVH(leftElements, maxElementsPerNode);
        node.Right = BuildBVH(rightElements, maxElementsPerNode);

        return node;
    }

    private Bounds ComputeBounds(List<T> elements)
    {
        Bounds bounds = elements[0].GetBounds();
        foreach (var element in elements)
        {
            bounds.Encapsulate(element.GetBounds());
        }
        return bounds;
    }

    public T FindClosestElement(Vector2 point, float maxDistance = float.MaxValue)
    {
        if (Root == null) return default;

        Stack<BVHNode<T>> stack = new Stack<BVHNode<T>>();
        stack.Push(Root);

        T closestElement = default;
        float closestDistance = maxDistance;

        while (stack.Count > 0)
        {
            BVHNode<T> node = stack.Pop();

            // Skip nodes whose bounds are farther than the current closest distance
            if (node == null || node.Bounds.SqrDistance(point) > closestDistance * closestDistance)
                continue;

            // Check all elements in the leaf node
            if (node.IsLeaf)
            {
                foreach (var element in node.Elements)
                {
                    float distance = element.Distance(point);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestElement = element;
                    }
                }
            }

            // Push child nodes onto the stack
            else
            {
                if (node.Right != null) stack.Push(node.Right);
                if (node.Left != null) stack.Push(node.Left);
            }
        }

        return closestElement;
    }

    public void DrawGizmos()
    {
        if (Root == null) return;

        Stack<BVHNode<T>> stack = new Stack<BVHNode<T>>();
        stack.Push(Root);

        while (stack.Count > 0)
        {
            BVHNode<T> node = stack.Pop();

            if (node == null) continue;

            Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
            Gizmos.DrawWireCube(node.Bounds.center, node.Bounds.size);

            if (node.IsLeaf)
            {
                foreach (var element in node.Elements)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(element.GetBounds().center, 0.1f);
                }
            }
            else
            {
                if (node.Right != null) stack.Push(node.Right);
                if (node.Left != null) stack.Push(node.Left);
            }
        }
    }
}

public class BVHEdge : IBVHElement
{
    public Vector2 Start { get; private set; }
    public Vector2 End { get; private set; }

    private Vector2 dir;
    private Vector2 dirTrp;
    private float lengthSq;

    public BVHEdge(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
        dir = End - Start;
        dirTrp = new Vector2(dir.y, -dir.x);
        lengthSq = dir.sqrMagnitude;
    }

    public Bounds GetBounds()
    {
        Vector3 min = Vector2.Min(Start, End);
        Vector3 max = Vector2.Max(Start, End);
        return new Bounds((min + max) * 0.5f, max - min);
    }

    public float Distance(Vector2 point)
    {
        Vector2 pointDir = point - Start;
        float t = Mathf.Clamp01(Vector2.Dot(pointDir, dir) / lengthSq);
        Vector2 projection = Start + t * dir;
        return (point - projection).magnitude;
    }

    public float SignedDistance(Vector2 point)
    {
        // Find distance to edge with negative if inside
        Vector2 pointDir = point - Start;
        float t = Mathf.Clamp01(Vector2.Dot(pointDir, dir) / lengthSq);
        Vector2 projection = Start + t * dir;
        return (point - projection).magnitude * (Vector2.Dot(pointDir, dirTrp) < 0 ? -1 : 1);
    }

    public Vector2 ClosestPoint(Vector2 point)
    {
        Vector2 pointDir = point - Start;
        float t = Mathf.Clamp01(Vector2.Dot(pointDir, dir) / lengthSq);
        return Start + t * dir;
    }
}
