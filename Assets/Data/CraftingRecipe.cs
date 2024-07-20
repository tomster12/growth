using System;
using UnityEngine;

[Serializable]
public class CraftingRecipe
{
    [Serializable]
    public class RecipeIngredient
    {
        public CraftingIngredient ingredient;
        public int amount;
    }

    public RecipeIngredient[] ingredients;
    public RecipeIngredient result;
}
