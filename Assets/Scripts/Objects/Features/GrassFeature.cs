using UnityEngine;
using System;
using UnityEngine.Events;

public class GrassFeature : MonoBehaviour, IWorldFeature
{
    public Transform Transform => transform;
    public Vector3 Position => transform.position;
    public float BlockingRadius => blockingRadius;

    public void Spawn(WorldSurfaceEdge edge, float edgePct)
    {
        // Choose to flip or not
        bool toFlipX = UnityEngine.Random.value < 0.5f;

        // Place correctly
        Vector3 dir = (edge.b - edge.a);
        transform.right = dir.normalized;
        transform.position = edge.a + dir / 2.0f;

        // Grow sprite to correct size
        float width = dir.magnitude + 0.04f;
        blockingRadius = width / 2.0f;
        float height = heightNoise.GetCyclicNoise(edgePct);
        spriteRenderer.size = new Vector3(width, spriteRenderer.size.y);
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
