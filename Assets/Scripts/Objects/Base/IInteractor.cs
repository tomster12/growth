using UnityEngine;

public interface IInteractor
{
    public void OnInteractionFinished();

    public void SetInteractionEmphasis(float amount);

    public void SetInteractionSlowdown(float amount);

    public void SetInteractionPulling(Vector2 target, float amount);

    public ToolType GetInteractorToolType();
}
