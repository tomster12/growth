
using UnityEngine;


public class ParticleDestroyer : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;

    private void Update()
    {
        if (!particles.IsAlive()) DestroyImmediate(gameObject);
    }
}
