using UnityEngine;

public class BoulderFeature : MonoBehaviour, IWorldFeature
{
    public void Spawn(WorldSurfaceEdge edge, float edgePct)
    {
        generator.Generate();

        // Position and rotate
        float t = 0.25f + Random.value * 0.5f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, UnityEngine.Random.value * 360.0f);
        transform.position = Vector2.Lerp(edge.a, edge.b, t);
    }

    public Transform Transform => transform;
    public Vector3 Position => transform.position;
    public float BlockingRadius => Mathf.Max(polyCollider.bounds.extents.x, polyCollider.bounds.extents.y, polyCollider.bounds.extents.z);

    [Header("References")]
    [SerializeField] private GeneratorController generator;
    [SerializeField] private PolygonCollider2D polyCollider;
}
