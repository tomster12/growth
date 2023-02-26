
using System;
using UnityEngine;


public class PlanetPolygonGenerator : MonoBehaviour
{

    [Serializable]
    public class PlanetShapeInfo
    {
        public int vertexCount;
        public NoiseData[] noiseData;
    }


    // --- Parameters ---
    [Header("Parameters")]
    [SerializeField] public PolygonCollider2D outsidePolygon;
    [SerializeField] public PlanetShapeInfo shapeInfo;

    // --- Output ---
    public Vector2[] points { get; private set; }


    public void Generate(PolygonCollider2D outsidePolygon, PlanetShapeInfo shapeInfo)
    {
        this.outsidePolygon = outsidePolygon;
        this.shapeInfo = shapeInfo;
        Generate();
    }

    [ContextMenu("Generate Polygon")]
    public void Generate()
    {
        // Clear output
        ClearOutput();

        // Generate points in a circle
        points = new Vector2[shapeInfo.vertexCount];
        for (int i = 0; i < points.Length; i++)
        {
            // Add each noise to value
            float pct = (float)i / points.Length;
            float value = 0;
            foreach (NoiseData noiseData in shapeInfo.noiseData) value += noiseData.GetCyclicNoise(pct);

            // Create and add point
            points[i] = value * new Vector2(Mathf.Cos(pct * Mathf.PI * 2), Mathf.Sin(pct * Mathf.PI * 2));
        }

        // Assign points to the polygon
        outsidePolygon.SetPath(0, points);
    }


    public void ClearOutput()
    {
        // Clear output variables
        points = null;
    }
}
