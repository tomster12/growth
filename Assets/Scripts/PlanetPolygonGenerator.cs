
using System;
using UnityEngine;
using static GK.VoronoiClipper;


public class PlanetPolygonGenerator : MonoBehaviour
{
    [Serializable]
    public class PlanetShapeInfo
    {
        public int vertexCount;
        public float[][] noiseDatas = new float[][]
        {
            // Scale, Mid-Level, Magnitude 
            new float[] { 1.0f, 2.0f, 0.5f }
        };
    }

    [Header("References")]
    [SerializeField] private PolygonCollider2D polygon;
    

    public void GeneratePlanet(PlanetShapeInfo info)
    {
        // Generate points in a circle
        Vector2[] points = new Vector2[info.vertexCount];
        for (int i = 0; i < points.Length; i++)
        {
            float pct = (2 * Mathf.PI) * (float)i / (points.Length - 1);
            float value = 0;

            // Add each noise to value
            foreach (float[] noiseData in info.noiseDatas)
            {
                Vector2 perlinOffset = noiseData[0] * 0.5f * Vector2.one;
                float perlinMagnitude = noiseData[0] * 2.0f;
                float perlinAngle = pct * (2 * Mathf.PI);
                value += noiseData[1] + noiseData[2] * Mathf.PerlinNoise(
                    perlinOffset.x + perlinMagnitude * Mathf.Cos(perlinAngle),
                    perlinOffset.y + perlinMagnitude * Mathf.Sin(perlinAngle)
                );
            }

            // Create and add point
            points[i] = value * new Vector2(Mathf.Cos(pct), Mathf.Sin(pct));
        }

        // Assign points to the polygon
        polygon.SetPath(0, points);
    }
}
