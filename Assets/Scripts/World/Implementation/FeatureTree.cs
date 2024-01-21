
using UnityEngine;
using static WorldGenerator;


public class FeatureTree : MonoBehaviour, IWorldFeature
{
    [Header("References")]
    [SerializeField] private Transform mesh;
    [SerializeField] private Vector2 sizeMin;
    [SerializeField] private Vector2 sizeMax;
    [SerializeField] private float sizeTall;
    [SerializeField] private float tallChance;
    [SerializeField] private float embedDistance;


    public void Spawn(WorldSurfaceEdge edge, Vector3 a, Vector3 b, float edgePct)
    {
        // Position and rotate
        float t = 0.25f + UnityEngine.Random.value * 0.5f;
        transform.position = Vector2.Lerp(a, b, t);
        Vector2 edgeUp = Vector2.Perpendicular(b - a);
        Vector2 worldUp = (a + b) / 2.0f - edge.worldSite.world.GetCentre();
        transform.up = (edgeUp + worldUp) / 2.0f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, transform.eulerAngles.z - 6.0f + UnityEngine.Random.value * 12.0f);

        // Scale Randomly
        float width = Mathf.Lerp(sizeMin.x, sizeMax.x, UnityEngine.Random.value);
        float height = Mathf.Lerp(sizeMin.y, sizeMax.y, UnityEngine.Random.value);
        if (UnityEngine.Random.value < tallChance) height = sizeTall;
        mesh.localScale = new Vector3(width, height, 1.0f);
        mesh.localPosition = new Vector3(-mesh.localScale.x * 0.5f, mesh.localScale.y * 0.5f - embedDistance, mesh.transform.position.z);
    }

    public Vector3 GetPosition() => transform.position;
}
