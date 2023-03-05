
using System.Collections.Generic;
using UnityEngine;


public class ComponentControllable : ComponentHoverable, IInteractable
{
    [Header("Controllable References")]
    [SerializeField] private Rigidbody rb;

    [Header("Controllable Config")]
    [SerializeField] private float controlForce = 30.0f;
    [SerializeField] private float controlDrag = 20.0f;
    [SerializeField] private float idleDrag = 0.5f;

    public Vector2 targetPosition;
    public bool canControl { get; private set; }
    public bool isControlled { get; private set; }

    protected List<Interaction> interactions = new List<Interaction>();
    private Interaction interactionPickup;
    private Interaction interactionDrop;


    protected virtual void Start()
    {
        interactionPickup = new Interaction(true, Interaction.Visibility.FULL, "Pickup", OnPickup, InteractionInput.LMB);
        interactionDrop = new Interaction(true, Interaction.Visibility.FULL, "Drop", OnDrop, InteractionInput.LMB);
        interactions.Add(interactionPickup);
        interactions.Add(interactionDrop);
    }


    private void FixedUpdate()
    {
        // If controlled move towards target
        if (isControlled)
        {
            Vector2 dir = targetPosition - (Vector2)rb.transform.position;
            rb.AddForce(dir * controlForce);
        }
    }


    public void OnPickup() => SetControlled(true);
    public void OnDrop() => SetControlled(false);

    public void SetControlled(bool isControlled)
    {
        if (isControlled == this.isControlled) return;
        if (isControlled && canControl) return;

        // Update variables
        rb.drag = isControlled ? controlDrag : idleDrag;
        this.isControlled = isControlled;
        interactionDrop.isEnabled = isControlled;
        interactionPickup.isEnabled = !isControlled;
    }

    public void SetCanControl(bool canControl)
    {
        // Update variables
        this.canControl = canControl;
        interactionPickup.isEnabled = canControl ? !isControlled : false;
    }


    public List<Interaction> GetInteractions() => interactions;
}
