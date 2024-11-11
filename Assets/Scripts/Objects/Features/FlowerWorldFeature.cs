using UnityEngine;
using System;
using UnityEngine.Events;

public class FlowerWorldFeature : MonoBehaviour, IWorldFeature
{
    public float BlockingRadius => blockingRadius;

    public void Spawn(WorldSurfaceEdge edge, float edgePct, WorldFeatureConfig config)
    {
        // Choose to flip or not
        bool toFlipX = UnityEngine.Random.value < 0.5f;

        // Place correctly
        float position = UnityEngine.Random.value;
        Vector3 dir = (edge.b - edge.a);
        transform.right = dir.normalized;
        transform.position = edge.a + dir * position;

        // Grow sprite to correct size
        float height = heightNoise.GetCyclicNoise(edgePct);
        float width = (spriteRenderer.transform.localScale.x / spriteRenderer.transform.localScale.y) * height;
        blockingRadius = width / 2;
        spriteRenderer.flipX = toFlipX;
        spriteRenderer.transform.localScale = new Vector3(spriteRenderer.transform.localScale.x, height, spriteRenderer.transform.localScale.z);

        // Invoke event
        OnSpawnEvent.Invoke();
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private UnityEvent OnSpawnEvent;

    [Header("Config")]
    [SerializeField] private NoiseData heightNoise = new NoiseData(new float[] { 0.8f, 1.2f });

    private float blockingRadius;
}