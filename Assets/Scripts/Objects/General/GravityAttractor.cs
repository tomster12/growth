using UnityEngine;

public class GravityAttractor : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] public float gravityForce = 200.0f;
    [SerializeField] public float gravityRadius = 200.0f; // Standard Range: 150-300
    [SerializeField] public float minimumDistance = 3.0f;

    public Rigidbody2D RB => rb;
    public PolygonCollider2D PolygonCollider => polygonCollider;
    public Vector2 Centre => RB.position;

    public Vector2 GetGravityDir(GravityObject obj)
    {
        return obj.UseRBSurface ? RB.ClosestPoint(obj.Centre) - obj.Centre : PolygonCollider.ClosestPoint(obj.Centre) - obj.Centre;
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PolygonCollider2D polygonCollider;

    private void FixedUpdate()
    {
        // Attract active all objects
        foreach (GravityObject obj in GravityObject.gravityObjects)
        {
            // If within gravity radius
            Vector2 centreDir = Centre - obj.Centre;
            if (centreDir.sqrMagnitude < gravityRadius * gravityRadius)
            {
                Vector2 surfaceDir = GetGravityDir(obj);

                // On the surface for some reason
                if (surfaceDir.sqrMagnitude == 0) continue;

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
            if (!obj.IsKinematic) continue;
            if (!obj.RB.simulated) continue;

            // If within gravity radius
            Vector2 centreDir = Centre - obj.Centre;
            if (centreDir.magnitude < gravityRadius)
            {
                Vector2 surfaceDir = GetGravityDir(obj);

                // On the surface for some reason
                if (surfaceDir.magnitude == 0) continue;

                Gizmos.color = Color.green;
                Gizmos.DrawLine(obj.Centre, obj.Centre + surfaceDir);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(obj.Centre, obj.Centre + centreDir);
            }
        }
    }
}
