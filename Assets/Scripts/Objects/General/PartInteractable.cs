using System.Collections.Generic;
using System.Linq;
using static PlayerInteractor;

public class PartInteractable : Part
{
    public List<PlayerInteraction> Interactions { get; protected set; } = new List<PlayerInteraction>();
    public bool CanInteract => Interactions.Where(i => i.IsEnabled && i.CanInteract).Count() > 0;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }

    public void UpdateInteracting()
    {
        foreach (PlayerInteraction interaction in Interactions) interaction.Update();
    }
}
