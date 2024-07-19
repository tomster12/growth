using UnityEngine;

public class CraftingResultObject : CompositeObject
{
    protected override void Awake()
    {
        base.Awake();
        CL.isTrigger = true;
    }

    public void SetRecipe(CraftingRecipe recipe)
    {
        this.recipe = recipe;
    }

    private CraftingRecipe recipe;
}
