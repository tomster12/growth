using System;

[Serializable]
public class WorldBiomeInstance
{
    // End index is exclusive
    public int startIndex;
    public int endIndex;
    public SurfaceBiome biome;

    public WorldBiomeInstance(int startIndex, int endIndex, SurfaceBiome biome)
    {
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.biome = biome;
    }
}
