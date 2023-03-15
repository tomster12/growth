
using UnityEngine;


public class PluckableStone : WorldObject
{
    private static float popForce = 80.0f;

    [SerializeField] private float pluckTimerMax = 1.0f;
    [SerializeField] private GameObject pluckPSPfb;
    
    public bool isPlucked { get; private set; }
    public bool isPlucking { get; private set; }

    private Interaction interactionPluck;
    private IInteractor interactorI;
    private float pluckTimer;

    public Vector2 popDir;


    protected void Awake()
    {
        InitComponentHighlight();
        InitComponentControl();
    }

    protected void Start()
    {
        // Initialize interaction
        interactionPluck = new Interaction(true, Interaction.Visibility.FULL, "Pluck", InteractionInput.LMB, null, OnPluckDown, OnPluckUp);
        interactions.Add(interactionPluck);
        
        // Initialize variables
        isPlucked = false;
        SetCanControl(false);
    }


    private void Update()
    {
        if (isPlucking)
        {
            pluckTimer = Mathf.Min(pluckTimer + Time.deltaTime, pluckTimerMax);
            interactorI.Interaction_SetSqueezeAmount(pluckTimer / pluckTimerMax);
            if (pluckTimer >= pluckTimerMax) Pluck();
        }
        else pluckTimer = 0.0f;
    }


    public void OnPluckDown(IInteractor interactorI)
    {
        if (isPlucked) return;
        isPlucking = true;
        pluckTimer = 0.0f;
        this.interactorI = interactorI;
        this.interactorI.Interaction_SetInteracting(true);
    }

    public void OnPluckUp(IInteractor interactorI)
    {
        if (isPlucked) return;
        isPlucking = false;
        pluckTimer = 0.0f;
        this.interactorI.Interaction_SetSqueezeAmount(0.0f);
        this.interactorI.Interaction_SetInteracting(false);
        this.interactorI = null;
    }

    private void Pluck()
    {
        if (isPlucked) return;

        // Update variables
        isPlucked = true;
        isPlucking = false;
        
        pluckTimer = 0.0f;
        this.interactorI.Interaction_SetSqueezeAmount(0.0f);
        interactorI.Interaction_SetInteracting(false);
        interactorI = null;

        interactionPluck.isEnabled = false;
        InitComponentPhysical();
        SetCanControl(true);

        // Move out of ground
        physicalRB.transform.position += (Vector3)(popDir.normalized * cl.bounds.extents * 2f);

        // Pop in a direction
        physicalRB.AddForce(popDir.normalized * popForce);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPSPfb);
        particlesGO.transform.position = physicalRB.transform.position;
        particlesGO.transform.up = popDir.normalized;
    }
}
