
using System;
using System.Collections.Generic;
using UnityEngine;
using static VoronoiMeshGenerator;


public class World : MonoBehaviour
{
    public enum ColorMode { ENERGY, RANDOM, DEPTH };

    public class WorldSite
    {
        public MeshSite meshSite;
        public int edgeDistance = -1;
        public float maxEnergy = 0;
    }


    [Header("References")]
    [SerializeField] private VoronoiMeshGenerator meshGenerator;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material meshMaterial;

    [Header("Config")]
    [SerializeField] private int setSeed = -1;
    [SerializeField] private float energyNoiseScale = 0.2f;
    [SerializeField] private float[] energyRandomRange = new float[] { 50, 100 };
    [SerializeField] private Color[] groundColorRange = new Color[] { new Color(0,0,0), new Color(1,1,1) };

    private List<WorldSite> worldSites;


    [ContextMenu("Full Generate World")]
    public void FullGenerateWorld()
    {
        // Run all procedures
        _ResetPipeline();
        _GenerateMesh();
        _GenerateSites();

        // Set color mode
        SetColorMode(ColorMode.ENERGY);
    }
    
    private void _ResetPipeline()
    {
        // Reset variables
        worldSites = null;

        // Set seed to preset or random
        if (setSeed != -1) UnityEngine.Random.InitState(setSeed);
        else UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
    }

    private void _GenerateMesh()
    {
        // Generate mesh
        meshGenerator.FullGenerateMesh();
        meshRenderer.material = meshMaterial;
    }

    private void _GenerateSites()
    {
        // Generate world sites
        worldSites = new List<WorldSite>();
        foreach (MeshSite meshSite in meshGenerator.meshSites)
        {
            WorldSite worldSite = new WorldSite();
            worldSite.meshSite = meshSite;
            worldSites.Add(worldSite);
        }


        // Calculate edge distance using neighbours
        HashSet<int> closedSet = new HashSet<int>();
        Stack<int> openSet = new Stack<int>();

        // - Set edges to edge distance 0 and add to open
        foreach (WorldSite worldSite in worldSites)
        {
            if (worldSite.meshSite.isEdge)
            {
                worldSite.edgeDistance = 0;
                openSet.Push(worldSite.meshSite.siteIndex);
            }
        }

        // - Get next valid site from open
        while (openSet.Count > 0)
        {
            int currentSiteIndex = openSet.Pop();
            var currentSite = worldSites[currentSiteIndex];
            if (closedSet.Contains(currentSiteIndex)) continue;

            // - Check each valid neighbour
            foreach (var neighbourSiteIndex in currentSite.meshSite.neighbours)
            {
                var neighbourSite = worldSites[neighbourSiteIndex];
                if (!closedSet.Contains(neighbourSiteIndex))
                {

                    // - If this is better parent then update and add to open
                    int newDist = currentSite.edgeDistance + 1;
                    if (neighbourSite.edgeDistance == -1 || newDist < neighbourSite.edgeDistance)
                    {
                        neighbourSite.edgeDistance = newDist;
                        if (!openSet.Contains(neighbourSiteIndex)) openSet.Push(neighbourSiteIndex);
                    }
                }
            }
        }


        // Give sites energy
        Vector2 energyNoiseOffset = new Vector2(UnityEngine.Random.value * 2000, UnityEngine.Random.value * 2000);
        foreach (WorldSite worldSite in worldSites)
        {
            Vector2 centre = meshGenerator.mesh.vertices[worldSite.meshSite.meshCentroidI];
            float r = Mathf.PerlinNoise(
                centre.x * energyNoiseScale + energyNoiseOffset.x,
                centre.y * energyNoiseScale + energyNoiseOffset.y
            );;
            worldSite.maxEnergy = energyRandomRange[0] + (energyRandomRange[1] - energyRandomRange[0]) * r;
        }
    }


    private void SetColorMode(ColorMode mode)
    {
        // Change colours to a gradient
        Color[] meshColors = new Color[meshGenerator.mesh.vertexCount];
        foreach (var site in worldSites)
        {
            Color col = Color.magenta;

            if (mode == ColorMode.RANDOM)
            {
                float lower = site.meshSite.isEdge ? 0.75f : 0.35f;
                col = new Color(
                    lower + UnityEngine.Random.value * 0.25f,
                    lower + UnityEngine.Random.value * 0.25f,
                    lower + UnityEngine.Random.value * 0.25f
                );
            }

            else if (mode == ColorMode.DEPTH)
            {
                float pct = Mathf.Max(1.0f - site.edgeDistance * 0.2f, 0.0f);
                col = new Color(pct, pct, pct);
            }

            else if (mode == ColorMode.ENERGY)
            {
                float pct = site.maxEnergy / energyRandomRange[1];
                col = Color.Lerp(groundColorRange[0], groundColorRange[1], pct);
            }

            meshColors[site.meshSite.meshCentroidI] = col;
            foreach (int v in site.meshSite.meshVerticesI) meshColors[v] = col;
        }
        meshGenerator.mesh.colors = meshColors;
    }
}
