using System;
using UnityEngine;

[Serializable]
public class WorldFeatureRule_Old
{
    [SerializeField] public bool everyEdge;
    [SerializeField] public bool canOverlapTerrain = false;
    [SerializeField] public float averagePer100;
    [SerializeField] public float minDistance;

    [SerializeField] public GameObject feature;
    [SerializeReference] public WorldFeatureConfig config;
};
