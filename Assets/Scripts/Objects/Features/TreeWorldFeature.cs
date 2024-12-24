using System.Globalization;
using UnityEngine;

public class TreeWorldFeature : MonoBehaviour, IWorldFeature
{
    public Transform Transform => transform;

    public void Place(WorldSurfaceEdge edge, float edgePct, WorldFeatureConfig config)
    {
        // Position and rotate
        float t = 0.25f + Random.value * 0.5f;
        transform.position = Utility.WithZ(Vector2.Lerp(edge.a, edge.b, t), transform.position.z);
        Vector2 edgeUp = Vector2.Perpendicular(edge.b - edge.a);
        Vector2 worldUp = edge.centre - edge.worldSite.world.GetCentre();
        transform.up = (edgeUp + worldUp) / 2.0f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, transform.eulerAngles.z - 6.0f + Random.value * 12.0f);
        transform.position += -transform.up * embedDistance;

        // Generate tree
        treeGenerator.Generate();
    }

    public bool Contains(Vector2 point)
    {
        return (point - (Vector2)transform.position).sqrMagnitude < (treeGenerator.BaseWidth * treeGenerator.BaseWidth);
    }

    [Header("References")]
    [SerializeField] private TreeGenerator treeGenerator;
    [SerializeField] private float embedDistance;

    private void Update()
    {
        // Update each tree node by the wind
        foreach (TreeNode node in treeGenerator.TreeNodes)
        {
            // Calculate wind blowing against the tree
            Vector2 centre = node.transform.position + node.transform.up * node.length / 2.0f;
            Vector2 windDir = GlobalWind.GetWind(centre);
            float dirDot = Vector2.Dot(node.transform.right, windDir);
            float windAmount = treeGenerator.TreeData.WindStrength * dirDot * ((node.groundDistance + 1) / 6.0f) / node.width;

            // Rotate based on wind, lerp towards base with resistance
            float newAngle = node.transform.localEulerAngles.z + windAmount * Time.deltaTime;
            float resistedAngle = Mathf.LerpAngle(newAngle, node.localAngle, treeGenerator.TreeData.BranchResistance);
            node.transform.localEulerAngles = new Vector3(0.0f, 0.0f, resistedAngle);
        }
    }
}
