using System;
using UnityEngine;

[Serializable]
public class InteractionClick : Interaction
{
    public InteractionClick(string name, string sprite, Action onClick, ToolType tool = ToolType.Any) : base(name, sprite, InteractionInput.Mouse(0), tool)
    {
        this.onClick = onClick;
        IsEnabled = true;
    }

    public override void StartInteracting(IInteractor interactor)
    {
        base.StartInteracting(interactor);
        base.StopInteracting();
        onClick();
    }

    private Action onClick;
}
