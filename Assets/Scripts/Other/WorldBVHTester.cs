using UnityEngine;

public class WorldBVHTester : MonoBehaviour
{
    [SerializeField] private World world;

    private void OnDrawGizmos()
    {
        if (world == null) return;
        if (world.TerrainBVH == null) return;

        BVHEdge edge = world.TerrainBVH.FindClosestElement(transform.position);

        if (edge == null) return;

        Vector2 closestPoint = edge.ClosestPoint(transform.position);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(closestPoint, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, closestPoint);
    }
}
