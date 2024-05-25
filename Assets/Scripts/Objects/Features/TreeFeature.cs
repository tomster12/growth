using UnityEngine;

public class TreeFeature : MonoBehaviour, IWorldFeature
{
    public void Spawn(WorldSurfaceEdge edge, float edgePct)
    {
        // Position and rotate
        float t = 0.25f + UnityEngine.Random.value * 0.5f;
        transform.position = Utility.WithZ(Vector2.Lerp(edge.a, edge.b, t), transform.position.z);
        Vector2 edgeUp = Vector2.Perpendicular(edge.b - edge.a);
        Vector2 worldUp = (edge.a + edge.b) / 2.0f - edge.worldSite.world.GetCentre();
        transform.up = (edgeUp + worldUp) / 2.0f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, transform.eulerAngles.z - 6.0f + UnityEngine.Random.value * 12.0f);

        // Scale Randomly
        float width = Mathf.Lerp(sizeMin.x, sizeMax.x, UnityEngine.Random.value);
        float height = Mathf.Lerp(sizeMin.y, sizeMax.y, UnityEngine.Random.value);
        if (UnityEngine.Random.value < tallChance) height = sizeTall;
        mesh.localScale = new Vector3(width, height, 1.0f);
        mesh.localPosition = new Vector3(-mesh.localScale.x * 0.5f, mesh.localScale.y * 0.5f - embedDistance, mesh.transform.position.z);
    }

    public Transform Transform => transform;
    public Vector3 Position => transform.position;
    public float BlockingRadius => mesh.localScale.x * 0.5f;

    [Header("References")]
    [SerializeField] private Transform mesh;
    [SerializeField] private Vector2 sizeMin;
    [SerializeField] private Vector2 sizeMax;
    [SerializeField] private float sizeTall;
    [SerializeField] private float tallChance;
    [SerializeField] private float embedDistance;
}
