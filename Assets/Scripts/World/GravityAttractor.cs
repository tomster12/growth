
using UnityEngine;


public class GravityAttractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _RB;
    [SerializeField] private PolygonCollider2D _polygonCollider;
    
    [Header("Config")]
    [SerializeField] public float gravityForce = 200.0f; // Standard Range: 150-300
    [SerializeField] public float gravityRadius = 200.0f;
    [SerializeField] public float minimumDistance = 3.0f;
    [SerializeField] public bool rigidbodySurface = true;

    public Rigidbody2D RB => _RB;
    public PolygonCollider2D PolygonCollider => _polygonCollider;
    public Vector2 Centre => RB.transform.position;


    public Vector2 ClosestPoint(Vector2 pos) => rigidbodySurface ? RB.ClosestPoint(pos) : PolygonCollider.ClosestPoint(pos);


    private void FixedUpdate()
    {
        // Attract all objects
        foreach (GravityObject obj in GravityObject.gravityObjects)
        {
            if (!obj.IsEnabled) continue;
            if (!obj.rb.simulated) continue;
            Vector2 centreDir = Centre - obj.Centre;
            if (centreDir.magnitude < gravityRadius)
            {
                Vector2 surface = ClosestPoint(obj.Centre);
                Vector2 surfaceDir = surface - obj.Centre;
                if (surfaceDir.magnitude == 0) continue;
                float cleanMagnitude = Mathf.Max(surfaceDir.magnitude, minimumDistance);
                float force = gravityForce * (RB.mass * obj.rb.mass) / cleanMagnitude;
                obj.rb.AddForce(surfaceDir.normalized * force);
            }
        }
    }
}
