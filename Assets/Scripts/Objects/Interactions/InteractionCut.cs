using System;
using UnityEngine;

[Serializable]
public class InteractionCut : Interaction
{
    public InteractionCut(Action onCut) : base("Cut", "cut", InteractionInput.Mouse(0), ToolType.Cutter)
    {
        this.onCut = onCut;
        IsEnabled = true;
    }

    public bool IsCut { get; private set; }

    public override bool CanInteract(IInteractor interactor) => base.CanInteract(interactor) && !IsCut;

    public override void StartInteracting(IInteractor interactor)
    {
        base.StartInteracting(interactor);
        base.StopInteracting();

        // Reset variables call callback
        IsEnabled = false;
        IsCut = true;
        onCut();
    }

    private Action onCut;
}
