
public interface IInteractor
{
    float CursorSpacing { get; }

    void SetInteractionEmphasis(float squeezeAmount);
    void SetInteraction(Interaction interaction);
    void SetControlled(bool toControl);
}
