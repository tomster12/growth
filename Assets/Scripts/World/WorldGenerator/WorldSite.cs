using System;

[Serializable]
public class WorldSite
{
    public World world;
    public MeshSite meshSite;
    public int outsideDistance = -1;
    public float maxEnergy = 0, energy = 0;
    public Biome biome;

    public WorldSite(World world, MeshSite meshSite)
    {
        this.world = world;
        this.meshSite = meshSite;
    }
}
