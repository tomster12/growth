
using UnityEngine;


public class PluckableStone : WorldObject
{
    public bool isPlucked { get; private set; }

    Interaction interactionPluck;


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
    }
}
