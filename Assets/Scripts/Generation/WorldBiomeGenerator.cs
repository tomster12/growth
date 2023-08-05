
using System.Collections.Generic;
using UnityEngine;
using static VoronoiMeshGenerator;
using static World;


public class WorldBiomeGenerator : MonoBehaviour, IGenerator
{
    // --- Editor ---
    [Header("References")]
    [SerializeField] private World world;
    [SerializeField] private GroundMaterial materialDirt;
    [SerializeField] private GroundMaterial materialStone;
    [SerializeField] private GameObject pebblePfb;
    [SerializeField] private GameObject boulderTerrainPfb;
    [SerializeField] private GameObject boulderFeaturePfb;

    [Header("Config")]
    [SerializeField] private float stoneChance = 0.03f;
    [SerializeField] private float[] stoneChances = new float[] { 0.8f, 0.1f, 0.1f };
    [SerializeField] private NoiseData energyMaxNoise = new NoiseData(new float[2] { 40, 200 });
    [SerializeField] private NoiseData energyPctNoise = new NoiseData();

    public List<SpriteRenderer> surfaceFoliage { get; private set; } = new List<SpriteRenderer>();
    public List<IGeneratorController> surfaceStones { get; private set; } = new List<IGeneratorController>();
    public bool isGenerated { get; private set; } = false;
    

    public void Clear()
    {
        // Assume they are already deleted
        surfaceFoliage.Clear();
        surfaceStones.Clear();
        isGenerated = false;
    }

    public void Generate()
    {
        Clear();
        Step_GenerateMaterial();
        Step_GenerateFoliage();
        Step_GenerateStones();
        Step_SetColors();
        isGenerated = true;
    }

    private void Step_GenerateMaterial()
    {
        // Loop over sites
        foreach (WorldSite worldSite in world.worldSites)
        {
            // Generate site energy
            Vector2 centre = world.mesh.vertices[worldSite.meshSite.meshCentroidI];
            worldSite.maxEnergy = energyMaxNoise.GetNoise(centre);
            float pct = energyPctNoise.GetNoise(centre);
            if (UnityEngine.Random.value < 0.02f) pct = 0.0f;
            worldSite.energy = pct * worldSite.maxEnergy;

            // Generate site terrain
            if (worldSite.outsideDistance >= 3) worldSite.groundMaterial = materialStone;
            else if (worldSite.outsideDistance >= 2 && UnityEngine.Random.value < 0.8f) worldSite.groundMaterial = materialStone;
            else worldSite.groundMaterial = materialDirt;
        }
    }

    private void Step_GenerateFoliage()
    {
        // For each edge (clockwise)
        surfaceFoliage = new List<SpriteRenderer>();
        for (int i = 0; i < world.surfaceEdges.Count; i++)
        {
            MeshSiteEdge edge = world.surfaceEdges[i];
            WorldSite site = world.worldSites[edge.siteIndex];

            // Back out early if no foliage
            if (!site.groundMaterial.hasFoliage)
            {
                surfaceFoliage.Add(null);
                continue;
            }

            // Continue and find where to put it
            Vector2 a = world.mesh.vertices[site.meshSite.meshVerticesI[edge.siteToVertexI]];
            Vector2 b = world.mesh.vertices[site.meshSite.meshVerticesI[edge.siteFromVertexI]];
            Vector2 dir = (b - a);
            bool toFlipX = UnityEngine.Random.value < 0.5f;

            // Generate foliage and set position
            GameObject grass = Instantiate(site.groundMaterial.foliagePfb);
            grass.transform.parent = world.foliageContainer;
            Vector3 pos;
            if (toFlipX) pos = world.worldTransform.TransformPoint(a + dir);
            else pos = world.worldTransform.TransformPoint(a);
            grass.transform.right = dir.normalized;
            pos.z = world.foliageContainer.transform.position.z;
            grass.transform.position = pos;

            // Grow sprite to correct size
            float width = dir.magnitude + 0.04f;
            float height = site.groundMaterial.foliageHeightNoise.GetCyclicNoise((float)i / world.surfaceEdges.Count);
            SpriteRenderer renderer = grass.GetComponent<SpriteRenderer>();
            renderer.size = new Vector2(width, renderer.size.y);
            renderer.flipX = toFlipX;
            renderer.transform.localScale = new Vector3(renderer.transform.localScale.x, height, renderer.transform.localScale.z);
            surfaceFoliage.Add(renderer);
        }
    }

    private void Step_GenerateStones()
    {
        for (int i = 0; i < world.surfaceEdges.Count; i++)
        {
            float r = UnityEngine.Random.value;
            if (r > stoneChance) continue;

            // Get edges and site
            MeshSiteEdge edge = world.surfaceEdges[i];
            WorldSite site = world.worldSites[edge.siteIndex];
            Vector2 a = world.mesh.vertices[site.meshSite.meshVerticesI[edge.siteToVertexI]];
            Vector2 b = world.mesh.vertices[site.meshSite.meshVerticesI[edge.siteFromVertexI]];

            // Generate a pebble on this edge
            // Randomly pick a stone type
            float sr = UnityEngine.Random.value;
            GameObject stoneObj;
            if (sr < stoneChances[0])
            {
                stoneObj = Instantiate(pebblePfb);
                stoneObj.transform.parent = world.foregroundContainer;
                PluckableStone pluckable = stoneObj.GetComponent<PluckableStone>();
                pluckable.popDir = Vector2.Perpendicular(b - a);
            }
            else if (sr < (stoneChances[0] + stoneChances[1]))
            {
                stoneObj = Instantiate(boulderTerrainPfb);
                stoneObj.transform.parent = world.terrainContainer;
            }
            else
            {
                stoneObj = Instantiate(boulderFeaturePfb);
                stoneObj.transform.parent = world.featureContainer;
            }

            // Generate the mesh and rotate
            IGeneratorController stone = stoneObj.GetComponent<IGeneratorController>();
            surfaceStones.Add(stone);
            stone.Generate();
            stone.transform.eulerAngles = new Vector3(0.0f, 0.0f, UnityEngine.Random.value * 360.0f);

            // Position randomly
            float t = 0.25f + UnityEngine.Random.value * 0.5f;
            Vector3 pos = world.worldTransform.TransformPoint(Vector2.Lerp(a, b, t));
            pos.z = stoneObj.transform.parent.position.z;
            stone.transform.position = pos;
        }
    }

    public void Step_SetColors()
    {
        // Update colors of the grass
        for (int i = 0; i < surfaceFoliage.Count; i++)
        {
            SpriteRenderer sprite = surfaceFoliage[i];
            if (sprite == null) continue;
            MeshSiteEdge edge = world.surfaceEdges[i];
            WorldSite site = world.worldSites[edge.siteIndex];
            float pct = site.energy / site.maxEnergy;
            sprite.color = Color.Lerp(site.groundMaterial.foliageColorRange[0], site.groundMaterial.foliageColorRange[1], pct);
        }

        // Update colours of the mesh
        Color[] meshColors = new Color[world.mesh.vertexCount];
        foreach (var site in world.worldSites)
        {
            float pct = site.energy / site.maxEnergy;
            Color col = Color.Lerp(site.groundMaterial.materialColorRange[0], site.groundMaterial.materialColorRange[1], pct);
            meshColors[site.meshSite.meshCentroidI] = col;
            foreach (int v in site.meshSite.meshVerticesI) meshColors[v] = col;
        }

        world.mesh.colors = meshColors;
    }


    public bool GetIsGenerated() => isGenerated;
    
    public string GetName() => gameObject.name;    
}
