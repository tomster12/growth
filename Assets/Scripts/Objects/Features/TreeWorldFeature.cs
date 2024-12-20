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

        // Scale Randomly
        float width = Mathf.Lerp(sizeMin.x, sizeMax.x, Random.value);
        float height = Mathf.Lerp(sizeMin.y, sizeMax.y, Random.value);
        if (Random.value < tallChance) height = sizeTall;
        mesh.localScale = new Vector3(width, height, 1.0f);
        mesh.localPosition = new Vector3(-mesh.localScale.x * 0.5f, mesh.localScale.y * 0.5f - embedDistance, mesh.transform.position.z);
    }

    public bool Contains(Vector2 point)
    {
        return Vector2.Distance(transform.position, point) < mesh.localScale.x * 0.5f;
    }

    [Header("References")]
    [SerializeField] private Transform mesh;
    [SerializeField] private Vector2 sizeMin;
    [SerializeField] private Vector2 sizeMax;
    [SerializeField] private float sizeTall;
    [SerializeField] private float tallChance;
    [SerializeField] private float embedDistance;
}
