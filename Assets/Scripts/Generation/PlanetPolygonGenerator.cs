
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


    public void Clear()
    {
        points = null;
        isGenerated = false;
    }

    public void Generate(PolygonCollider2D outsidePolygon, PlanetShapeInfo shapeInfo)
    {
        this.outsidePolygon = outsidePolygon;
        this.shapeInfo = shapeInfo;
        Generate();
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


    public bool GetIsGenerated() => isGenerated;

    public string GetName() => gameObject.name;    
}
