
using System.Collections.Generic;
using UnityEngine;


public class WorldObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Collider2D cl;

    protected List<Interaction> interactions = new List<Interaction>();


    [ContextMenu("Clear Components")]
    private void ClearComponents()
    {
        if (hasComponentHighlight) ClearComponentHighlight();
        if (hasComponentPhysical) ClearComponentPhysical();
        if (hasComponentControl) ClearComponentControl();
    }


    protected void FixedUpdate()
    {
        FixedUpdateControl();
    }

    
    public Bounds GetHoverBounds() => cl.bounds;

    public List<Interaction> GetInteractions() => interactions;


    #region Component: Highlight

    public bool hasComponentHighlight { get; protected set; }
    public bool isHovered { get; private set; }

    protected OutlineController highlightOutline;


    protected void InitComponentHighlight()
    {
        if (hasComponentHighlight) return;

        // Initialize variables
        hasComponentHighlight = true;
        highlightOutline = gameObject.AddComponent<OutlineController>();
        highlightOutline.enabled = false;
        isHovered = false;
    }

    protected void ClearComponentHighlight()
    {
        if (!hasComponentHighlight) return;

        // Deinitialize variables
        hasComponentHighlight = false;
        DestroyImmediate(highlightOutline);
    }


    public void SetHovered(bool isHovered)
    {
        if (!hasComponentHighlight) return;
        if (this.isHovered == isHovered) return;

        // Update variables
        this.isHovered = isHovered;
        highlightOutline.enabled = this.isHovered;
    }

    #endregion


    # region Component: Physical

    public bool hasComponentPhysical { get; protected set; }

    public Rigidbody2D physicalRB { get; protected set; }
    protected GravityObject physicalGO;


    protected void InitComponentPhysical()
    {
        if (hasComponentPhysical) return;

        // Initialize variables
        hasComponentPhysical = true;
        physicalRB = gameObject.AddComponent<Rigidbody2D>();
        physicalGO = gameObject.AddComponent<GravityObject>();
        physicalRB.gravityScale = 0;
        physicalRB.useAutoMass = true;
        physicalGO.isEnabled = true;
        physicalGO.rb = physicalRB;
        cl.isTrigger = false;
    }

    protected void ClearComponentPhysical()
    {
        if (!hasComponentPhysical) return;

        // Deinitialize variables
        DestroyImmediate(physicalRB);
        DestroyImmediate(physicalGO);
        hasComponentPhysical = false;
    }

    #endregion


    #region Component: Control

    [Header("Component: Control - Config")]
    [SerializeField] protected float controlDrag = 20.0f;
    [SerializeField] protected float idleDrag = 0.5f;

    public bool hasComponentControl { get; protected set; }
    public bool isControlled { get; private set; }
    public bool canControl { get; private set; }
    public Vector2 controlPosition;
    public float controlForce;


    protected void InitComponentControl()
    {
        if (hasComponentControl) return;

        // Initialize variables
        hasComponentControl = true;
        isControlled = false;
        canControl = false;
        controlPosition = Vector2.zero;
    }

    protected void ClearComponentControl()
    {
        if (!hasComponentControl) return;

        // Deinitialize variables
        if (isControlled) SetControlled(false);
        hasComponentControl = false;
    }


    private void FixedUpdateControl()
    {
        // If controlled move towards target
        if (isControlled)
        {
            Vector2 dir = controlPosition - (Vector2)physicalRB.transform.position;
            physicalRB.AddForce(dir * controlForce);
        }
    }


    public void SetCanControl(bool canControl)
    {
        // Update variables
        this.canControl = canControl;
    }

    public void OnControl() => SetControlled(true);
    public void OnDrop() => SetControlled(false);

    public bool SetControlled(bool isControlled)
    {
        if (!hasComponentControl) return false;
        if (isControlled == this.isControlled) return false;
        if (isControlled && !canControl) return false;

        // Update variables
        this.isControlled = isControlled;
        physicalRB.drag = isControlled ? controlDrag : idleDrag;
        physicalGO.isEnabled = !isControlled;
        return true;
    }

    #endregion
}
