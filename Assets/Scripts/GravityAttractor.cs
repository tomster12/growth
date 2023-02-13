
using System.Collections.Generic;
using UnityEngine;


public class GravityAttractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rb;
    public Rigidbody2D rb => _rb;

    [Header("Config")]
    [SerializeField] private float gravityForce;
    [SerializeField] private float forceRadius;
    public Vector2 centre => rb.transform.position;


    private void Update()
    {
        // Attract all objects
        foreach (GravityObject obj in GravityObject.gravityObjects)
        {
            Vector2 target = rb.ClosestPoint(obj.centre);
            Vector2 dir = target - obj.centre;
            if (dir.magnitude < forceRadius)
            {
                float force = gravityForce * (rb.mass * obj.rb.mass) / dir.magnitude;
                obj.rb.AddForce(dir.normalized * force);
            }
        }
    }
}
