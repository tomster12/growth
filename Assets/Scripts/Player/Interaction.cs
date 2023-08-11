using System;
using UnityEngine;


[Serializable]
public abstract class InteractionInput
{
    public static InteractionInput LMB = new InteractionMouseInput(0);
    public static InteractionInput RMB = new InteractionMouseInput(1);


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
        name = code.ToString();
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
        name = button == 0 ? "lmb" : button == 1 ? "rmb" : button.ToString();
        this.button = button;
    }


    public override bool CheckInput() => Input.GetMouseButton(button);
    public override bool CheckInputDown() => Input.GetMouseButtonDown(button);
    public override bool CheckInputUp() => Input.GetMouseButtonUp(button);
}

[Serializable]
public class Interaction
{
    public enum Visibility { HIDDEN, INPUT, ICON, TEXT }

    public bool isEnabled { get; protected set; }
    public bool isActive { get; protected set; }
    public bool isBlocked { get; protected set; }

    public String name { get; private set; }
    public InteractionInput input { get; private set; }
    public Visibility visibility { get; protected set; }

    private Sprite blockedSprite;
    private Sprite spriteInputInactive;
    private Sprite spriteInputActive;
    private Sprite spriteIconInactive;
    private Sprite spriteIconActive;


    public Interaction(string name, InteractionInput input, Visibility visibility, String iconSpriteName)
    {
        isEnabled = true;
        isActive = false;
        isBlocked = false;

        this.name = name;
        this.input = input;
        this.visibility = visibility;

        blockedSprite = SpriteSet.instance.GetSprite("cross");
        spriteInputInactive = SpriteSet.instance.GetSprite(this.input.name + "_inactive");
        spriteInputActive  = SpriteSet.instance.GetSprite(this.input.name + "_active");
        if (iconSpriteName != null)
        {
            spriteIconInactive = SpriteSet.instance.GetSprite(iconSpriteName + "_inactive");
            spriteIconActive = SpriteSet.instance.GetSprite(iconSpriteName + "_active");
        }
    }


    public bool TryInteract(IInteractor IInteractor)
    {
        if (!isEnabled) return false;
        else if (input.CheckInputDown()) OnInputDown(IInteractor);
        else if (input.CheckInput()) OnHold(IInteractor);
        else if (input.CheckInputUp()) OnInputUp(IInteractor);
        else return false;
        return true;
    }

    public Sprite GetCurrentSpriteInput()
    {
        if (isBlocked) return blockedSprite;
        if (!isActive) return spriteInputInactive;
        return spriteInputActive;
    }

    public Sprite GetCurrentSpriteIcon()
    {
        if (isBlocked) return blockedSprite;
        if (!isActive) return spriteIconInactive;
        return spriteIconActive;
    }


    protected virtual void OnHold(IInteractor IInteractor) { }

    protected virtual void OnInputDown(IInteractor IInteractor) { }

    protected virtual void OnInputUp(IInteractor IInteractor) { }
}


public interface IInteractor
{
    public void SetSqueezeAmount(float squeezeAmount);

    public void SetInteracting(bool isInteracting);

    public void SetControlled(bool toControl);
}
