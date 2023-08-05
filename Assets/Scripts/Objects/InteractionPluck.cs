
using System;
using UnityEngine;


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
