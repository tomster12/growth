using UnityEngine;

public class PartCraftingIngredient : Part
{
    public CraftingIngredient Ingredient => ingredient;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartHighlightable>();
        composable.RequirePart<PartControllable>();
    }

    public void UseIngredient()
    {
        DestroyImmediate(gameObject);
    }

    [SerializeField] private CraftingIngredient ingredient;
}
