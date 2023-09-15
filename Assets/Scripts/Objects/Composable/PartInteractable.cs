
using System.Collections.Generic;
using System.Linq;


public class PartInteractable : Part
{
    public List<PlayerInteractor.Interaction> Interactions { get; protected set; } = new List<PlayerInteractor.Interaction>();
    public bool CanInteract => Interactions.Where(i => i.IsEnabled && i.CanInteract).Count() > 0;


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
