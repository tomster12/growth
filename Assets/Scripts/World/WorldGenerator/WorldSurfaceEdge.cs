using UnityEngine;
using UnityEngine.Assertions;

// Counter-clockwise edge
public class WorldSurfaceEdge
{
    public SurfaceBiome biome => (SurfaceBiome)worldSite.biome;
    public WorldSite worldSite;
    public MeshSiteEdge meshSiteEdge;
    public Vector3 a, b;
    public float length;

    public WorldSurfaceEdge(WorldSite worldSite, MeshSiteEdge meshSiteEdge)
    {
        Assert.AreEqual(worldSite.meshSite.siteIdx, meshSiteEdge.siteIndex);
        this.worldSite = worldSite;
        this.meshSiteEdge = meshSiteEdge;
    }
}
