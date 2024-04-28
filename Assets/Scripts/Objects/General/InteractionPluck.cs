using System;
using UnityEngine;
using static PlayerInteractor;

[Serializable]
internal class InteractionPluck : PlayerInteraction
{
    public InteractionPluck(Action onPluck, float pluckTimerMax) : base("Pluck", InteractionInput.LMB, Visibility.Icon, "up")
    {
        this.onPluck = onPluck;
        this.pluckTimerMax = pluckTimerMax;
        IsEnabled = true;
    }

    public bool IsPlucked { get; private set; }

    protected override void OnInputDown() => StartPluck();

    protected override void OnInputUp() => CancelPluck();

    protected override void UpdateAction() => UpdatePluck();

    private Action onPluck;
    private float pluckTimerMax;
    private float pluckTimer;

    private void StartPluck()
    {
        if (IsPlucked || IsActive) return;

        // Calculate direction of target
        float rightPct = Vector2.Dot(PlayerController.RightDir, TargetComposable.Position - (Vector2)PlayerController.Transform.position);

        // Set variables to started
        if (!PlayerInteractor.StartInteracting(this)) return;
        IsActive = true;
        PlayerController.MovementSlowdown = InteractSlowdown;
        //PlayerLegs.IsPointing = true; TODO
        //PlayerLegs.PointingLeg = rightPct > 0.0f ? 2 : 1; TODO
        TargetDirLH.SetActive(true);
        pluckTimer = 0.0f;
    }

    private void UpdatePluck()
    {
        if (IsActive)
        {
            // Decrease timer and update player
            pluckTimer = Mathf.Min(pluckTimer + Time.deltaTime, pluckTimerMax);
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

            // Pluck when done
            if (pluckTimer >= pluckTimerMax) CompletePluck();
        }
    }

    private void CancelPluck()
    {
        if (IsPlucked || !IsActive) return;
        if (!PlayerInteractor.StopInteracting(this)) return;

        // Reset variables to stopped
        IsActive = false;
        //PlayerLegs.IsPointing = false; TODO
        PlayerInteractor.InteractionEmphasis = 0.0f;
        PlayerController.MovementSlowdown = 0.0f;
        PlayerController.OverrideVerticalLean = 0.0f;
        TargetDirLH.SetActive(false);
        pluckTimer = 0.0f;
    }

    private void CompletePluck()
    {
        if (IsPlucked || !IsActive) return;
        if (!PlayerInteractor.StopInteracting(this)) return;

        // Reset variables to finished
        IsEnabled = false;
        IsActive = false;
        IsPlucked = true;
        //PlayerLegs.IsPointing = false; TODO
        PlayerInteractor.InteractionEmphasis = 0.0f;
        PlayerController.MovementSlowdown = 0.0f;
        PlayerController.OverrideVerticalLean = 0.0f;
        TargetDirLH.SetActive(false);
        pluckTimer = 0.0f;
        onPluck();
    }
}
