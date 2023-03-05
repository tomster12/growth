
using System.Collections.Generic;
using UnityEngine;


public class WorldObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Collider2D cl;

    protected List<Interaction> interactions = new List<Interaction>();


    protected virtual void Awake()
    {
        InitComponents();
    }

    [ContextMenu("Init Components")]
    private void InitComponents()
    {
        if (hasComponentHighlight) InitComponentHighlight();
        if (hasComponentPhysical) InitComponentPhysical();
        if (hasComponentPickup) InitComponentPickup();
    }

    [ContextMenu("Clear Components")]
    private void ClearComponents()
    {
        if (hasComponentHighlight) ClearComponentHighlight();
        if (hasComponentPhysical) ClearComponentPhysical();
        if (hasComponentPickup) ClearComponentPickup();
    }


    protected void FixedUpdate()
    {
        // If controlled move towards target
        if (isControlled)
        {
            controlPosition = PlayerInteractor.instance.hoverPos;
            Vector2 dir = controlPosition - (Vector2)physicalRB.transform.position;
            physicalRB.AddForce(dir * controlForce);
        }
    }

    
    public Bounds GetHighlightBounds() => cl.bounds;

    public List<Interaction> GetInteractions() => interactions;


    #region Component: Highlight

    [Header("Component: Highlight")]
    [SerializeField] protected bool hasComponentHighlight;
    [SerializeField] protected OutlineController highlightOutline;
    public bool isHovered { get; private set; }


    protected void InitComponentHighlight()
    {
        if (hasComponentHighlight) return;

        // Initialize variables
        hasComponentHighlight = true;
        highlightOutline = gameObject.AddComponent<OutlineController>();
        isHovered = false;
    }

    protected void ClearComponentHighlight()
    {
        if (!hasComponentHighlight) return;

        // Deinitialize variables
        hasComponentHighlight = false;
        GameObject.Destroy(highlightOutline);
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

    [Header("Component: Physical")]
    [SerializeField] protected bool hasComponentPhysical;
    [SerializeField] protected Rigidbody2D physicalRB;
    [SerializeField] protected GravityObject physicalGO;


    protected void InitComponentPhysical()
    {
        if (hasComponentPhysical) return;

        // Create physical components
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
        Destroy(physicalRB);
        Destroy(physicalGO);
    }

    #endregion


    #region Component: Pickup

    [Header("Component: Pickup")]
    [SerializeField] protected bool hasComponentPickup;
    [Space(15)]
    [SerializeField] protected float controlForce = 30.0f;
    [SerializeField] protected float controlDrag = 20.0f;
    [SerializeField] protected float idleDrag = 0.5f;

    public bool isControlled { get; private set; }
    public bool canPickup { get; private set; }
    public Vector2 controlPosition;

    protected Interaction interactionPickup;
    protected Interaction interactionDrop;


    protected void InitComponentPickup()
    {
        hasComponentPickup = true;
        isControlled = false;
        canPickup = false;
        controlPosition = Vector2.zero;
    }

    protected void ClearComponentPickup()
    {
        if (isControlled) SetControlled(false);
        hasComponentPickup = false;
    }


    public void OnPickup() => SetControlled(true);
    public void OnDrop() => SetControlled(false);


    public void SetControlled(bool isControlled)
    {
        if (!hasComponentPickup) return;
        if (isControlled == this.isControlled) return;

        // Update variables
        this.isControlled = isControlled;
        physicalRB.drag = isControlled ? controlDrag : idleDrag;
        physicalGO.isEnabled = !isControlled;
        interactionPickup.isEnabled = !isControlled;
        interactionDrop.isEnabled = isControlled;
    }

    public void SetCanPickup(bool canPickup)
    {
        // Update variables
        this.canPickup = canPickup;
        interactionPickup.isEnabled = canPickup ? !isControlled : false;
    }

    #endregion
}
