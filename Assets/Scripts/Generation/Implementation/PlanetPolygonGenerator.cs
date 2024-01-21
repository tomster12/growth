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
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private PlanetShapeInfo shapeInfo;

    public Vector2[] Points { get; private set; }
    public bool IsGenerated { get; private set; } = false;
    public string Name => "Poly. Planet";


    public void Clear()
    {
        outsidePolygon.points = new Vector2[0];
        Points = null;
        IsGenerated = false;
    }

    public void Generate()
    {
        Clear();

        // Generate points in a circle
        Points = new Vector2[shapeInfo.vertexCount];
        foreach (NoiseData noiseData in shapeInfo.noiseData) noiseData.RandomizeOffset();
        for (int i = 0; i < Points.Length; i++)
        {
            // Add each noise to value
            float pct = (float)i / Points.Length;
            float value = 0;
            foreach (NoiseData noiseData in shapeInfo.noiseData) value += noiseData.GetCyclicNoise(pct);

            // Create and add point
            Points[i] = value * new Vector2(Mathf.Cos(pct * Mathf.PI * 2), Mathf.Sin(pct * Mathf.PI * 2));
        }

        // Assign points to the polygon
        outsidePolygon.SetPath(0, Points);

        IsGenerated = true;
    }

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
