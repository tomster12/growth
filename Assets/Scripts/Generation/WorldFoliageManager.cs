
using System.Collections.Generic;
using UnityEngine;
using static VoronoiMeshGenerator;
using static WorldManager;


public class WorldFoliageManager : Generator
{
    // -- Parameters --
    [Header("Parameters")]
    [SerializeField] private WorldManager worldManager;
    [SerializeField] private Transform featureContainer;
    [SerializeField] private Transform foregroundContainer;
    [SerializeField] private Transform terrainContainer;
    [SerializeField] private Transform foliageContainer;
    [SerializeField] private GameObject pebblePfb;
    [SerializeField] private GameObject boulderTerrainPfb;
    [SerializeField] private GameObject boulderFeaturePfb;
    [SerializeField] private float stoneChance = 0.03f;
    [SerializeField] private float[] stoneChances = new float[] { 0.8f, 0.1f, 0.1f };

    // -- Output --
    public List<SpriteRenderer> surfaceFoliage { get; private set; }
    public List<GeneratorController> surfaceStones { get; private set; }
    

    [ContextMenu("Generate Foliage")]
    public override void Generate()
    {
        ClearOutput();
        _GenerateFoliage();
        _GeneratePebblesAndBoulders();
    }

    [ContextMenu("Clear Output")]
    public override void ClearOutput()
    {
        // Delete foliage container children
        for (int i = foliageContainer.childCount - 1; i >= 0; i--) GameObject.DestroyImmediate(foliageContainer.GetChild(i).gameObject);
        if (surfaceFoliage == null) surfaceFoliage = new List<SpriteRenderer>();
        surfaceFoliage.Clear();

        // Delete pebbles
        if (surfaceStones == null) surfaceStones = new List<GeneratorController>();
        foreach (GeneratorController p in surfaceStones)
        {
            if (p != null) GameObject.DestroyImmediate(p.gameObject);
        }
        surfaceStones.Clear();
    }


    private void _GenerateFoliage()
    {
        // For each edge (clockwise)
        surfaceFoliage = new List<SpriteRenderer>();
        for (int i = 0; i < worldManager.surfaceEdges.Count; i++)
        {
            MeshSiteEdge edge = worldManager.surfaceEdges[i];
            WorldSite site = worldManager.worldSites[edge.siteIndex];

            // Back out early if no foliage
            if (!site.groundMaterial.hasFoliage)
            {
                surfaceFoliage.Add(null);
                continue;
            }

            // Continue and find where to put it
            Vector2 a = worldManager.mesh.vertices[site.meshSite.meshVerticesI[edge.siteToVertexI]];
            Vector2 b = worldManager.mesh.vertices[site.meshSite.meshVerticesI[edge.siteFromVertexI]];
            Vector2 dir = (b - a);
            bool toFlipX = UnityEngine.Random.value < 0.5f;

            // Generate foliage and set position
            GameObject grass = Instantiate(site.groundMaterial.foliagePfb);
            grass.transform.parent = foliageContainer;
            if (toFlipX) grass.transform.position = worldManager.worldTransform.TransformPoint(a + dir);
            else grass.transform.position = worldManager.worldTransform.TransformPoint(a);
            grass.transform.right = dir.normalized;

            // Grow sprite to correct size
            float width = dir.magnitude + 0.04f;
            float height = site.groundMaterial.foliageHeightNoise.GetCyclicNoise((float)i / worldManager.surfaceEdges.Count);
            SpriteRenderer renderer = grass.GetComponent<SpriteRenderer>();
            renderer.size = new Vector2(width, renderer.size.y);
            renderer.flipX = toFlipX;
            renderer.transform.localScale = new Vector3(renderer.transform.localScale.x, height, renderer.transform.localScale.z);
            surfaceFoliage.Add(renderer);
        }
    }

    private void _GeneratePebblesAndBoulders()
    {
        for (int i = 0; i < worldManager.surfaceEdges.Count; i++)
        {
            float r = UnityEngine.Random.value;
            if (r > stoneChance) continue;

            // Get edges and site
            MeshSiteEdge edge = worldManager.surfaceEdges[i];
            WorldSite site = worldManager.worldSites[edge.siteIndex];

            // Generate a pebble on this edge
            // Randomly pick a stone type
            float sr = UnityEngine.Random.value;
            GameObject stoneObj;
            if (sr < stoneChances[0])
            {
                stoneObj = Instantiate(pebblePfb);
                stoneObj.transform.parent = foregroundContainer;
            }
            else if (sr < (stoneChances[0] + stoneChances[1]))
            {
                stoneObj = Instantiate(boulderTerrainPfb);
                stoneObj.transform.parent = terrainContainer;
            }
            else
            {
                stoneObj = Instantiate(boulderFeaturePfb);
                stoneObj.transform.parent = featureContainer;
            }

            // Generate the mesh and rotate
            GeneratorController stone = stoneObj.GetComponent<GeneratorController>();
            surfaceStones.Add(stone);
            stone.Generate();
            stone.transform.eulerAngles = new Vector3(0.0f, 0.0f, UnityEngine.Random.value * 360.0f);

            // Position randomly
            Vector2 a = worldManager.mesh.vertices[site.meshSite.meshVerticesI[edge.siteToVertexI]];
            Vector2 b = worldManager.mesh.vertices[site.meshSite.meshVerticesI[edge.siteFromVertexI]];
            float t = 0.25f + UnityEngine.Random.value * 0.5f;
            Vector3 pos = worldManager.worldTransform.TransformPoint(Vector2.Lerp(a, b, t));
            pos.z = 3.0f;
            stone.transform.position = pos;
        }
    }


    public void UpdateColours()
    {
        // Update colors of the grass
        for (int i = 0; i < surfaceFoliage.Count; i++)
        {
            SpriteRenderer sprite = surfaceFoliage[i];
            if (sprite == null) continue;
            MeshSiteEdge edge = worldManager.surfaceEdges[i];
            WorldSite site = worldManager.worldSites[edge.siteIndex];

            Color col = Color.magenta;
            if (worldManager.currentColorMode == ColorMode.RANDOM)
            {
                col = new Color(
                    0.75f + UnityEngine.Random.value * 0.25f,
                    0.75f + UnityEngine.Random.value * 0.25f,
                    0.75f + UnityEngine.Random.value * 0.25f
                );
            }
            else if (worldManager.currentColorMode == ColorMode.DEPTH)
            {
                float pct = Mathf.Max(1.0f - site.outsideDistance * 0.2f, 0.0f);
                col = new Color(pct, pct, pct);
            }
            else if (worldManager.currentColorMode == ColorMode.STANDARD)
            {
                float pct = site.energy / site.maxEnergy;
                col = Color.Lerp(site.groundMaterial.foliageColorRange[0], site.groundMaterial.foliageColorRange[1], pct);
            }

            sprite.color = col;
        }
    }
}
