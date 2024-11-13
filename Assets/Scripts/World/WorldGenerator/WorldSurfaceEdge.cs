using System;
using UnityEngine;
using UnityEngine.Assertions;

// Counter-clockwise edge
[Serializable]
public class WorldSurfaceEdge
{
    public SurfaceBiome biome => (SurfaceBiome)worldSite.biome;
    public WorldSite worldSite;
    public MeshSiteEdge meshSiteEdge;
    public Vector2 a, b, centre;
    public float length;

    public WorldSurfaceEdge(WorldSite worldSite, MeshSiteEdge meshSiteEdge)
    {
        Assert.AreEqual(worldSite.meshSite.siteIndex, meshSiteEdge.siteIndex);

        this.worldSite = worldSite;
        this.meshSiteEdge = meshSiteEdge;
    }

    public void InitPositions(Transform transform, Mesh mesh)
    {
        a = transform.TransformPoint(mesh.vertices[worldSite.meshSite.verticesMeshIndices[meshSiteEdge.fromVertexIndex]]);
        b = transform.TransformPoint(mesh.vertices[worldSite.meshSite.verticesMeshIndices[meshSiteEdge.toVertexIndex]]);
        centre = (a + b) / 2;
        length = Vector2.Distance(a, b);
    }
}
