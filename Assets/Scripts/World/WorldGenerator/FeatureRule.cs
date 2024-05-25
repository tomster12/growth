using System;
using UnityEngine;

[Serializable]
public class FeatureRule
{
    [SerializeField] public bool everyEdge;
    [SerializeField] public bool canOverlapTerrain = false;
    [SerializeField] public float averagePer100;
    [SerializeField] public float minDistance;
    [SerializeField] public GameObject feature;
};
