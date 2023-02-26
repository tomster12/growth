
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class NoiseData
{
    [SerializeField] public float[] valueRange;
    [SerializeField] public float noiseScale = 1.0f;

    public float seedOffset => (500.0f * Mathf.Abs(GeneratorController.globalSeed) / int.MaxValue);


    public NoiseData()
    {
        this.valueRange = new float[] { 0.0f, 1.0f };
        this.noiseScale = 1.0f;
    }

    public NoiseData(float[] valueRange)
    {
        this.valueRange = valueRange;
        this.noiseScale = 1.0f;
    }

    public NoiseData(float[] valueRange, float noiseScale)
    {
        this.valueRange = valueRange;
        this.noiseScale = noiseScale;
    }


    public float GetNoise(Vector2 pos) => GetNoise(pos.x, pos.y);

    public float GetNoise(float x, float y)
    {
        return valueRange[0] + Mathf.PerlinNoise(x * noiseScale + seedOffset, y * noiseScale + seedOffset) * (valueRange[1] - valueRange[0]);
    }


    public float GetCyclicNoise(float pct)
    {
        // Get noise on a circle
        float a = pct * (2 * Mathf.PI);
        float x = 0.5f + 0.5f * Mathf.Cos(a);
        float y = 0.5f + 0.5f * Mathf.Sin(a);
        return GetNoise(x, y);
    }
}
