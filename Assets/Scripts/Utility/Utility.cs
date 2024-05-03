using System.Collections.Generic;
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

    public static Vector2 RandomInPolygon(PolygonCollider2D polygon, bool useCached = false)
    {
        // Generate all variables and store
        if (!useCached)
        {
            Mesh mesh = polygon.CreateMesh(true, false);
            cacheRandomInPolygonTris = mesh.triangles;
            cacheRandomInPolygonVerts = mesh.vertices;
            cacheRandomInPolygonTriCount = cacheRandomInPolygonTris.Length / 3;
            cacheRandomInPolygonTriAreas = new float[cacheRandomInPolygonTriCount];
            cacheRandomInPolygonTotalArea = 0.0f;
            for (int i = 0; i < cacheRandomInPolygonTriCount; i++)
            {
                cacheRandomInPolygonTriAreas[i] = AreaOfTriangle(
                    cacheRandomInPolygonVerts[cacheRandomInPolygonTris[i * 3]],
                    cacheRandomInPolygonVerts[cacheRandomInPolygonTris[i * 3 + 1]],
                    cacheRandomInPolygonVerts[cacheRandomInPolygonTris[i * 3 + 2]]);
                cacheRandomInPolygonTotalArea += cacheRandomInPolygonTriAreas[i];
            }
        }

        // Error check mesh has valid triangles
        if (cacheRandomInPolygonTriCount == 0) { Debug.LogError("triangleCount == 0"); return Vector2.zero; }
        if (cacheRandomInPolygonTotalArea == 0.0f) { Debug.LogError("Area == 0.0f"); return Vector2.zero; }

        // Pick a random triangle weighted by area
        int triangle = -1;
        float r = UnityEngine.Random.Range(0.0f, cacheRandomInPolygonTotalArea);
        for (int i = 0; i < cacheRandomInPolygonTriCount && triangle == -1; i++)
        {
            if (r < cacheRandomInPolygonTriAreas[i]) triangle = i;
            else r -= cacheRandomInPolygonTriAreas[i];
        }

        // Error check picked a triangle
        if (triangle == -1) { Debug.LogError("did not pick a triangle"); return Vector2.zero; }

        // Pick a random point within the triangle
        return RandomInTriangle(
            cacheRandomInPolygonVerts[cacheRandomInPolygonTris[triangle * 3]],
            cacheRandomInPolygonVerts[cacheRandomInPolygonTris[triangle * 3 + 1]],
            cacheRandomInPolygonVerts[cacheRandomInPolygonTris[triangle * 3 + 2]]
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
            float d;
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

    public static void SetLayer(Transform t, int layer)
    {
        // Recursive transform layer update
        t.gameObject.layer = layer;
        foreach (Transform c in t) SetLayer(c, layer);
    }

    public static float CalculateBezierLength(Vector2 p0, Vector2 p1, Vector2 p2, int segments)
    {
        float length = 0;
        Vector2 previousPoint = CalculateBezierPoint(p0, p1, p2, 0);

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 currentPoint = CalculateBezierPoint(p0, p1, p2, t);
            length += Vector2.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        return length;
    }

    public static Vector2 CalculateBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1 - t;
        float uu = u * u;
        float tt = t * t;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }

    public static class Easing
    {
        // https://easings.net/

        public static float EaseInSine(float x) => 1 - Mathf.Cos((x * Mathf.PI) / 2);

        public static float EaseOutSine(float x) => Mathf.Sin((x * Mathf.PI) / 2);

        public static float EaseOutCubic(float x) => 1 - Mathf.Pow(1 - x, 3);

        public static float EaseInExpo(float x) => x == 0 ? 0 : Mathf.Pow(2, 10 * x - 10);

        public static float EaseOutExpo(float x) => x == 1 ? 1 : 1 - Mathf.Pow(2, -10 * x);
    }

    private static int[] cacheRandomInPolygonTris;
    private static Vector3[] cacheRandomInPolygonVerts;
    private static int cacheRandomInPolygonTriCount;
    private static float[] cacheRandomInPolygonTriAreas;
    private static float cacheRandomInPolygonTotalArea;
}
