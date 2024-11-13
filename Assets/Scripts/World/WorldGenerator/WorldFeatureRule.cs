using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
    public float countPer100;
}

public class WorldFeatureSpawnConfigChance : WorldFeatureSpawnConfig
{
    public float countPer100;
}

public class WorldFeatureSpawnConfigEvery : WorldFeatureSpawnConfig
{ }

public class WorldFeatureSpawnConfigNear : WorldFeatureSpawnConfig
{
    public List<(WorldFeatureType feature, float distance)> preferences;
    public float chanceAtNearby;
}

[Serializable]
public class WorldFeatureRule
{
    [SerializeReference] public WorldFeatureType type;
    [SerializeReference] public WorldFeatureConfig config;
    [SerializeField] public WorldFeatureSpawnType spawnType;
    [SerializeReference] public WorldFeatureSpawnConfig spawnConfig;

    public IWorldFeature SpawnFeature(WorldSurfaceEdge edge, float pct)
    {
        GameObject featureGO = GameObject.Instantiate(type.prefab);
        IWorldFeature feature = featureGO.GetComponent<IWorldFeature>();
        Assert.IsNotNull(feature, "Feature prefab must have a component that implements IWorldFeature");
        feature.Place(edge, pct, config);
        GameLayers.SetLayer(featureGO.transform, type.gameLayer);
        return feature;
    }

    public List<WorldFeatureInstance> PopulateEdges(List<WorldSurfaceEdge> edges, WorldBiomeInstance biomeInstance, List<WorldFeatureInstance> instances)
    {
        List<WorldFeatureInstance> newInstances = new List<WorldFeatureInstance>();

        for (int i = biomeInstance.startIndex; i != biomeInstance.endIndex; i = (i + 1) % edges.Count)
        {
            switch (spawnType)
            {
                case WorldFeatureSpawnType.CLUSTER:
                    WorldFeatureSpawnConfigCluster clusterConfig = (WorldFeatureSpawnConfigCluster)spawnConfig;
                    if (UnityEngine.Random.value * 100.0f < (clusterConfig.countPer100 * edges[i].length / 100))
                    {
                        // TODO: Spawn clusters instead of single
                        IWorldFeature clusterFeature = SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f));
                        WorldFeatureInstance clusterFeatureInstance = new WorldFeatureInstance(type, clusterFeature);
                        newInstances.Add(clusterFeatureInstance);
                    }
                    break;

                case WorldFeatureSpawnType.CHANCE:
                    WorldFeatureSpawnConfigChance chanceConfig = (WorldFeatureSpawnConfigChance)spawnConfig;

                    if (UnityEngine.Random.value < (edges[i].length * chanceConfig.countPer100 / 100))
                    {
                        IWorldFeature chanceFeature = SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f));
                        WorldFeatureInstance chanceFeatureInstance = new WorldFeatureInstance(type, chanceFeature);
                        newInstances.Add(chanceFeatureInstance);
                    }
                    break;

                case WorldFeatureSpawnType.EVERY:
                    IWorldFeature everyFeature = SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f));
                    WorldFeatureInstance everyFeatureInstance = new WorldFeatureInstance(type, everyFeature);
                    newInstances.Add(everyFeatureInstance);
                    break;

                case WorldFeatureSpawnType.NEAR:
                    WorldFeatureSpawnConfigNear nearConfig = (WorldFeatureSpawnConfigNear)spawnConfig;
                    float strongestPreference = 0.0f;
                    foreach ((WorldFeatureType feature, float distance) in nearConfig.preferences)
                    {
                        float closestDist = float.MaxValue;
                        for (int j = 0; j < instances.Count; j++)
                        {
                            if (feature == instances[j].Feature)
                            {
                                float dist = Mathf.Min(Vector3.Distance(instances[j].Feature.Transform.position, edges[i].centre), closestDist);
                                closestDist = Mathf.Min(dist, closestDist);
                                break;
                            }
                        }

                        float preference = 1.0f - Mathf.Clamp(closestDist / distance, 0.0f, 1.0f);
                        strongestPreference = Mathf.Max(preference, strongestPreference);
                    }
                    if (UnityEngine.Random.Range(0, 100) < strongestPreference * nearConfig.chanceAtNearby)
                    {
                        IWorldFeature nearFeature = SpawnFeature(edges[i], UnityEngine.Random.Range(0.0f, 1.0f));
                        WorldFeatureInstance nearFeatureInstance = new WorldFeatureInstance(type, nearFeature);
                        newInstances.Add(nearFeatureInstance);
                    }
                    break;
            }
        }

        return newInstances;
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
