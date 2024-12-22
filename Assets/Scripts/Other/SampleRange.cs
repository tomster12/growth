using System;
using UnityEngine;

public enum SampleTypes { UNIFORM, GAUSSIAN, EXPONENTIAL }

[Serializable]
public class SampleRange
{
    [SerializeField] public float Min = 0.0f;
    [SerializeField] public float Max = 1.0f;
    [SerializeField] public SampleTypes SampleType = SampleTypes.UNIFORM;

    public SampleRange(float min, float max, SampleTypes sampleType)
    {
        Min = min;
        Max = max;
        SampleType = sampleType;
    }

    public float GetSample()
    {
        switch (SampleType)
        {
            case SampleTypes.UNIFORM:
                return UnityEngine.Random.Range(Min, Max);

            case SampleTypes.GAUSSIAN:
                float u1 = 1.0f - UnityEngine.Random.value;
                float u2 = 1.0f - UnityEngine.Random.value;
                float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
                float mean = (Min + Max) / 2.0f;
                float stdDev = (Max - Min) / 6.0f;
                return mean + stdDev * randStdNormal;

            case SampleTypes.EXPONENTIAL:
                float lambda = 1.0f / ((Min + Max) / 2.0f);
                return -1.0f / lambda * Mathf.Log(UnityEngine.Random.value);

            default:
                return UnityEngine.Random.Range(Min, Max);
        }
    }
}
