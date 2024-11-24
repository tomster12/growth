using UnityEngine;

public class WorldSpacePointTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D cl;

    private Vector2[] points;

    private void Update()
    {
        UpdatePoints();
    }

    [ContextMenu("Update Points")]
    private void UpdatePoints()
    {
        points = Utility.GetWorldSpacePoints(cl);
    }

    private void OnDrawGizmos()
    {
        if (points == null) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawSphere(points[i], 0.1f);
        }
    }
}
