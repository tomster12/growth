using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceBiome", menuName = "Surface Biome")]
public class SurfaceBiome : Biome
{
    public WorldFeatureRule[] rules;

    public void OnValidate()
    {
        if (rules != null)
        {
            if (rules.Length > 1)
            {
                if (rules[rules.Length - 1].spawnConfig == rules[rules.Length - 2].spawnConfig)
                {
                    rules[rules.Length - 1].spawnConfig = null;
                }
            }

            foreach (WorldFeatureRule rule in rules) rule.OnValidate();
        }
    }
}
