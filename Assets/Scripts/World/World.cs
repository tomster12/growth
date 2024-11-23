using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static List<World> Worlds { get; private set; } = new List<World>();

    [Header("Config")]
    [SerializeField] private bool showGizmos = true;

    public WorldGenerator WorldGenerator => worldGenerator;
    public BVH<BVHEdge> TerrainBVH { get; private set; }

    public static World GetClosestWorldByRB(Vector2 pos, out Vector2 groundPosition)
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

    public static World GetClosestWorldByCentre(Vector2 pos)
    {
        // Loop over and find the closest world
        World closestWorld = null;
        float closestDst = float.PositiveInfinity;
        foreach (World world in World.Worlds)
        {
            float dst = (world.GetCentre() - pos).magnitude;
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
        float closestDstSq = float.PositiveInfinity;
        foreach (WorldSurfaceEdge edge in worldGenerator.SurfaceEdges)
        {
            float dstSq = (edge.centre - pos).sqrMagnitude;
            if (dstSq < closestDstSq)
            {
                closestEdge = edge;
                closestDstSq = dstSq;
            }
        }
        return closestEdge;
    }

    [Header("References")]
    [SerializeField] private WorldGenerator worldGenerator;
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private Rigidbody2D rb;

    private List<Vector2> outsidePoints;

    private void Awake()
    {
        Worlds.Add(this);
        RecalculateOuterBoundary();
    }

    [ContextMenu("Recalculate Outer Boundary")]
    private void RecalculateOuterBoundary()
    {
        // Aggregate edges from all coliders on RB into a list of BVHEdges
        List<BVHEdge> edges = new List<BVHEdge>();
        PolygonCollider2D[] colliders = new PolygonCollider2D[rb.attachedColliderCount];
        rb.GetAttachedColliders(colliders);

        foreach (PolygonCollider2D collider in colliders)
        {
            for (int i = 0; i < collider.pathCount; i++)
            {
                Vector2[] path = collider.GetPath(i);
                for (int j = 0; j < path.Length; j++)
                {
                    Vector2 start = path[j];
                    Vector2 end = path[(j + 1) % path.Length];
                    start = collider.transform.TransformPoint(start);
                    end = collider.transform.TransformPoint(end);
                    edges.Add(new BVHEdge(start, end));
                }
            }
        }

        // Build BVH from all terrain edges
        TerrainBVH = new BVH<BVHEdge>(edges, 2);
    }

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            TerrainBVH?.DrawGizmos();
        }
    }
}
