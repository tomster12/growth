using System;
using UnityEngine;

[Serializable]
public class InteractionToolClick : Interaction
{
    public InteractionToolClick(string name, string sprite, ToolType tool, Action onClick) : base(name, sprite, InteractionInput.Mouse(0), tool)
    {
        this.onClick = onClick;
        IsEnabled = true;
    }

    public bool IsClicked { get; private set; }

    public override bool CanInteract(IInteractor interactor) => base.CanInteract(interactor) && !IsClicked;

    public override void StartInteracting(IInteractor interactor)
    {
        base.StartInteracting(interactor);
        base.StopInteracting();
        IsEnabled = false;
        IsClicked = true;
        onClick();
    }

    private Action onClick;
}
