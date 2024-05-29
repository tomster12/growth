using UnityEngine;

public class PluckableStoneFeature : MonoBehaviour, IWorldFeature
{
    public void Spawn(WorldSurfaceEdge edge, float edgePct)
    {
        // Setup pluck direction and generate
        composite.PluckDir = Vector2.Perpendicular(edge.b - edge.a);
        generator.Generate();

        // Position and rotate
        float t = 0.25f + UnityEngine.Random.value * 0.5f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, UnityEngine.Random.value * 360.0f);
        transform.position = Utility.WithZ(Vector2.Lerp(edge.a, edge.b, t), transform.position.z);

        // Set blocking radius
        blockingRadius = (edge.b - edge.a).magnitude * 0.5f;
    }

    public Transform Transform => transform;
    public Vector3 Position => transform.position;
    public float BlockingRadius => blockingRadius;

    [Header("References")]
    [SerializeField] private PluckableStoneObject composite;
    [SerializeField] private GeneratorController generator;

    private float blockingRadius;
}
