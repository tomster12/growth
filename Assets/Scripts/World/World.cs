using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static List<World> Worlds { get; private set; } = new List<World>();

    public WorldGenerator WorldGenerator => worldGenerator;

    public static World GetClosestWorld(Vector2 pos) => GetClosestWorld(pos, out Vector2 _);

    public static World GetClosestWorld(Vector2 pos, out Vector2 groundPosition)
    {
        // Loop over and find the closest world
        World closestWorld = null;
        float closestDst = float.PositiveInfinity;
        groundPosition = pos;
        foreach (World world in World.Worlds)
        {
            Vector2 closestGroundPosition = world.GetClosestOverallPoint(pos);
            float dst = (closestGroundPosition - pos).magnitude;
            if (dst < closestDst)
            {
                closestWorld = world;
                closestDst = dst;
                groundPosition = closestGroundPosition;
            }
        }
        return closestWorld;
    }

    public static World GetClosestWorldCheap(Vector2 pos)
    {
        // Loop over and find the closest world
        World closestWorld = null;
        float closestDst = float.PositiveInfinity;
        foreach (World world in World.Worlds)
        {
            float dst = ((Vector2)world.GetCentre() - pos).magnitude;
            if (dst < closestDst)
            {
                closestWorld = world;
                closestDst = dst;
            }
        }
        return closestWorld;
    }

    public Vector2 GetCentre() => rb.transform.position;

    public Vector2 GetClosestOverallPoint(Vector2 pos) => rb.ClosestPoint(pos);

    public Vector2 GetClosestSurfacePoint(Vector2 pos) => outsidePolygon.ClosestPoint(pos);

    public WorldSurfaceEdge GetClosestEdge(Vector2 pos)
    {
        // Get closest edge using sq distance
        WorldSurfaceEdge closestEdge = null;
        float closestDst = float.PositiveInfinity;
        foreach (WorldSurfaceEdge edge in worldGenerator.SurfaceEdges)
        {
            float dstSq = (edge.centre - pos).sqrMagnitude;
            if (dstSq < closestDst)
            {
                closestEdge = edge;
                closestDst = dstSq;
            }
        }
        return closestEdge;
    }

    [Header("References")]
    [SerializeField] private WorldGenerator worldGenerator;
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private Rigidbody2D rb;

    private void Awake()
    {
        Worlds.Add(this);
    }
}
