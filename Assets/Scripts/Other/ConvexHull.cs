using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class ConvexHull
{
    public static double OriginCross(Vector2 O, Vector2 A, Vector2 B)
    {
        return (A.x - O.x) * (B.y - O.y) - (A.y - O.y) * (B.x - O.x);
    }

    public static List<Vector2> GetConvexHull(List<Vector2> points)
    {
        if (points == null)
        {
            return null;
        }

        if (points.Count <= 1)
        {
            return points;
        }

        points.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        int k = 0;
        List<Vector2> H = new List<Vector2>(new Vector2[2 * points.Count]);

        // Build lower hull
        for (int i = 0; i < points.Count; ++i)
        {
            while (k >= 2 && OriginCross(H[k - 2], H[k - 1], points[i]) <= 0)
            {
                k--;
            }
            H[k++] = points[i];
        }

        // Build upper hull
        for (int i = points.Count - 2, t = k + 1; i >= 0; i--)
        {
            while (k >= t && OriginCross(H[k - 2], H[k - 1], points[i]) <= 0)
            {
                k--;
            }
            H[k++] = points[i];
        }

        return H.Take(k - 1).ToList();
    }
}
