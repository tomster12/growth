using UnityEngine;
using static WorldGenerator;

public class FeatureGrass : MonoBehaviour, IWorldFeature
{
    public void Spawn(WorldSurfaceEdge edge, Vector3 a, Vector3 b, float edgePct)
    {
        // Choose to flip or not
        Vector3 dir = (b - a);
        bool toFlipX = UnityEngine.Random.value < 0.5f;

        // Generate foliage and set position
        Vector3 pos = a;
        if (toFlipX) pos += dir;
        transform.right = dir.normalized;
        transform.position = pos;

        // Grow sprite to correct size
        float width = dir.magnitude + 0.04f;
        float height = heightNoise.GetCyclicNoise(edgePct);
        spriteRenderer.size = new Vector3(width, spriteRenderer.size.y);
        spriteRenderer.flipX = toFlipX;
        spriteRenderer.transform.localScale = new Vector3(spriteRenderer.transform.localScale.x, height, spriteRenderer.transform.localScale.z);
    }

    public Vector3 GetPosition() => transform.position;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Config")]
    [SerializeField] private NoiseData heightNoise = new NoiseData(new float[] { 0.8f, 1.2f });
}
