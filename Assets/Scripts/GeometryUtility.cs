
using System.Collections.Generic;
using UnityEngine;


public static class GeometryUtility
{

    public static float AreaOfTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        // Mathematical area of triangle from A, B, C
        return Mathf.Abs(Vector3.Cross(A - B, A - C).z) * 0.5f;
    }


    public static Vector2 RandomInTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        // Return a random point in a triangle
        float r1 = Mathf.Sqrt(Random.Range(0f, 1f));
        float r2 = Random.Range(0f, 1f);
        float m1 = 1 - r1;
        float m2 = r1 * (1 - r2);
        float m3 = r2 * r1;
        return (m1 * A) + (m2 * B) + (m3 * C);
    }


    private static float[] RandomInPolygon_triangleAreas;
    private static float RandomInPolygon_totalArea;
    public static Vector2 RandomInPolygon(PolygonCollider2D polygon, bool useCached = false)
    {
        // Create mesh from polygon
        Mesh mesh = polygon.CreateMesh(true, false);
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;

        // Calculate triangle / total area
        int triangleCount = tris.Length / 3;
        if (!useCached || RandomInPolygon_triangleAreas == null)
        {
            RandomInPolygon_triangleAreas = new float[triangleCount];
            RandomInPolygon_totalArea = 0.0f;
            for (int i = 0; i < triangleCount; i++)
            {
                RandomInPolygon_triangleAreas[i] = AreaOfTriangle(verts[tris[i * 3]], verts[tris[i * 3 + 1]], verts[tris[i * 3 + 2]]);
                RandomInPolygon_totalArea += RandomInPolygon_triangleAreas[i];
            }
        }

        // Pick a random triangle weighted by area
        if (triangleCount == 0) { Debug.LogError("triangleCount == 0"); return Vector2.zero; }
        if (RandomInPolygon_totalArea == 0.0f) { Debug.LogError("Area == 0.0f"); return Vector2.zero; }
        int triangle = -1;
        float r = Random.Range(0.0f, RandomInPolygon_totalArea);
        for (int i = 0; i < triangleCount && triangle == -1; i++)
        {
            if (r < RandomInPolygon_triangleAreas[i]) triangle = i;
            else r -= RandomInPolygon_triangleAreas[i];
        }

        // Pick random point within the triangle
        if (triangle == -1) { Debug.LogError("did not pick a triangle"); return Vector2.zero; }
        return RandomInTriangle(verts[tris[triangle * 3]], verts[tris[triangle * 3 + 1]], verts[tris[triangle * 3 + 2]]);
    }


    public static Vector2 GetAveragePoint(List<Vector2> points)
    {
        Vector2 sum = Vector2.zero;
        points.ForEach(p => sum += p);
        return sum / points.Count;
    }


    public static bool PointInside(Vector2 p, List<Vector2> points)
    {
        int j = points.Count - 1;
        bool inside = false;
        for (int i = 0; i < points.Count; j = i++)
        {
            var pi = points[i];
            var pj = points[j];
            if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                inside = !inside;
        }
        return inside;
    }
}
