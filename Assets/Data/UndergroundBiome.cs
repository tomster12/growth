using UnityEngine;

[CreateAssetMenu(fileName = "UndergroundBiome", menuName = "Underground Biome")]
public class UndergroundBiome : Biome
{
    public int depth = 3;
    public int gradientOffset = 1;
    public float gradientPct = 0.8f;
}
