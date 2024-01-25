using System.Collections.Generic;
using UnityEngine;

public class ComposableObject : MonoBehaviour
{
    public Collider2D CL => _CL;
    public Bounds Bounds => CL.bounds;
    public Vector2 Position => transform.position;

    public bool HasPart<T>() where T : Part
    {
        return parts.ContainsKey(typeof(T));
    }

    public T GetPart<T>() where T : Part
    {
        return (T)parts.GetValueOrDefault(typeof(T), null);
    }

    public bool RequirePart<T>() where T : Part
    {
        if (!HasPart<T>()) throw new System.Exception("ComponentControllable requires " + typeof(T).ToString() + ".");
        return true;
    }

    protected void AddPart<T>() where T : Part
    {
        if (HasPart<T>()) return;
        Part part = gameObject.AddComponent<T>();
        parts[typeof(T)] = part;
        part.InitPart(this);
    }

    protected void RemovePart<T>() where T : Part
    {
        if (!HasPart<T>()) return;
        parts[typeof(T)].DeinitPart();
        GameObject.DestroyImmediate(parts[typeof(T)]);
        parts[typeof(T)] = null;
    }

    [Header("References")]
    [SerializeField] private Collider2D _CL;
    private Dictionary<System.Type, Part> parts = new Dictionary<System.Type, Part>();

    private void Awake()
    {
        Part[] existingParts = GetComponents<Part>();
        foreach (Part part in existingParts)
        {
            Debug.Log(part.GetType());
            parts[part.GetType()] = part;
            part.InitPart(this);
        }
    }
}
