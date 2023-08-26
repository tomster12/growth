
using UnityEngine;
using System;


[Serializable]
public class EdgeRule
{
    [SerializeField] public bool isGuaranteed;
    [SerializeField] public float averagePer100;
    [SerializeField] public float minDistance;
    [SerializeField] public GameObject feature;
};


public class Biome : ScriptableObject
{
    public Color[] colorRange = new Color[] { Color.black, Color.white };
    public NoiseData energyMaxNoise = new NoiseData(new float[2] { 40, 200 });
    public NoiseData energyPctNoise = new NoiseData();
    public float deadspotChance = 0.02f;
    public float deadspotPct = 0.15f;
    public EdgeRule[] frontDecorRules = new EdgeRule[0];
    public EdgeRule[] terrainRules = new EdgeRule[0];
    public EdgeRule[] foregroundRules = new EdgeRule[0];
    public EdgeRule[] backgroundRules = new EdgeRule[0];
    public EdgeRule[] backDecorRules = new EdgeRule[0];
}
