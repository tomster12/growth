
using System.Collections.Generic;
using UnityEngine;

public class PluckableStone : ComponentControllable
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    public bool isPlucked { get; private set; }

    Interaction interactionPluck;
    

    protected override void Start()
    {
        base.Start();

        // Initialize interaction
        interactionPluck = new Interaction(true, Interaction.Visibility.FULL, "Pluck", Pluck, InteractionInput.LMB);
        interactions.Add(interactionPluck);

        // Initialize variables
        canHover = true;
        SetCanControl(false);
    }

    
    public void Pluck()
    {
        // Pluck stone
        isPlucked = true;
        interactionPluck.isEnabled = false;
        SetCanControl(true);
    }
}
