
using System.Collections.Generic;


public class PartInteractable : Part
{
    public List<PlayerInteractor.Interaction> Interactions { get; protected set; } = new List<PlayerInteractor.Interaction>();


    public override void InitPart(ComposableObject composable)
    {
        base.InitPart(composable);
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }

    public void UpdateInteracting()
    {
        foreach (PlayerInteractor.Interaction interaction in Interactions) interaction.UpdateInteracting();
    }
}
