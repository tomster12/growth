using UnityEngine;
using static WorldGenerator;

public interface IWorldFeature
{
    void Spawn(WorldSurfaceEdge edge, Vector3 a, Vector3 b, float pct);

    Vector3 GetPosition();
};
