using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftingRecipe
{
    [Serializable]
    public class Amount
    {
        public CraftingIngredient ingredient;
        public int amount;
    }

    [Serializable]
    public class Usage
    {
        public bool canCraft = false;
        public List<CompositeObject> usedIngredients = new List<CompositeObject>();
        public List<CompositeObject> usedObjects = new List<CompositeObject>();
        public bool isToolUsed = false;
    }

    public ToolType requiredTool;
    public CraftingObject requiredObject;
    public Amount[] ingredients;
    public Amount result;

    public Usage CanCraft(
        Dictionary<CraftingIngredient,
        List<CompositeObject>> availableIngredients,
        List<(CraftingObject, CompositeObject)> availableObjects, ToolType tool)
    {
        Usage usage = new();

        // Check required tool is met
        if (requiredTool != ToolType.Any && requiredTool != ToolType.None)
        {
            if (requiredTool != tool) return usage;

            usage.isToolUsed = true;
        }

        // Check required object is met
        if (requiredObject != null)
        {
            bool found = false;
            foreach ((CraftingObject, CompositeObject) obj in availableObjects)
            {
                if (obj.Item1 == requiredObject)
                {
                    found = true;
                    usage.usedObjects.Add(obj.Item2);
                    break;
                }
            }
            if (!found) return usage;
        }

        foreach (Amount ingredient in ingredients)
        {
            if (!availableIngredients.ContainsKey(ingredient.ingredient)) return usage;
            if (availableIngredients[ingredient.ingredient].Count < ingredient.amount) return usage;

            for (int i = 0; i < ingredient.amount; i++)
            {
                usage.usedIngredients.Add(availableIngredients[ingredient.ingredient][i]);
            }
        }

        usage.canCraft = true;
        return usage;
    }
}
