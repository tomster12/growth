
using System.Collections.Generic;
using UnityEngine;


public class ComposableObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D _CL;

    public PartPhysical PartPhysical { get; private set; }
    public PartHighlightable PartHighlightable { get; private set; }
    public PartControllable PartControllable { get; private set; }
    public PartInteractable PartInteractable { get; private set; }
    public bool HasPartPhysical => PartPhysical != null;
    public bool HasPartHighlightable => PartHighlightable != null;
    public bool HasPartControllable => PartControllable != null;
    public bool HasPartInteractable => PartInteractable != null;
    public Collider2D CL => _CL;
    public Bounds Bounds => CL.bounds;
    public Vector2 Position => transform.position;


    private void Awake()
    {
        PartPhysical = gameObject.GetComponent<PartPhysical>();
        PartHighlightable = gameObject.GetComponent<PartHighlightable>();
        PartControllable = gameObject.GetComponent<PartControllable>();
        PartInteractable = gameObject.GetComponent<PartInteractable>();
        PartPhysical?.InitPart(this);
        PartHighlightable?.InitPart(this);
        PartControllable?.InitPart(this);
        PartInteractable?.InitPart(this);
    }


    protected void AddPartPhysical()
    {
        if (HasPartPhysical) return;
        PartPhysical ??= gameObject.AddComponent<PartPhysical>();
        PartPhysical.InitPart(this);
    }

    protected void AddPartHighlightable()
    {
        if (HasPartHighlightable) return;
        PartHighlightable ??= gameObject.AddComponent<PartHighlightable>();
        PartHighlightable.InitPart(this);
    }

    protected void AddPartControllable()
    {
        if (HasPartControllable) return;
        PartControllable ??= gameObject.AddComponent<PartControllable>();
        PartControllable.InitPart(this);
    }

    protected void AddPartInteractable()
    {
        if (HasPartInteractable) return;
        PartInteractable ??= gameObject.AddComponent<PartInteractable>();
        PartInteractable.InitPart(this);
    }

    protected void RemovePartPhysical()
    {
        if (!HasPartPhysical) return;
        PartPhysical.DeinitPart();
        GameObject.DestroyImmediate(PartPhysical);
    }

    protected void RemovePartHighlightable()
    {
        if (!HasPartHighlightable) return;
        PartHighlightable.DeinitPart();
        GameObject.DestroyImmediate(PartHighlightable);
    }

    protected void RemovePartControllable()
    {
        if (!HasPartControllable) return;
        PartControllable.DeinitPart();
        GameObject.DestroyImmediate(PartControllable);
    }

    protected void RemovePartInteractable()
    {
        if (!HasPartInteractable) return;
        PartInteractable.DeinitPart();
        GameObject.DestroyImmediate(PartInteractable);
    }
}
