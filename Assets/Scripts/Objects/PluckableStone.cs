
using UnityEngine;


public class PluckableStone : WorldObject
{
    private static float popForce = 125.0f;

    [SerializeField] private GameObject pluckPSPfb;
    
    public bool isPlucked { get; private set; }

    private Interaction interactionPluck;
    public Vector2 popDir;


    protected void Awake()
    {
        InitComponentHighlight();
        InitComponentControl();
    }

    protected void Start()
    {
        // Initialize interaction
        interactionPluck = new Interaction(true, Interaction.Visibility.FULL, "Pluck", InteractionInput.LMB, null, OnPluck, null);
        interactions.Add(interactionPluck);
        
        // Initialize variables
        isPlucked = false;
        SetCanControl(false);
    }


    public void OnPluck()
    {
        if (isPlucked) return;

        // Update variables
        isPlucked = true;
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
