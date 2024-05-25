using System;
using UnityEditorInternal;
using UnityEngine;

[Serializable]
public class InteractionPluck : Interaction
{
    private static float INTERACTOR_SLOWDOWN = 0.85f;

    public InteractionPluck(CompositeObject targetCO, Action onPluck, float pluckTimerDuration) : base("Pluck", "up", InteractionInput.Mouse(0))
    {
        this.targetCO = targetCO;
        this.onPluck = onPluck;
        this.pluckTimerDuration = pluckTimerDuration;
        IsEnabled = true;
    }

    public bool IsPlucked { get; private set; }

    public override bool CanInteract(IInteractor interactor) => base.CanInteract(interactor) && !IsPlucked;

    public override void Update()
    {
        if (!IsActive) return;

        // Decrease timer
        pluckTimer = Mathf.Min(pluckTimer + Time.deltaTime, pluckTimerDuration);
        if (pluckTimer >= pluckTimerDuration)
        {
            CompletePluck();
            return;
        }

        // Update interactor
        float t = pluckTimer / pluckTimerDuration;
        interactor.SetInteractionEmphasis(0.8f * t);
        interactor.SetInteractionPulling(targetCO.Position, t);
    }

    public override void StartInteracting(IInteractor interactor)
    {
        base.StartInteracting(interactor);

        // Reset variables
        pluckTimer = 0.0f;
        interactor.SetInteractionSlowdown(INTERACTOR_SLOWDOWN);
    }

    public override void StopInteracting()
    {
        base.StopInteracting();

        // Reset variables
        pluckTimer = 0.0f;
    }

    private CompositeObject targetCO;
    private Action onPluck;
    private float pluckTimerDuration;
    private float pluckTimer;

    private void CompletePluck()
    {
        base.StopInteracting();

        // Reset variables call callback
        IsEnabled = false;
        IsPlucked = true;
        pluckTimer = 0.0f;
        onPluck();
    }
}
