
using System.Collections.Generic;
using UnityEngine;


public class WorldObject : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] protected Collider2D cl;

    [Header("Config")]
    [SerializeField] protected float controlDrag = 20.0f;
    [SerializeField] protected float idleDrag = 0.5f;
    [SerializeField] protected float density = 1.0f;
    [SerializeField] private bool _hasComponentHighlight = false;
    [SerializeField] private bool _hasComponentPhysical = false;
    [SerializeField] private bool _hasComponentControl  = false;
    
    public OutlineController highlightOutline { get; protected set; } = null;
    public Rigidbody2D physicalRB { get; protected set; } = null;
    public bool isHovered { get; private set; } = false;
    public bool isControlled { get; private set; } = false;
    public bool canControl { get; private set; } = false;
    public bool hasComponentHighlight => _hasComponentHighlight;
    public bool hasComponentPhysical => _hasComponentPhysical;
    public bool hasComponentControl => _hasComponentControl;

    public bool CanControl => canControl;

    protected List<Interaction> interactions = new List<Interaction>();
    protected GravityObject physicalGR = null;
    protected Vector2 controlPosition = Vector2.zero;
    protected float controlForce = 0.0f;
    protected float controlAngle = 0.0f;


    private void Awake()
    {
        DetectComponents();
    }


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
        if (!hasComponentHighlight) return;
        if (this.isHovered == isHovered) return;

        // Update variables
        this.isHovered = isHovered;
        highlightOutline.enabled = this.isHovered;
    } 

    public void SetCanControl(bool canControl)
    {
        if (!hasComponentControl) return;

        // Update variables
        this.canControl = canControl;
    }

    public void SetControlAngle(float controlAngle) => this.controlAngle = controlAngle;

    public bool SetControlled(bool isControlled)
    {
        if (!hasComponentControl) return false;
        if (isControlled == this.isControlled) return false;
        if (isControlled && !canControl) return false;

        // Update variables
        this.isControlled = isControlled;
        physicalRB.drag = isControlled ? controlDrag : idleDrag;
        physicalGR.isEnabled = !isControlled;
        return true;
    }

    public void OnControl() => SetControlled(true);

    public void OnDrop() => SetControlled(false);


    [ContextMenu("Detect Components")]
    private void DetectComponents()
    {
        highlightOutline = gameObject.GetComponent<OutlineController>();
        physicalRB = gameObject.GetComponent<Rigidbody2D>();
        physicalGR = gameObject.GetComponent<GravityObject>();
        if (highlightOutline) _hasComponentHighlight = true;
        if (physicalRB && physicalGR) _hasComponentPhysical = true;
    }

    [ContextMenu("Init Highlight")]
    protected void InitComponentHighlight()
    {
        if (highlightOutline != null) return;
        highlightOutline = gameObject.AddComponent<OutlineController>();
        highlightOutline.enabled = false;
        _hasComponentHighlight = true;
    }

    [ContextMenu("Init Physical")]
    protected void InitComponentPhysical()
    {
        if (physicalRB != null || physicalGR != null) return;
        _hasComponentPhysical = true;
        physicalRB = gameObject.AddComponent<Rigidbody2D>();
        physicalGR = gameObject.AddComponent<GravityObject>();
        physicalRB.gravityScale = 0;
        physicalGR.isEnabled = true;
        physicalGR.rb = physicalRB;
        cl.isTrigger = false;
        physicalRB.useAutoMass = true;
        physicalRB.useAutoMass = false;
        physicalRB.mass *= density;
    }

    [ContextMenu("Init Control")]
    protected void InitComponentControl()
    {
        _hasComponentControl = true;
    }

    [ContextMenu("Clear Components")]
    private void ClearComponents()
    {
        DetectComponents();
        if (hasComponentHighlight) ClearComponentHighlight();
        if (hasComponentPhysical) ClearComponentPhysical();
        if (hasComponentControl) ClearComponentControl();
    }
    
    protected void ClearComponentHighlight()
    {
        if (highlightOutline != null) DestroyImmediate(highlightOutline);
        _hasComponentHighlight = false;
    }

    protected void ClearComponentPhysical()
    {
        if (physicalRB != null) DestroyImmediate(physicalRB);
        if (physicalGR != null) DestroyImmediate(physicalGR);
        _hasComponentPhysical = false;
    }

    protected void ClearComponentControl()
    {
        if (isControlled) SetControlled(false);
        _hasComponentControl = false;
    }

    protected void FixedUpdate()
    {
        FixedUpdateControl();
    }
    
    private void FixedUpdateControl()
    {
        if (isControlled)
        {
            // Move towards target
            Vector2 dir = controlPosition - (Vector2)physicalRB.transform.position;
            physicalRB.AddForce(dir * controlForce);

            // Angle towards desired
            physicalRB.AddTorque((physicalRB.transform.eulerAngles.y - controlAngle) * controlForce);
        }
    }
}
