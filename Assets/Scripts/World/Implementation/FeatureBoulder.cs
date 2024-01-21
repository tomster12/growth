
using UnityEngine;
using static WorldGenerator;


public class FeatureBoulder : MonoBehaviour, IWorldFeature
{
    [Header("References")]
    [SerializeField] private GeneratorController generator;


    public void Spawn(WorldSurfaceEdge edge, Vector3 a, Vector3 b, float edgePct)
    {
        // Generate
        generator.Generate();

        // Position and rotate
        float t = 0.25f + UnityEngine.Random.value * 0.5f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, UnityEngine.Random.value * 360.0f);
        transform.position = Vector2.Lerp(a, b, t);
    }

    public Vector3 GetPosition() => transform.position;
}
