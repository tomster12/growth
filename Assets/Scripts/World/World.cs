
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static List<World> Worlds { get; private set; } = new List<World>();

    [Header("References")]
    [SerializeField] private WorldGenerator worldGenerator;
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private Rigidbody2D rb;

    public WorldGenerator WorldGenerator => worldGenerator;

    private void Awake()
    {
        Worlds.Add(this);
    }

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

    public Vector3 GetCentre() => rb.transform.position;

    public Vector3 GetClosestOverallPoint(Vector2 pos) => rb.ClosestPoint(pos);

    public Vector3 GetClosestSurfacePoint(Vector2 pos) => outsidePolygon.ClosestPoint(pos);
}
