using System;
using UnityEngine;

public enum InputType
{ Key, Mouse }

public enum InputEvent
{ Inactive, Active }

[Serializable]
public class InteractionInput
{
    public InputType Type;
    public KeyCode Code;
    public int MouseButton;
    public string Name => Type == InputType.Key ? Code.ToString() : (MouseButton == 0 ? "lmb" : "rmb");

    public static InteractionInput Key(KeyCode code)
    {
        InteractionInput input = new InteractionInput();
        input.Type = InputType.Key;
        input.Code = code;
        return input;
    }

    public static InteractionInput Mouse(int button)
    {
        InteractionInput input = new InteractionInput();
        input.Type = InputType.Mouse;
        input.MouseButton = button;
        return input;
    }
}
