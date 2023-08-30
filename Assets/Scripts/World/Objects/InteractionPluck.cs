
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
            IInteractor.SetSqueezeAmount(0.8f * pluckTimer / pluckTimerMax);
            if (pluckTimer >= pluckTimerMax) OnPluck();
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
        this.IInteractor.SetInteracting(true);
    }

    protected override void OnInputUp(IInteractor IInteractor)
    {
        // Stop timer decreasing and reset variables
        if (IsPlucked) return;
        IsActive = false;
        pluckTimer = 0.0f;
        if (this.IInteractor != null)
        {
            IsActive = false;
            this.IInteractor.SetSqueezeAmount(0.0f);
            this.IInteractor.SetInteracting(false);
            this.IInteractor = null;
        }
    }

    private void OnPluck()
    {
        if (IsPlucked) return;

        // Update interaction variables
        IsPlucked = true;
        IsActive = false;
        IsEnabled = false;

        pluckTimer = 0.0f;
        IInteractor.SetSqueezeAmount(0.0f);
        IInteractor.SetInteracting(false);
        IInteractor = null;

        // Tell stone to pluck
        onPluck();
    }
}
