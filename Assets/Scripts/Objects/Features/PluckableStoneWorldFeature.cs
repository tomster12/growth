using UnityEngine;

public class PluckableStoneWorldFeature : MonoBehaviour, IWorldFeature
{
    public float BlockingRadius => blockingRadius;
    public Transform Transform => transform;

    public void Place(WorldSurfaceEdge edge, float edgePct, WorldFeatureConfig config)
    {
        // Setup pluck direction and generate
        composite.PluckDir = Vector2.Perpendicular(edge.b - edge.a);
        generator.Generate();

        // Position and rotate
        float t = 0.25f + Random.value * 0.5f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, Random.value * 360.0f);
        transform.position = Vector2.Lerp(edge.a, edge.b, t);

        // Set blocking radius
        blockingRadius = edge.length * 0.5f;
    }

    public bool Contains(Vector2 point)
    {
        return Vector2.Distance(point, transform.position) < blockingRadius;
    }

    [Header("References")]
    [SerializeField] private PluckableStoneObject composite;
    [SerializeField] private GeneratorController generator;

    private float blockingRadius;
}
