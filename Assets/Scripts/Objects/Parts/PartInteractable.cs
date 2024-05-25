using System.Collections.Generic;
using System.Linq;

public class PartInteractable : Part
{
    public List<Interaction> Interactions { get; private set; } = new List<Interaction>();

    public bool CanInteractAny(IInteractor interactor) => Interactions.Where(i => i.CanInteract(interactor)).Count() > 0;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }

    public void AddInteraction(Interaction interaction) => Interactions.Add(interaction);

    public void RemoveInteraction(Interaction interaction) => Interactions.Remove(interaction);

    private void Update()
    {
        // Update interactions, with a copy of the list to avoid concurrent modification
        foreach (var interaction in Interactions.ToList()) interaction.Update();
    }
}
