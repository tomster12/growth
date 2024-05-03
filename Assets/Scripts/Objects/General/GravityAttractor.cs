using UnityEngine;

public class GravityAttractor : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] public float gravityForce = 200.0f;
    [SerializeField] public float gravityRadius = 200.0f; // Standard Range: 150-300
    [SerializeField] public float minimumDistance = 3.0f;
    [SerializeField] public bool rigidbodySurface = true;

    public Rigidbody2D RB => _RB;
    public PolygonCollider2D PolygonCollider => _polygonCollider;
    public Vector2 Centre => RB.transform.position;

    public Vector2 ClosestPoint(Vector2 pos) => rigidbodySurface ? RB.ClosestPoint(pos) : PolygonCollider.ClosestPoint(pos);

    [Header("References")]
    [SerializeField] private Rigidbody2D _RB;
    [SerializeField] private PolygonCollider2D _polygonCollider;

    private void FixedUpdate()
    {
        // Attract active all objects
        foreach (GravityObject obj in GravityObject.gravityObjects)
        {
            if (!obj.IsEnabled) continue;
            if (!obj.RB.simulated) continue;

            // If within gravity radius
            Vector2 centreDir = Centre - obj.Centre;
            if (centreDir.magnitude < gravityRadius)
            {
                // On the surface for some reason
                Vector2 surface = ClosestPoint(obj.Centre);
                Vector2 surfaceDir = surface - obj.Centre;
                if (surfaceDir.magnitude == 0) continue;

                float cleanMagnitude = Mathf.Max(surfaceDir.magnitude, minimumDistance);
                float force = gravityForce * (RB.mass * obj.RB.mass) / cleanMagnitude;
                obj.AddForce(surfaceDir.normalized * force);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw arrows for centre dir and surface dir for each object
        foreach (GravityObject obj in GravityObject.gravityObjects)
        {
            if (!obj) continue;
            if (!obj.IsEnabled) continue;
            if (!obj.RB.simulated) continue;

            // If within gravity radius
            Vector2 centreDir = Centre - obj.Centre;
            if (centreDir.magnitude < gravityRadius)
            {
                // On the surface for some reason
                Vector2 surface = ClosestPoint(obj.Centre);
                Vector2 surfaceDir = surface - obj.Centre;
                if (surfaceDir.magnitude == 0) continue;

                Gizmos.color = Color.green;
                Gizmos.DrawLine(obj.Centre, obj.Centre + surfaceDir);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(obj.Centre, obj.Centre + centreDir);
            }
        }
    }
}
