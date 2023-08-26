
using System;
using UnityEngine;


[Serializable]
class InteractionPluck : Interaction
{
    public bool isPlucked { get; private set; }

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
        isEnabled = true;
    }


    public void Update()
    {
        // Decrease timer and pluck once done
        if (isActive)
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
        if (isPlucked) return;
        isActive = true;
        pluckTimer = 0.0f;
        this.IInteractor = IInteractor;
        this.IInteractor.SetInteracting(true);
    }

    protected override void OnInputUp(IInteractor IInteractor)
    {
        // Stop timer decreasing and reset variables
        if (isPlucked) return;
        isActive = false;
        pluckTimer = 0.0f;
        if (this.IInteractor != null)
        {
            isActive = false;
            this.IInteractor.SetSqueezeAmount(0.0f);
            this.IInteractor.SetInteracting(false);
            this.IInteractor = null;
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
        IInteractor.SetSqueezeAmount(0.0f);
        IInteractor.SetInteracting(false);
        IInteractor = null;

        // Tell stone to pluck
        onPluck();
    }
}
