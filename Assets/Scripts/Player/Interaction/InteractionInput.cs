using System;
using UnityEngine;


[Serializable]
public abstract class InteractionInput
{
    public readonly static InteractionInput LMB = new InteractionInputMouse(0);
    public readonly static InteractionInput RMB = new InteractionInputMouse(1);
    public String name;


    public abstract bool CheckInput();
    public abstract bool CheckInputDown();
    public abstract bool CheckInputUp();
}
