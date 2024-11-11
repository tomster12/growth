using UnityEngine;

public class WorldFeature : ScriptableObject
{
    [SerializeReference] public GameObject prefab;

    public void Spawn(WorldSurfaceEdge edge, float pct, WorldFeatureConfig config = null)
    { }
};

public class WorldFeatureInstance
{
    private GameObject Instance { get; }
    private float BlockingRadius { get; }

    public WorldFeatureInstance(GameObject instance, float blockingRadius)
    {
        Instance = instance;
        BlockingRadius = blockingRadius;
    }
}

public abstract class WorldFeatureConfig : ScriptableObject
{
};
