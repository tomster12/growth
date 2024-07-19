using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundMaterial", menuName = "Ground Material")]
public class GroundMaterial : ScriptableObject
{
    [SerializeField] public String materialName = "NA";
    [SerializeField] public Color[] materialColorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };

    [SerializeField] public Boolean hasFoliage = false;
    [SerializeField] public GameObject foliagePfb;
    [SerializeField] public Color[] foliageColorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };
    [SerializeField] public NoiseData foliageHeightNoise = new NoiseData(new float[2] { 0.5f, 1.0f });
}
