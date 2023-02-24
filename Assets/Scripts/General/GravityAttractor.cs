
using GK;
using System.Collections.Generic;
using UnityEngine;


public class GravityAttractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rb;
    public Rigidbody2D rb => _rb;

    [Header("Config")]
    [SerializeField] public float gravityForce; // Standard Range: 800-1200
    [SerializeField] public float gravityRadius;
    public Vector2 centre => rb.transform.position;


    private void FixedUpdate()
    {
        // Attract all objects
        foreach (GravityObject obj in GravityObject.gravityObjects)
        {
            if (!obj.isEnabled) continue;
            Vector2 centreDir = centre - obj.centre;
            if (centreDir.magnitude < gravityRadius)
            {
                Vector2 surface = rb.ClosestPoint(obj.centre);
                Vector2 surfaceDir = surface - obj.centre;
                if (surfaceDir.magnitude == 0) continue;
                float force = gravityForce * (rb.mass * obj.rb.mass) / surfaceDir.magnitude;
                obj.rb.AddForce(surfaceDir.normalized * force);
            }
        }
    }
}
