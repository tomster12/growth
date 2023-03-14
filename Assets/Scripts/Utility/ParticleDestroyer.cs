
using UnityEngine;


public class ParticleDestroyer : MonoBehaviour
{

    [SerializeField] private ParticleSystem particles;

    void Update()
    {
        if (!particles.IsAlive()) DestroyImmediate(gameObject);
    }
}
