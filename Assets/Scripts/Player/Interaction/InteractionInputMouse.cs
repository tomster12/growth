
using System;
using UnityEngine;


[Serializable]
public class InteractionInputMouse : InteractionInput
{
    public int button;


    public InteractionInputMouse(int button)
    {
        name = button == 0 ? "lmb" : button == 1 ? "rmb" : button.ToString();
        this.button = button;
    }


    public override bool CheckInput() => Input.GetMouseButton(button);
    public override bool CheckInputDown() => Input.GetMouseButtonDown(button);
    public override bool CheckInputUp() => Input.GetMouseButtonUp(button);
}
