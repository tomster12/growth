using UnityEngine;

public interface IWorldFeature
{
    void Spawn(WorldSurfaceEdge edge, float pct);

    Transform Transform { get; }
    Vector3 Position { get; }
    float BlockingRadius { get; }
};
