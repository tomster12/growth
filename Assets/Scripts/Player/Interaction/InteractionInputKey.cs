using System;
using UnityEngine;

[Serializable]
public class InteractionInputKey : InteractionInput
{
    public KeyCode code;

    public InteractionInputKey(KeyCode code)
    {
        name = code.ToString();
        this.code = code;
    }

    public override bool CheckInput() => Input.GetKey(code);

    public override bool CheckInputDown() => Input.GetKeyDown(code);

    public override bool CheckInputUp() => Input.GetKeyUp(code);
}
