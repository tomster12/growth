using UnityEngine;

public class WindWobbler : MonoBehaviour
{
    [SerializeField] private Transform child;
    private Vector3 latestDir;

    private void Update()
    {
        Vector3 windDir = GlobalWind.GetWind(transform.position);
        latestDir = windDir;
        child.localPosition = windDir;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(latestDir.x, latestDir.y, latestDir.z);
        Gizmos.DrawSphere(transform.position, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, child.position);
    }
}
