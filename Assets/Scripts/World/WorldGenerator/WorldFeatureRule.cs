using System;
using System.Collections.Generic;
using UnityEngine;
using static WorldBiomeGenerator;

public enum WorldFeatureSpawnType
{ CLUSTER, CHANCE, EVERY, NEAR }

public abstract class WorldFeatureSpawnConfig
{
};

public class WorldFeatureSpawnConfigCluster : WorldFeatureSpawnConfig
{
    public int clusterSize;
    public int clusterRadius;
    public float clusterPer100;
}

public class WorldFeatureSpawnConfigChance : WorldFeatureSpawnConfig
{
    public float chancePer100;
}

public class WorldFeatureSpawnConfigEvery : WorldFeatureSpawnConfig
{ }

public class WorldFeatureSpawnConfigNear : WorldFeatureSpawnConfig
{
    public List<(IWorldFeature feature, float distance)> preferences;
    public float chanceAtNearby;
}

[Serializable]
public class WorldFeatureRule
{
    [SerializeReference] public WorldFeatureConfig config;
    [SerializeField] public WorldFeatureSpawnType spawnType;
    [SerializeReference] public WorldFeatureSpawnConfig spawnConfig;

    public void SpawnFeature(WorldSurfaceEdge edge, float pct, WorldFeatureConfig config = null)
    {
    }

    public void SpawnOnEdges(List<WorldSurfaceEdge> edges, int startIndex, int endIndex, List<WorldFeatureInstance> instances)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            Vector2 centre = (edges[i].a + edges[i].b) / 2.0f;

            switch (spawnType)
            {
                case WorldFeatureSpawnType.CLUSTER:
                    WorldFeatureSpawnConfigCluster clusterConfig = (WorldFeatureSpawnConfigCluster)spawnConfig;
                    if (UnityEngine.Random.Range(0, 100) < clusterConfig.clusterPer100)
                    {
                        // TODO: Spawn clusters instead of single
                        SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f), config);
                    }
                    break;

                case WorldFeatureSpawnType.CHANCE:
                    WorldFeatureSpawnConfigChance chanceConfig = (WorldFeatureSpawnConfigChance)spawnConfig;
                    if (UnityEngine.Random.Range(0, 100) < chanceConfig.chancePer100)
                    {
                        SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f), config);
                    }
                    break;

                case WorldFeatureSpawnType.EVERY:
                    SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f), config);
                    break;

                case WorldFeatureSpawnType.NEAR:
                    WorldFeatureSpawnConfigNear nearConfig = (WorldFeatureSpawnConfigNear)spawnConfig;
                    float strongestPreference = 0.0f;
                    foreach ((IWorldFeature feature, float distance) in nearConfig.preferences)
                    {
                        float closestDist = float.MaxValue;
                        for (int j = 0; j < instances.Count; j++)
                        {
                            if (instances[j].Feature == feature)
                            {
                                float dist = Mathf.Min(Vector3.Distance(instances[j].Feature.Transform.position, centre), closestDist);
                                closestDist = Mathf.Min(dist, closestDist);
                                break;
                            }
                        }

                        float preference = 1.0f - Mathf.Clamp(closestDist / distance, 0.0f, 1.0f);
                        strongestPreference = Mathf.Max(preference, strongestPreference);
                    }
                    if (UnityEngine.Random.Range(0, 100) < strongestPreference * nearConfig.chanceAtNearby)
                    {
                        SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f), config);
                    }
                    break;
            }
        }
    }

    public void OnValidate()
    {
        switch (spawnType)
        {
            case WorldFeatureSpawnType.CLUSTER:
                if (spawnConfig == null || spawnConfig.GetType() != typeof(WorldFeatureSpawnConfigCluster))
                {
                    spawnConfig = new WorldFeatureSpawnConfigCluster();
                }
                break;

            case WorldFeatureSpawnType.CHANCE:
                if (spawnConfig == null || spawnConfig.GetType() != typeof(WorldFeatureSpawnConfigChance))
                {
                    spawnConfig = new WorldFeatureSpawnConfigChance();
                }
                break;

            case WorldFeatureSpawnType.EVERY:
                if (spawnConfig == null || spawnConfig.GetType() != typeof(WorldFeatureSpawnConfigEvery))
                {
                    spawnConfig = new WorldFeatureSpawnConfigEvery();
                }
                break;

            case WorldFeatureSpawnType.NEAR:
                if (spawnConfig == null || spawnConfig.GetType() != typeof(WorldFeatureSpawnConfigNear))
                {
                    spawnConfig = new WorldFeatureSpawnConfigNear();
                }
                break;
        }
    }
}
