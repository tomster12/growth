using UnityEngine;

[CreateAssetMenu(fileName = "CraftingRecipeList", menuName = "Crafting Recipe List")]
public class CraftingRecipeList : ScriptableObject
{
    public CraftingRecipe[] recipes;
}
