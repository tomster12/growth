using UnityEngine;

public class TreeWorldFeature : MonoBehaviour, IWorldFeature
{
    public Transform Transform => transform;

    public void Place(WorldSurfaceEdge edge, float edgePct, WorldFeatureConfig config)
    {
        // Position and rotate
        float t = 0.25f + Random.value * 0.5f;
        transform.position = Utility.WithZ(Vector2.Lerp(edge.a, edge.b, t), transform.position.z);
        Vector2 edgeUp = Vector2.Perpendicular(edge.b - edge.a);
        Vector2 worldUp = edge.centre - edge.worldSite.world.GetCentre();
        transform.up = (edgeUp + worldUp) / 2.0f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, transform.eulerAngles.z - 6.0f + Random.value * 12.0f);
        transform.position += -transform.up * embedDistance;

        // Generate tree
        treeGenerator.Generate();
    }

    public bool Contains(Vector2 point)
    {
        return (point - (Vector2)transform.position).sqrMagnitude < (treeGenerator.BaseWidth * treeGenerator.BaseWidth);
    }

    [Header("References")]
    [SerializeField] private TreeGenerator treeGenerator;
    [SerializeField] private float embedDistance;
}
