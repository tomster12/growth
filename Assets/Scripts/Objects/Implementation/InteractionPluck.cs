
using System;
using UnityEngine;


[Serializable]
class InteractionPluck : Interaction
{
    public bool IsPlucked { get; private set; }

    private Action onPluck;
    private float pluckTimerMax;
    private IInteractor IInteractor;
    private float pluckTimer;


    public InteractionPluck(Action onPluck, float pluckTimerMax) : base("Pluck", InteractionInput.LMB, Visibility.ICON, "up")
    {
        // Initialize variables
        this.onPluck = onPluck;
        this.pluckTimerMax = pluckTimerMax;

        // Initialize as enabled
        IsEnabled = true;
    }


    public void Update()
    {
        // Decrease timer and pluck once done
        if (IsActive)
        {
            pluckTimer = Mathf.Min(pluckTimer + Time.deltaTime, pluckTimerMax);
            IInteractor.SetInteractionEmphasis(0.8f * pluckTimer / pluckTimerMax);
            if (pluckTimer >= pluckTimerMax) Pluck();
        }
        else pluckTimer = 0.0f;
    }


    protected override void OnInputDown(IInteractor IInteractor)
    {
        // Begin timer decreasing and update variables
        if (IsPlucked) return;
        IsActive = true;
        pluckTimer = 0.0f;
        this.IInteractor = IInteractor;
        this.IInteractor.SetInteraction(this);
    }

    protected override void OnInputUp(IInteractor IObjectController)
    {
        if (IsPlucked) return;

        // Stop timer decreasing and reset variables
        IsActive = false;
        pluckTimer = 0.0f;

        if (this.IInteractor != null)
        {
            IsActive = false;
            this.IInteractor.SetInteractionEmphasis(0.0f);
            this.IInteractor.SetInteraction(null);
            this.IInteractor = null;
        }
    }

    private void Pluck()
    {
        if (IsPlucked) return;

        // Update interaction variables
        IsPlucked = true;
        IsActive = false;
        IsEnabled = false;
        pluckTimer = 0.0f;
        IInteractor.SetInteractionEmphasis(0.0f);
        IInteractor.SetInteraction(null);
        IInteractor = null;

        // Tell stone to pluck
        onPluck();
    }
}
