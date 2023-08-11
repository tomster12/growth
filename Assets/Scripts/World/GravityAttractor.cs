
using UnityEngine;


public class GravityAttractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private PolygonCollider2D _polygonCollider;
    
    public Rigidbody2D rb => _rb;
    public PolygonCollider2D polygonCollider => _polygonCollider;
    public Vector2 centre => rb.transform.position;

    [Header("Config")]
    [SerializeField] public float gravityForce = 200.0f; // Standard Range: 150-300
    [SerializeField] public float gravityRadius = 200.0f;
    [SerializeField] public float minimumDistance = 3.0f;
    [SerializeField] public bool rigidbodySurface = true;


    public Vector2 ClosestPoint(Vector2 pos) => rigidbodySurface ? rb.ClosestPoint(pos) : polygonCollider.ClosestPoint(pos);


    private void FixedUpdate()
    {
        // Attract all objects
        foreach (GravityObject obj in GravityObject.gravityObjects)
        {
            if (!obj.isEnabled) continue;
            if (!obj.rb.simulated) continue;
            Vector2 centreDir = centre - obj.centre;
            if (centreDir.magnitude < gravityRadius)
            {
                Vector2 surface = ClosestPoint(obj.centre);
                Vector2 surfaceDir = surface - obj.centre;
                if (surfaceDir.magnitude == 0) continue;
                float cleanMagnitude = Mathf.Max(surfaceDir.magnitude, minimumDistance);
                float force = gravityForce * (rb.mass * obj.rb.mass) / cleanMagnitude;
                obj.rb.AddForce(surfaceDir.normalized * force);
            }
        }
    }
}
