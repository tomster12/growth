
using UnityEngine;


public class GrassGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PolygonCollider2D sourceCollider;
    [SerializeField] Transform grassContainer;
    [SerializeField] GameObject grassPfb;


    [ContextMenu("Clear current")]
    public void ClearCurrent()
    {
        // Delete container children
        for (int i = grassContainer.childCount - 1; i >= 0; i--) GameObject.DestroyImmediate(grassContainer.GetChild(i).gameObject);
    }


    [ContextMenu("Generate Grass")]
    public void GenerateGrass()
    {
        // Get edge points and clear current
        Vector2[] edgePoints = sourceCollider.points;
        ClearCurrent();

        // For each edge
        for (int i = edgePoints.Length - 1; i >= 0; i--)
        {
            Vector2 a = edgePoints[i];
            Vector2 b = edgePoints[(i + edgePoints.Length - 1) % edgePoints.Length];
            Vector2 dir = (b - a);
            bool toFlipX = UnityEngine.Random.value < 0.5f;

            // Generate grass and set position
            GameObject grass = Instantiate(grassPfb);
            grass.transform.parent = grassContainer;
            if (toFlipX) grass.transform.position = transform.TransformPoint(a + dir);
            else grass.transform.position = transform.TransformPoint(a);
            grass.transform.right = dir.normalized;

            // Grow sprite to correct size
            SpriteRenderer sprite = grass.GetComponent<SpriteRenderer>();
            sprite.size = new Vector2(dir.magnitude, sprite.size.y);
            sprite.flipX = toFlipX;
        }
    }
}
