
using System;
using UnityEngine;


[Serializable]
public class PlanetShapeInfo
{
    public int vertexCount;
    public NoiseData[] noiseData;
}

public class PlanetPolygonGenerator : MonoBehaviour, IGenerator
{
    [Header("Parameters")]
    [SerializeField] public PolygonCollider2D outsidePolygon;
    [SerializeField] public PlanetShapeInfo shapeInfo;

    public Vector2[] points { get; private set; }
    public bool isGenerated { get; private set; } = false;
    public bool IsGenerated() => isGenerated;


    public void Clear()
    {
        outsidePolygon.points = new Vector2[0];
        points = null;
        isGenerated = false;
    }

    public void Generate()
    {
        Clear();

        // Generate points in a circle
        points = new Vector2[shapeInfo.vertexCount];
        foreach (NoiseData noiseData in shapeInfo.noiseData) noiseData.RandomizeOffset();
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

        isGenerated = true;
    }
    
    public string GetName() => "Poly. Planet";

    public float[] GetSurfaceRange()
    {
        float min = 0, max = 0;
        foreach (NoiseData noiseData in shapeInfo.noiseData)
        {
            min += noiseData.valueRange[0];
            max += noiseData.valueRange[1];
        }
        return new float[] { min, max };
    }
}
