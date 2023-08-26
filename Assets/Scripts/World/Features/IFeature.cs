
using UnityEngine;
using static WorldGenerator;


public interface IFeature
{
    public void Spawn(WorldSurfaceEdge edge, Vector3 a, Vector3 b, float pct);
    public Vector3 GetPosition();
};
