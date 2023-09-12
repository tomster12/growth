
using System;
using UnityEngine;


[Serializable]
class InteractionPluck : PlayerInteractor.Interaction
{
    public bool IsPlucked { get; private set; }

    private Action onPluck;
    private float pluckTimerMax;
    private float pluckTimer;


    public InteractionPluck(Action onPluck, float pluckTimerMax) : base("Pluck", InteractionInput.LMB, Visibility.ICON, "up")
    {
        this.onPluck = onPluck;
        this.pluckTimerMax = pluckTimerMax;
        IsEnabled = true;
    }


    protected override void OnInputDown() => StartPluck();

    protected override void OnInputUp() => CancelPluck();

    protected override void UpdateAction() => UpdatePluck();

    private void StartPluck()
    {
        if (IsPlucked || IsActive) return;

        // Set variables to started
        if (!PlayerInteractor.StartInteracting(this)) return;
        IsActive = true;
        PlayerController.MovementSlowdown = InteractSlowdown;
        PlayerLegs.IsPointing = true;
        PlayerLegs.PointingLeg = 2;
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
            PlayerController.OverrideLean = pluckTimer / pluckTimerMax;

            // Point and draw line
            float upAmount = 0.2f + 0.2f * Utility.Easing.EaseInExpo(pluckTimer / pluckTimerMax);
            Vector2 pathStartRaw = PlayerLegs.GetLegEnd(PlayerLegs.PointingLeg);
            Vector2 pathEndRaw = TargetComposable.Position;
            Vector3 pathStart = new Vector3(pathStartRaw.x, pathStartRaw.y, PlayerController.Transform.position.z + 0.1f);
            Vector3 pathEnd = new Vector3(pathEndRaw.x, pathEndRaw.y, PlayerController.Transform.position.z + 0.1f);
            Vector3 controlStart = PlayerController.Transform.position;
            Vector3 controlDir = pathEnd - controlStart;
            Vector3 controlUp = PlayerController.UpDir.normalized * (pathEnd - pathStart).magnitude * upAmount;
            Vector3 controlPoint = controlStart + controlDir * 0.75f + controlUp;
            TargetDirLH.DrawCurve(pathStart, pathEnd, controlPoint, LegDirInteractColor);
            PlayerLegs.PointingPos = controlPoint;

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
        PlayerLegs.IsPointing = false;
        PlayerInteractor.InteractionEmphasis = 0.0f;
        PlayerController.MovementSlowdown = 0.0f;
        PlayerController.OverrideLean = 0.0f;
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
        PlayerLegs.IsPointing = false;
        PlayerInteractor.InteractionEmphasis = 0.0f;
        PlayerController.MovementSlowdown = 0.0f;
        PlayerController.OverrideLean = 0.0f;
        TargetDirLH.SetActive(false);
        pluckTimer = 0.0f;
        onPluck();
    }
}
