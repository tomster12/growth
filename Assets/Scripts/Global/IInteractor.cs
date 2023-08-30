
public interface IInteractor
{
    float SqueezeAmount { get; }

    void SetSqueezeAmount(float squeezeAmount);

    void SetInteracting(bool isInteracting);

    void SetControlled(bool toControl);
}
