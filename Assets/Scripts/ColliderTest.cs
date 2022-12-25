
using PixelsForGlory.VoronoiDiagram;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ColliderTest : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D collider;

    private Vector2[] vertices;


    [ContextMenu("Run 1")]
    private void Type1()
    {
        vertices = new Vector2[16];

        vertices[0] = new Vector2(0.00f, 0.00f);
        vertices[1] = new Vector2(0.33f, 0.00f);
        vertices[2] = new Vector2(0.67f, 0.00f);
        vertices[3] = new Vector2(1.00f, 0.00f);

        vertices[4] = new Vector2(1.00f, 0.33f);
        vertices[5] = new Vector2(1.00f, 0.67f);

        vertices[6] = new Vector2(1.00f, 1.00f);
        vertices[7] = new Vector2(0.67f, 1.00f);
        vertices[8] = new Vector2(0.33f, 1.00f);
        vertices[9] = new Vector2(0.00f, 1.00f);

        vertices[10] = new Vector2(0.00f, 0.67f);
        vertices[11] = new Vector2(0.00f, 0.33f);

        vertices[12] = new Vector2(0.33f, 0.33f);
        vertices[13] = new Vector2(0.67f, 0.33f);
        vertices[14] = new Vector2(0.67f, 0.67f);
        vertices[15] = new Vector2(0.33f, 0.67f);

        collider.pathCount = 2;
        collider.SetPath(0, vertices.Take(12).ToArray());
        collider.SetPath(1, vertices.Skip(12).Take(4).ToArray());
    }

    [ContextMenu("Run 2")]
    private void Type2()
    {
        vertices = new Vector2[12];

        vertices[0] = new Vector2(0.00f, 0.00f);
        vertices[1] = new Vector2(0.00f, 0.33f);
        vertices[2] = new Vector2(0.00f, 0.67f);
        vertices[3] = new Vector2(0.00f, 1.00f);

        vertices[4] = new Vector2(0.33f, 1.00f);
        vertices[5] = new Vector2(0.67f, 1.00f);

        vertices[6] = new Vector2(1.00f, 1.00f);
        vertices[7] = new Vector2(1.00f, 0.67f);
        vertices[8] = new Vector2(1.00f, 0.33f);
        vertices[9] = new Vector2(1.00f, 0.00f);

        vertices[10] = new Vector2(0.67f, 0.00f);
        vertices[11] = new Vector2(0.33f, 0.00f);

        collider.pathCount = 1;
        collider.SetPath(0, vertices);
    }

    [ContextMenu("Run 3")]
    private void Type3()
    {
        vertices = new Vector2[4];

        vertices[0] = new Vector2(0.00f, 0.00f);
        vertices[1] = new Vector2(0.00f, 1.00f);
        vertices[2] = new Vector2(1.00f, 1.00f);
        vertices[3] = new Vector2(1.00f, 0.00f);

        collider.pathCount = 1;
        collider.SetPath(0, vertices);
    }
}
