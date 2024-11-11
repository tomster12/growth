using UnityEngine;

public interface IWorldFeature
{
    public void Place(WorldSurfaceEdge edge, float pct, WorldFeatureConfig config = null);

    public float BlockingRadius { get; }
    public Transform Transform { get; }
};
