using UnityEngine;

[CreateAssetMenu(fileName = "CraftingIngredient", menuName = "Crafting Ingredient")]
public class CraftingIngredient : ScriptableObject
{
    public string ingredientName;
    public Sprite ingredientSprite;
    public GameObject ingredientPfb;
}
