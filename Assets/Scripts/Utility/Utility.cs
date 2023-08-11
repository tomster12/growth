
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;


public static class Utility
{
    public static float AreaOfTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        // Mathematical area of triangle from A, B, C
        return Mathf.Abs(Vector3.Cross(A - B, A - C).z) * 0.5f;
    }

    public static Vector2 RandomInTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        // Return a random point in a triangle
        float r1 = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f));
        float r2 = UnityEngine.Random.Range(0f, 1f);
        float m1 = 1 - r1;
        float m2 = r1 * (1 - r2);
        float m3 = r2 * r1;
        return (m1 * A) + (m2 * B) + (m3 * C);
    }

    private static int[] RandomInPolygon_tris;
    private static Vector3[] RandomInPolygon_verts;
    private static int RandomInPolygon_triCount;
    private static float[] RandomInPolygon_triangleAreas;
    private static float RandomInPolygon_totalArea;
    public static Vector2 RandomInPolygon(PolygonCollider2D polygon, bool useCached = false)
    {
        // Generate all variables and store
        if (!useCached)
        {
            Mesh mesh = polygon.CreateMesh(true, false);
            RandomInPolygon_tris = mesh.triangles;
            RandomInPolygon_verts = mesh.vertices;
            RandomInPolygon_triCount = RandomInPolygon_tris.Length / 3;
            RandomInPolygon_triangleAreas = new float[RandomInPolygon_triCount];
            RandomInPolygon_totalArea = 0.0f;
            for (int i = 0; i < RandomInPolygon_triCount; i++)
            {
                RandomInPolygon_triangleAreas[i] = AreaOfTriangle(
                    RandomInPolygon_verts[RandomInPolygon_tris[i * 3]],
                    RandomInPolygon_verts[RandomInPolygon_tris[i * 3 + 1]],
                    RandomInPolygon_verts[RandomInPolygon_tris[i * 3 + 2]]);
                RandomInPolygon_totalArea += RandomInPolygon_triangleAreas[i];
            }
        }

        // Error check mesh has valid triangles
        if (RandomInPolygon_triCount == 0) { Debug.LogError("triangleCount == 0"); return Vector2.zero; }
        if (RandomInPolygon_totalArea == 0.0f) { Debug.LogError("Area == 0.0f"); return Vector2.zero; }

        // Pick a random triangle weighted by area
        int triangle = -1;
        float r = UnityEngine.Random.Range(0.0f, RandomInPolygon_totalArea);
        for (int i = 0; i < RandomInPolygon_triCount && triangle == -1; i++)
        {
            if (r < RandomInPolygon_triangleAreas[i]) triangle = i;
            else r -= RandomInPolygon_triangleAreas[i];
        }

        // Error check picked a triangle
        if (triangle == -1) { Debug.LogError("did not pick a triangle"); return Vector2.zero; }

        // Pick a random point within the triangle
        return RandomInTriangle(
            RandomInPolygon_verts[RandomInPolygon_tris[triangle * 3]],
            RandomInPolygon_verts[RandomInPolygon_tris[triangle * 3 + 1]],
            RandomInPolygon_verts[RandomInPolygon_tris[triangle * 3 + 2]]
        );
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
    
    public static float DistanceToPoints(Vector2 p, Vector2[] points)
    {
        float minDist = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[(i + 1) % points.Length];
            Vector2 l01 = (p1 - p0);
            float ld = l01.sqrMagnitude;
            float d = float.MaxValue;
            if (ld == 0.0) d = (p0 - p).magnitude;
            else
            {
                float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - p0, l01) / ld));
                Vector2 projection = p0 + t * l01;
                d = (projection - p).magnitude;
            }
            if (d < minDist) minDist = d;
        }
        return minDist;
    }
}
