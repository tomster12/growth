
using System.Collections.Generic;


public class PartInteractable : Part
{
    public List<Interaction> Interactions { get; protected set; } = new List<Interaction>();

    private IInteractor viewingIInteractor;


    public override void InitPart(ComposableObject composable)
    {
        base.InitPart(composable);
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }

    public List<Interaction> StartViewingInteractions(IInteractor IInteractor)
    {
        viewingIInteractor = IInteractor;
        return Interactions;
    }

    public void StopViewingInteractions()
    {
        viewingIInteractor = null;
    }


    private void Update()
    {
        if (viewingIInteractor != null)
        {
            foreach (Interaction interaction in Interactions) interaction.PollInput(viewingIInteractor);
        }
    }
}
