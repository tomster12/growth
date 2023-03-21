
using System;
using UnityEngine;


public class PluckableStone : WorldObject
{
    [Header("Pluck Config")]
    [SerializeField] private float pluckTimerMax = 1.0f;
    [SerializeField] private float pluckVelocity = 30.0f;
    [SerializeField] private GameObject pluckPsysPfb;

    private InteractionPluck interactionPluck;

    [HideInInspector] public Vector2 popDir;


    protected void Awake()
    {
        InitComponentHighlight();
        InitComponentControl();
    }

    protected void Start()
    {
        SetCanControl(false);
        interactionPluck = new InteractionPluck(OnPluck, pluckTimerMax);
        interactions.Add(interactionPluck);
    }


    private void Update()
    {
        interactionPluck.Update();
    }


    private void OnPluck()
    {
        // Become physical and controllable
        InitComponentPhysical();
        SetCanControl(true);

        // Move out of ground
        physicalRB.transform.position += (Vector3)(popDir.normalized * cl.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        physicalRB.AddForce(popDir.normalized * pluckVelocity * physicalRB.mass, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = physicalRB.transform.position;
        particlesGO.transform.up = popDir.normalized;
    }
}


[Serializable]
class InteractionPluck : Interaction
{
    private Action onPluck;
    private float pluckTimerMax;

    public bool isPlucked { get; private set; }
    private IInteractor interactorI;
    private float pluckTimer;


    public InteractionPluck(Action onPluck, float pluckTimerMax) : base("Pluck", InteractionInput.LMB, Visibility.ICON, "up")
    {
        // Initialize variables
        this.onPluck = onPluck;
        this.pluckTimerMax = pluckTimerMax;

        // Initialize as enabled
        isEnabled = true;
    }


    public void Update()
    {
        // Decrease timer and pluck once done
        if (isActive)
        {
            pluckTimer = Mathf.Min(pluckTimer + Time.deltaTime, pluckTimerMax);
            interactorI.Interaction_SetSqueezeAmount(0.8f * pluckTimer / pluckTimerMax);
            if (pluckTimer >= pluckTimerMax) OnPluck();
        }
        else pluckTimer = 0.0f;
    }


    protected override void OnInputDown(IInteractor interactorI)
    {
        // Begin timer decreasing and update variables
        if (isPlucked) return;
        isActive = true;
        pluckTimer = 0.0f;
        this.interactorI = interactorI;
        this.interactorI.Interaction_SetInteracting(true);
    }

    protected override void OnInputUp(IInteractor interactorI)
    {
        // Stop timer decreasing and reset variables
        if (isPlucked) return;
        isActive = false;
        pluckTimer = 0.0f;
        if (this.interactorI != null)
        {
            isActive = false;
            this.interactorI.Interaction_SetSqueezeAmount(0.0f);
            this.interactorI.Interaction_SetInteracting(false);
            this.interactorI = null;
        }
    }


    private void OnPluck()
    {
        if (isPlucked) return;

        // Update interaction variables
        isPlucked = true;
        isActive = false;
        isEnabled = false;

        pluckTimer = 0.0f;
        interactorI.Interaction_SetSqueezeAmount(0.0f);
        interactorI.Interaction_SetInteracting(false);
        interactorI = null;

        // Tell stone to pluck
        onPluck();
    }
}
