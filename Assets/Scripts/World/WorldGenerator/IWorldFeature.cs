using UnityEngine;

public interface IWorldFeature
{
    public void Place(WorldSurfaceEdge edge, float pct, WorldFeatureConfig config = null);

    public bool Contains(Vector2 point);

    public Transform Transform { get; }
};
