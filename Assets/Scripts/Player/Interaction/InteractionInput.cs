using System;

[Serializable]
public abstract class InteractionInput
{
    public static readonly InteractionInput LMB = new InteractionInputMouse(0);
    public static readonly InteractionInput RMB = new InteractionInputMouse(1);
    public String name;

    public abstract bool CheckInput();

    public abstract bool CheckInputDown();

    public abstract bool CheckInputUp();
}
