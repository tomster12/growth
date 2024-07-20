using UnityEngine;

public class PartIngredient : Part
{
    public CraftingIngredient Ingredient => ingredient;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartHighlightable>();
        composable.RequirePart<PartControllable>();
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }

    [SerializeField] private CraftingIngredient ingredient;
}
