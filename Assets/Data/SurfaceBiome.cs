using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceBiome", menuName = "Surface Biome")]
public class SurfaceBiome : Biome
{
    public WorldFeatureRule[] rules;

    public void OnValidate()
    {
        if (rules != null)
        {
            foreach (WorldFeatureRule rule in rules) rule.OnValidate();
        }
    }
}
