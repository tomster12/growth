using System;
using System.Net;
using UnityEngine;
using static PlayerInteractor;

[Serializable]
internal class InteractionPluck : Interaction
{
    public InteractionPluck(Action onPluck, float pluckTimerMax) : base("Pluck", InteractionInput.Mouse(0), "up")
    {
        this.onPluck = onPluck;
        this.pluckTimerMax = pluckTimerMax;
        IsEnabled = true;
    }

    public bool IsPlucked { get; private set; }
    public override bool CanInteract => base.CanInteract && !IsPlucked;

    public override void Update()
    {
        if (!IsActive) return;

        // Decrease timer and update player
        pluckTimer = Mathf.Min(pluckTimer + Time.deltaTime, pluckTimerMax);

        // Pluck when done
        if (pluckTimer >= pluckTimerMax) CompletePluck();

        /* TODO: Update interactor
        PlayerInteractor.InteractionEmphasis = 0.8f * pluckTimer / pluckTimerMax;
        PlayerController.OverrideVerticalLean = pluckTimer / pluckTimerMax;
        // Point and draw line
        float upAmount = 0.2f + 0.2f * Utility.Easing.EaseOutSine(pluckTimer / pluckTimerMax);
        Vector2 pathStartRaw = TargetComposable.Position; // PlayerLegs.GetLegEnd(PlayerLegs.PointingLeg); TODO
        Vector2 pathEndRaw = TargetComposable.Position;
        Vector3 pathStart = new Vector3(pathStartRaw.x, pathStartRaw.y, PlayerController.Transform.position.z + 0.1f);
        Vector3 pathEnd = new Vector3(pathEndRaw.x, pathEndRaw.y, PlayerController.Transform.position.z + 0.1f);
        Vector3 controlStart = PlayerController.Transform.position;
        Vector3 controlDir = pathEnd - controlStart;
        Vector3 controlUp = PlayerController.UpDir.normalized * (pathEnd - pathStart).magnitude * upAmount;
        Vector3 controlPoint = controlStart + controlDir * 0.75f + controlUp;
        TargetDirLH.DrawCurve(pathStart, pathEnd, controlPoint, LegDirInteractColor);
        //PlayerLegs.PointingPos = controlPoint; TODO
        */
    }

    public override void StartInteracting(IInteractor interactor)
    {
        base.StartInteracting(interactor);

        // Initialize pluck timer
        pluckTimer = 0.0f;

        /* TODO: Update interactor
        // Calculate direction of target
        float rightPct = Vector2.Dot(PlayerController.RightDir, TargetComposable.Position - (Vector2)PlayerController.Transform.position);
        PlayerController.MovementSlowdown = InteractSlowdown;
        //PlayerLegs.IsPointing = true; TODO
        //PlayerLegs.PointingLeg = rightPct > 0.0f ? 2 : 1; TODO
        TargetDirLH.SetActive(true);
        */
    }

    public override void StopInteracting(IInteractor interactor)
    {
        base.StopInteracting(interactor);

        // Reset pluck timer
        pluckTimer = 0.0f;

        /* TODO: Update interactor
        // Reset variables to stopped
        //PlayerLegs.IsPointing = false; TODO
        PlayerInteractor.InteractionEmphasis = 0.0f;
        PlayerController.MovementSlowdown = 0.0f;
        PlayerController.OverrideVerticalLean = 0.0f;
        TargetDirLH.SetActive(false);
        */
    }

    private Action onPluck;
    private float pluckTimerMax;
    private float pluckTimer;

    private void CompletePluck()
    {
        // Finished so update variables
        IsEnabled = false;
        IsActive = false;
        IsPlucked = true;
        pluckTimer = 0.0f;
        onPluck();

        /* TODO: Update interactor
        // Reset variables to finished
        //PlayerLegs.IsPointing = false; TODO
        PlayerInteractor.InteractionEmphasis = 0.0f;
        PlayerController.MovementSlowdown = 0.0f;
        PlayerController.OverrideVerticalLean = 0.0f;
        TargetDirLH.SetActive(false);
        */
    }
}
