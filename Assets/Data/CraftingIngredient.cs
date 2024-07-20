using UnityEngine;

[CreateAssetMenu(fileName = "CraftingIngredient", menuName = "Crafting Ingredient")]
public class CraftingIngredient : ScriptableObject
{
    public new string name;
    public Sprite sprite;
    public GameObject pfb;
}
