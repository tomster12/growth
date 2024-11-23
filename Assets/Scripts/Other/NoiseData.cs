﻿using System;
using UnityEngine;

[Serializable]
public class NoiseData
{
    [SerializeField] public float[] valueRange;
    [SerializeField] public float noiseScale = 1.0f;

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

    public void RandomizeOffset() => currentOffset = -100000 + UnityEngine.Random.value * 200000;

    public float GetNoise(Vector2 pos) => GetNoise(pos.x, pos.y);

    public float GetNoise(float x, float y)
    {
        return valueRange[0] + Mathf.PerlinNoise(x * noiseScale + currentOffset, y * noiseScale + currentOffset) * (valueRange[1] - valueRange[0]);
    }

    public float GetCyclicNoise(float pct)
    {
        // Get noise on a circle
        float a = pct * (2 * Mathf.PI);
        float x = 0.5f + 0.5f * Mathf.Cos(a);
        float y = 0.5f + 0.5f * Mathf.Sin(a);
        return GetNoise(x, y);
    }

    private float currentOffset = 0;
}
