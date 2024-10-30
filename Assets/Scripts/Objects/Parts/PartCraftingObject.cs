using UnityEngine;

public class PartCraftingObject : Part
{
    public CraftingObject CraftingObject => craftingObject;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartHighlightable>();
    }

    [SerializeField] private CraftingObject craftingObject;
}
