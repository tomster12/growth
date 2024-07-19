using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompositeObject : MonoBehaviour
{
    public static List<CompositeObject> objects = new List<CompositeObject>();
    public Collider2D CL => _CL;
    public Bounds Bounds => CL.bounds;
    public Vector2 Position => Transform.position;
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
