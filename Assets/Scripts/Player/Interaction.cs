using System;
using UnityEngine;


[Serializable]
public abstract class InteractionInput
{
    public static InteractionInput LMB = new InteractionMouseInput(0);
    public static InteractionInput RMB = new InteractionMouseInput(0);

    public String name;

    public abstract bool CheckInput();
    public abstract bool CheckInputDown();
    public abstract bool CheckInputUp();
}

[Serializable]
public class InteractionKeyInput : InteractionInput
{
    public KeyCode code;

    public InteractionKeyInput(KeyCode code)
    {
        this.name = code.ToString();
        this.code = code;
    }

    public override bool CheckInput() => Input.GetKey(code);
    public override bool CheckInputDown() => Input.GetKeyDown(code);
    public override bool CheckInputUp() => Input.GetKeyUp(code);
}

[Serializable]
public class InteractionMouseInput : InteractionInput
{
    public int button;

    public InteractionMouseInput(int button)
    {
        this.name = button == 0 ? "LMB" : button == 1 ? "RMB" : button.ToString();
        this.button = button;
    }

    public override bool CheckInput() => Input.GetMouseButton(button);
    public override bool CheckInputDown() => Input.GetMouseButtonDown(button);
    public override bool CheckInputUp() => Input.GetMouseButtonUp(button);
}

[Serializable]
public class Interaction
{
    public enum Visibility { HIDDEN, ICON, FULL }

    public bool isEnabled;
    public Visibility visibility;
    public String name;
    public InteractionInput input;
    public Action<IInteractor> holdCallback, downCallback, upCallback;

    public Interaction(bool isEnabled, Visibility visibility, string name, InteractionInput input, Action<IInteractor> holdCallback, Action<IInteractor> downCallback, Action<IInteractor> upCallback)
    {
        this.isEnabled = isEnabled;
        this.visibility = visibility;
        this.name = name;
        this.input = input;
        this.holdCallback = holdCallback;
        this.downCallback = downCallback;
        this.upCallback = upCallback;
    }

    public bool TryInteract(IInteractor interactorI)
    {
        if (!isEnabled) return false;
        else if (input.CheckInput() && holdCallback != null) holdCallback(interactorI);
        else if (input.CheckInputDown() && downCallback != null) downCallback(interactorI);
        else if (input.CheckInputUp() && upCallback != null) upCallback(interactorI);
        else return false;
        return true;
    }
}


public interface IInteractor
{
    public void Interaction_SetSqueezeAmount(float squeezeAmount);

    public void Interaction_SetInteracting(bool isInteracting);

    public void Interaction_SetControlled(bool toControl);
}
