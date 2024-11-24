using System.Collections.Generic;
using UnityEngine;

public class CompositeObject : MonoBehaviour
{
    public static List<CompositeObject> objects = new List<CompositeObject>();
    public Collider2D CL => _CL;
    public Vector2 Position => Transform.position;
    public Bounds Bounds => CL.bounds;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public bool CanTarget { get; private set; } = true;

    public static List<CompositeObject> FindObjectsWithPart<T>() where T : Part
    {
        List<CompositeObject> objects = new List<CompositeObject>();
        foreach (CompositeObject obj in CompositeObject.objects)
        {
            if (obj.HasPart<T>()) objects.Add(obj);
        }
        return objects;
    }

    public static List<T> FindParts<T>() where T : Part
    {
        List<T> parts = new List<T>();
        foreach (CompositeObject obj in objects)
        {
            if (obj.HasPart<T>()) parts.Add(obj.GetPart<T>());
        }
        return parts;
    }

    public Vector2[] GetAlignedBoundCorners(Vector2 up)
    {
        Vector2 right = new Vector2(up.y, -up.x);

        // Get the collider points in world space
        Vector2[] points = Utility.GetWorldSpacePoints(CL);

        // Project points onto the camera's axes
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var point in points)
        {
            float projX = Vector2.Dot(point, right);
            float projY = Vector2.Dot(point, up);
            if (projX < minX) minX = projX;
            if (projX > maxX) maxX = projX;
            if (projY < minY) minY = projY;
            if (projY > maxY) maxY = projY;
        }

        // Calculate corners of the oriented bounding box
        Vector2 bottomLeft = (right * minX) + (up * minY);
        Vector2 bottomRight = (right * maxX) + (up * minY);
        Vector2 topLeft = (right * minX) + (up * maxY);
        Vector2 topRight = (right * maxX) + (up * maxY);

        return new Vector2[] { topLeft, topRight, bottomLeft, bottomRight };
    }

    public void SetCanTarget(bool value) => CanTarget = value;

    public bool HasPart<T>() where T : Part
    {
        return parts.ContainsKey(typeof(T));
    }

    public T GetPart<T>() where T : Part
    {
        return (T)parts.GetValueOrDefault(typeof(T), null);
    }

    public bool TryGetPart<T>(out T part) where T : Part
    {
        part = GetPart<T>();
        return part != null;
    }

    public bool RequirePart<T>() where T : Part
    {
        if (!HasPart<T>()) throw new System.Exception("CompositeObject requires " + typeof(T).ToString() + ".");
        return true;
    }

    protected T AddPart<T>() where T : Part
    {
        if (HasPart<T>()) return GetPart<T>();
        Part part = gameObject.AddComponent<T>();
        parts[typeof(T)] = part;
        part.InitPart(this);
        return (T)part;
    }

    protected void RemovePart<T>() where T : Part
    {
        if (!HasPart<T>()) return;
        parts[typeof(T)].DeinitPart();
        GameObject.DestroyImmediate(parts[typeof(T)]);
        parts[typeof(T)] = null;
    }

    protected virtual void Awake()
    {
        Part[] existingParts = GetComponents<Part>();
        foreach (Part part in existingParts)
        {
            parts[part.GetType()] = part;
            part.InitPart(this);
        }
        objects.Add(this);
    }

    [Header("References")]
    [SerializeField] private Collider2D _CL;
    private Dictionary<System.Type, Part> parts = new Dictionary<System.Type, Part>();
}
