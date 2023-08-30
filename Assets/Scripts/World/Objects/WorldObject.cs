
using System.Collections.Generic;
using UnityEngine;


public class WorldObject : MonoBehaviour, IRichObject
{
    [Header("References")]
    [SerializeField] protected Collider2D cl;

    [Header("Config")]
    [SerializeField] protected float controlDrag = 20.0f;
    [SerializeField] protected float idleDrag = 0.5f;
    [SerializeField] protected float density = 1.0f;
    [SerializeField] private bool hasComponentHighlight = false;
    [SerializeField] private bool hasComponentPhysical = false;
    [SerializeField] private bool hasComponentControl = false;
    
    public OutlineController HighlightOutline { get; protected set; } = null;
    public Rigidbody2D RB { get; protected set; } = null;
    public bool IsHovered { get; private set; } = false;
    public bool IsControlled { get; private set; } = false;
    public bool CanControl { get; private set; } = false;
    public bool HasComponentHighlight => hasComponentHighlight;
    public bool HasComponentPhysical => hasComponentPhysical;
    public bool HasComponentControl => hasComponentControl;

    protected List<Interaction> interactions = new List<Interaction>();
    protected GravityObject physicalGR = null;
    protected Vector2 controlPosition = Vector2.zero;
    protected float controlForce = 0.0f;
    protected float controlAngle = 0.0f;


    public List<Interaction> GetInteractions() => interactions;

    public Bounds GetHoverBounds() => cl.bounds;

    public Vector2 GetPosition() => transform.position;

    public void SetControlPosition(Vector3 pos, float force)
    {
        controlPosition = pos;
        controlForce = force;
    }

    public void SetHovered(bool isHovered)
    {
        if (!HasComponentHighlight) return;
        if (this.IsHovered == isHovered) return;

        // Update variables
        this.IsHovered = isHovered;
        HighlightOutline.enabled = this.IsHovered;
    } 

    public void SetCanControl(bool canControl)
    {
        if (!HasComponentControl) return;

        // Update variables
        this.CanControl = canControl;
    }

    public void SetControlAngle(float controlAngle) => this.controlAngle = controlAngle;

    public bool SetControlled(bool isControlled)
    {
        if (!HasComponentControl) return false;
        if (isControlled == this.IsControlled) return false;
        if (isControlled && !CanControl) return false;

        // Update variables
        this.IsControlled = isControlled;
        RB.drag = isControlled ? controlDrag : idleDrag;
        physicalGR.IsEnabled = !isControlled;
        return true;
    }

    public void OnControl() => SetControlled(true);

    public void OnDrop() => SetControlled(false);


    private void Awake()
    {
        DetectComponents();
    }

    [ContextMenu("Detect Components")]
    private void DetectComponents()
    {
        HighlightOutline = gameObject.GetComponent<OutlineController>();
        RB = gameObject.GetComponent<Rigidbody2D>();
        physicalGR = gameObject.GetComponent<GravityObject>();
        if (HighlightOutline) hasComponentHighlight = true;
        if (RB && physicalGR) hasComponentPhysical = true;
    }

    [ContextMenu("Init Highlight")]
    protected void InitComponentHighlight()
    {
        if (HighlightOutline != null) return;
        HighlightOutline = gameObject.AddComponent<OutlineController>();
        HighlightOutline.enabled = false;
        hasComponentHighlight = true;
    }

    [ContextMenu("Init Physical")]
    protected void InitComponentPhysical()
    {
        if (RB != null || physicalGR != null) return;
        hasComponentPhysical = true;
        RB = gameObject.AddComponent<Rigidbody2D>();
        physicalGR = gameObject.AddComponent<GravityObject>();
        RB.gravityScale = 0;
        physicalGR.IsEnabled = true;
        physicalGR.rb = RB;
        cl.isTrigger = false;
        RB.useAutoMass = true;
        RB.useAutoMass = false;
        RB.mass *= density;
    }

    [ContextMenu("Init Control")]
    protected void InitComponentControl()
    {
        hasComponentControl = true;
    }

    [ContextMenu("Clear Components")]
    private void ClearComponents()
    {
        DetectComponents();
        if (HasComponentHighlight) ClearComponentHighlight();
        if (HasComponentPhysical) ClearComponentPhysical();
        if (HasComponentControl) ClearComponentControl();
    }
    
    protected void ClearComponentHighlight()
    {
        if (HighlightOutline != null) DestroyImmediate(HighlightOutline);
        hasComponentHighlight = false;
    }

    protected void ClearComponentPhysical()
    {
        if (RB != null) DestroyImmediate(RB);
        if (physicalGR != null) DestroyImmediate(physicalGR);
        hasComponentPhysical = false;
    }

    protected void ClearComponentControl()
    {
        if (IsControlled) SetControlled(false);
        hasComponentControl = false;
    }

    protected void FixedUpdate()
    {
        FixedUpdateControl();
    }
    
    private void FixedUpdateControl()
    {
        if (IsControlled)
        {
            // Move towards target
            Vector2 dir = controlPosition - (Vector2)RB.transform.position;
            RB.AddForce(dir * controlForce);

            // Angle towards desired
            RB.AddTorque((RB.transform.eulerAngles.y - controlAngle) * controlForce);
        }
    }
}
