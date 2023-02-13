
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static VoronoiMeshGenerator;

public class World : MonoBehaviour
{
    public enum ColorMode { NONE, STANDARD, RANDOM, DEPTH };

    
    public class WorldSite
    {
        public MeshSite meshSite;
        public int outsideDistance = -1;
        public float maxEnergy = 0;

        public WorldSite(MeshSite meshSite)
        {
            this.meshSite = meshSite;
        }
    }


    [Header("References")]
    [SerializeField] private PlanetPolygonGenerator planetPolygonGenerator;
    [SerializeField] private VoronoiMeshGenerator meshGenerator;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material meshMaterial;
    [SerializeField] Transform grassContainer;
    [SerializeField] GameObject grassPfb;

    [Header("Config")]
    [SerializeField] private int pipelineMaxTries = 5;
    [SerializeField] private int setSeed = -1;
    [SerializeField] private float energyNoiseScale = 0.2f;
    [SerializeField] private float[] energyRandomRange = new float[] { 50, 100 };
    [SerializeField] private Color[] groundColorRange = new Color[] { new Color(0,0,0), new Color(1,1,1) };
    [SerializeField] private Color[] grassColorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };
    [SerializeField] private PlanetPolygonGenerator.PlanetShapeInfo planetShapeInfo;

    [SerializeField] private int chosenSeed;
    private Mesh mesh;
    private List<WorldSite> worldSites;
    private List<MeshSiteEdge> outsideEdges;
    private List<SpriteRenderer> grassSprites;
    private ColorMode currentColorMode = ColorMode.NONE;


    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        // Keep trying until successful generation
        int tries = 0;
        while (tries < pipelineMaxTries)
        {
            try
            {
                // Run all procedures
                _ResetPipeline();
                _GenerateMesh();
                _ProcessSites();
                _GenerateGrass();

                // Set color mode
                SetColorMode(ColorMode.STANDARD);
                break;
            }
            catch (Exception e)
            {
                tries++;
                Debug.LogException(e);
                Debug.LogWarning("World Pipeline threw an error " + tries + "/" + pipelineMaxTries + ".");
            }
        }
        if (tries == pipelineMaxTries)
        {
            Debug.LogException(new Exception("World Pipeline hit maximum number of tries (" + tries + "/" + pipelineMaxTries + ")."));
        }
    }
    
    private void _ResetPipeline()
    {
        // Set seed to preset or random
        chosenSeed = (setSeed != -1) ? setSeed : (int)DateTime.Now.Ticks;
        UnityEngine.Random.InitState(chosenSeed);

        // Reset variables
        worldSites = null;
        outsideEdges = null;
        currentColorMode = ColorMode.NONE;
        _ClearGrass();
    }
    
    private void _GenerateMesh()
    {
        // Generate polygon
        planetPolygonGenerator.GeneratePlanet(planetShapeInfo);

        // Generate mesh
        meshGenerator.GenerateMesh();
        meshRenderer.material = meshMaterial;
        mesh = meshGenerator.mesh;

        // Generate world sites
        worldSites = new List<WorldSite>();
        foreach (MeshSite meshSite in meshGenerator.meshSites) worldSites.Add(new WorldSite(meshSite));
    }

    private void _ProcessSites()
    {
        // Calculate external edges
        HashSet<MeshSiteEdge> outsideEdgesUnordered = new HashSet<MeshSiteEdge>();
        foreach (WorldSite worldSite in worldSites)
        {
            if (worldSite.meshSite.isOutside)
            {
                foreach (MeshSiteEdge edge in worldSite.meshSite.edges)
                {
                    if (edge.isOutside) outsideEdgesUnordered.Add(edge);
                }
            }
        }

        // - Grab random first edge from unordered
        outsideEdges = new List<MeshSiteEdge>();
        MeshSiteEdge first = outsideEdgesUnordered.First();
        outsideEdges.Add(first);
        outsideEdgesUnordered.Remove(first);

        // - While sites left unordered
        while (outsideEdgesUnordered.Count > 0)
        {
            MeshSiteEdge current = outsideEdges.Last();
            MeshSite currentSite = worldSites[current.siteIndex].meshSite;
            MeshSiteEdge picked = null;
            
            // - Find first edge that matches in either direction
            foreach (MeshSiteEdge checkEdge in outsideEdgesUnordered)
            {
                MeshSite checkSite = worldSites[checkEdge.siteIndex].meshSite;
                MeshSiteVertex toVertex = currentSite.vertices[current.siteToVertexI];
                MeshSiteVertex fromVertex = checkSite.vertices[checkEdge.siteFromVertexI];
                if (toVertex.vertexUID == fromVertex.vertexUID) { picked = checkEdge; break; }
            }
            if (picked == null)
            {
                throw new Exception("Could not find next edge for site " + current.siteIndex + " vertex " + current.siteFromVertexI + " to " + current.siteToVertexI + ", " + outsideEdges.Count + " edges left");
            }

            // - Add to ordered
            outsideEdges.Add(picked);
            outsideEdgesUnordered.Remove(picked);
        }


        // Calculate edge distance using neighbours
        HashSet<int> closedSet = new HashSet<int>();
        Stack<int> openSet = new Stack<int>();

        // - Set edges to edge distance 0 and add to open
        foreach (WorldSite worldSite in worldSites)
        {
            if (worldSite.meshSite.isOutside)
            {
                worldSite.outsideDistance = 0;
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
            foreach (var neighbourSiteIndex in currentSite.meshSite.neighbouringSites)
            {
                var neighbourSite = worldSites[neighbourSiteIndex];
                if (!closedSet.Contains(neighbourSiteIndex))
                {

                    // - If this is better parent then update and add to open
                    int newDist = currentSite.outsideDistance + 1;
                    if (neighbourSite.outsideDistance == -1 || newDist < neighbourSite.outsideDistance)
                    {
                        neighbourSite.outsideDistance = newDist;
                        if (!openSet.Contains(neighbourSiteIndex)) openSet.Push(neighbourSiteIndex);
                    }
                }
            }
        }


        // Give sites energy
        Vector2 energyNoiseOffset = new Vector2(UnityEngine.Random.value * 2000, UnityEngine.Random.value * 2000);
        foreach (WorldSite worldSite in worldSites)
        {
            // - Random chance empty site
            if (UnityEngine.Random.value < 0.05f) worldSite.maxEnergy = 0;

            // - Generate using perlin noise
            else
            {
                Vector2 centre = meshGenerator.mesh.vertices[worldSite.meshSite.meshCentroidI];
                float r = Mathf.PerlinNoise(
                    centre.x * energyNoiseScale + energyNoiseOffset.x,
                    centre.y * energyNoiseScale + energyNoiseOffset.y );
                worldSite.maxEnergy = energyRandomRange[0] + (energyRandomRange[1] - energyRandomRange[0]) * r;
            }

        }
    }

    [ContextMenu("Clear Grass")]
    private void _ClearGrass()
    {
        // Delete grass container children
        for (int i = grassContainer.childCount - 1; i >= 0; i--) GameObject.DestroyImmediate(grassContainer.GetChild(i).gameObject);
        if (grassSprites == null) grassSprites = new List<SpriteRenderer>();
        grassSprites.Clear();
    }

    private void _GenerateGrass()
    {
        // For each edge (clockwise)
        foreach (MeshSiteEdge edge in outsideEdges)
        {
            MeshSite site = worldSites[edge.siteIndex].meshSite;
            Vector2 a = mesh.vertices[site.meshVerticesI[edge.siteToVertexI]];
            Vector2 b = mesh.vertices[site.meshVerticesI[edge.siteFromVertexI]];
            Vector2 dir = (b - a);
            bool toFlipX = UnityEngine.Random.value < 0.5f;
            
            // Generate grass and set position
            GameObject grass = Instantiate(grassPfb);
            grass.transform.parent = grassContainer;
            if (toFlipX) grass.transform.position = transform.TransformPoint(a + dir);
            else grass.transform.position = transform.TransformPoint(a);
            grass.transform.right = dir.normalized;

            // Grow sprite to correct size
            SpriteRenderer sprite = grass.GetComponent<SpriteRenderer>();
            sprite.size = new Vector2(dir.magnitude, sprite.size.y);
            sprite.flipX = toFlipX;
            grassSprites.Add(sprite);
        }
    }


    [ContextMenu("Color/Set STANDARD")]
    private void SetColorModeEnergy() { SetColorMode(ColorMode.STANDARD); }
    [ContextMenu("Color/Set RANDOM")]
    private void SetColorModeRandom() { SetColorMode(ColorMode.RANDOM); }
    [ContextMenu("Color/Set DEPTH")]
    private void SetColorModeDepth() { SetColorMode(ColorMode.DEPTH); }


    private void SetColorMode(ColorMode mode)
    {
        if (currentColorMode == mode) return;
        currentColorMode = mode;
        UpdateColours();
    }

    [ContextMenu("Color/Update")]
    private void UpdateColours()
    {
        // Update colours of the mesh
        Color[] meshColors = new Color[meshGenerator.mesh.vertexCount];
        foreach (var site in worldSites)
        {
            Color col = Color.magenta;
            if (currentColorMode == ColorMode.RANDOM)
            {
                col = new Color(
                    0.75f + UnityEngine.Random.value * 0.25f,
                    0.75f + UnityEngine.Random.value * 0.25f,
                    0.75f + UnityEngine.Random.value * 0.25f
                );
            }
            else if (currentColorMode == ColorMode.DEPTH)
            {
                float pct = Mathf.Max(1.0f - site.outsideDistance * 0.2f, 0.0f);
                col = new Color(pct, pct, pct);
            }
            else if (currentColorMode == ColorMode.STANDARD)
            {
                float pct = site.maxEnergy / energyRandomRange[1];
                col = Color.Lerp(groundColorRange[0], groundColorRange[1], pct);
            }
            meshColors[site.meshSite.meshCentroidI] = col;
            foreach (int v in site.meshSite.meshVerticesI) meshColors[v] = col;
        }
        meshGenerator.mesh.colors = meshColors;
    
        // Update colors of the grass
        for (int i = 0; i < grassSprites.Count; i++)
        {
            SpriteRenderer sprite = grassSprites[i];
            MeshSiteEdge edge = outsideEdges[i];
            WorldSite site = worldSites[edge.siteIndex];

            Color col = Color.magenta;
            if (currentColorMode == ColorMode.RANDOM)
            {
                col = new Color(
                    0.75f + UnityEngine.Random.value * 0.25f,
                    0.75f + UnityEngine.Random.value * 0.25f,
                    0.75f + UnityEngine.Random.value * 0.25f
                );
            }
            else if (currentColorMode == ColorMode.DEPTH)
            {
                float pct = Mathf.Max(1.0f - site.outsideDistance * 0.2f, 0.0f);
                col = new Color(pct, pct, pct);
            }
            else if (currentColorMode == ColorMode.STANDARD)
            {
                float pct = site.maxEnergy / energyRandomRange[1];
                col = Color.Lerp(grassColorRange[0], grassColorRange[1], pct);
            }
            
            sprite.color = col;
        }
    }
}
