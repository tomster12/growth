using System.Collections.Generic;
using UnityEngine;

public enum WorldFeatureSpawnType
{ CLUSTER, CHANCE, EVERY, NEAR }

public abstract class WorldFeatureSpawnConfig
{
};

public class WorldFeatureSpawnConfigCluster : WorldFeatureSpawnConfig
{
    public int clusterSize;
    public int clusterRadius;
}

public class WorldFeatureSpawnConfigChance : WorldFeatureSpawnConfig
{
    public float chance;
}

public class WorldFeatureSpawnConfigEvery : WorldFeatureSpawnConfig
{ }

public class WorldFeatureSpawnConfigNear : WorldFeatureSpawnConfig
{
    public int near;
}

public class WorldFeatureRule
{
    [SerializeField] public WorldFeature feature;
    [SerializeReference] public WorldFeatureConfig config;
    [SerializeReference] public WorldFeatureSpawnConfig spawnConfig;

    public void SpawnOnEdges(List<WorldSurfaceEdge> edges, int startIndex, int endIndex, List<WorldFeatureInstance> instances)
    {
    }
}
