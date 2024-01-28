using UnityEngine;

public class Biome : ScriptableObject
{
    public Color[] colorRange = new Color[] { Color.black, Color.white };
    public NoiseData energyMaxNoise = new NoiseData(new float[2] { 40, 200 });
    public NoiseData energyPctNoise = new NoiseData();
    public float deadspotChance = 0.02f;
    public float deadspotPct = 0.15f;
}
