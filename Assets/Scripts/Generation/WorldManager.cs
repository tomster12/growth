
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static VoronoiMeshGenerator;
using Unity.Collections;

[ExecuteInEditMode]
public class WorldManager : MonoBehaviour
{
    public enum ColorMode { NONE, STANDARD, RANDOM, DEPTH };

    public class WorldSite
    {
        public MeshSite meshSite;
        public int outsideDistance = -1;
        public float maxEnergy = 0, energy = 0;

        public WorldSite(MeshSite meshSite)
        {
            this.meshSite = meshSite;
        }
    }


    // --- Static ---
    public static int chosenSeed;

    // --- Editor ---
    [Header("====== References ======", order = 0)]
    [Header("Generators", order = 1)]
    [SerializeField] private PlanetPolygonGenerator planetPolygonGenerator;
    [SerializeField] private VoronoiMeshGenerator meshGenerator;
    [SerializeField] private WorldFoliageManager foliageManager;
    [Space(6, order = 0)]
    [Header("Components", order = 1)]
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material meshMaterial;
    [Space(20, order = 0)]

    [Header("====== Pipeline Config ======", order = 1)]
    [Header("Overall", order = 2)]
    [SerializeField] private int pipelineMaxTries = 5;
    [SerializeField] private int setSeed = -1;
    [SerializeField] public bool doNotGenerateShape;
    [SerializeField] public bool toUpdate = false;
    [Header("Stage: Planet Polygon", order = 1)]
    [SerializeField] private PlanetPolygonGenerator.PlanetShapeInfo planetShapeInfo;
    [Header("Stage: Voronoi Mesh", order = 1)]
    [SerializeField] private int seedCount = 300;
    [SerializeField] private float seedMinDistance = 0.02f;
    [Header("Stage: Site Processing", order = 1)]
    [SerializeField] private NoiseData energyMaxNoise = new NoiseData(new float[2] { 40, 200 });
    [SerializeField] private NoiseData energyPctNoise = new NoiseData();
    [Space(20, order = 0)]

    [Header("====== General Config ======", order = 1)]
    [Space(10, order = 2)]
    [SerializeField] private Color[] groundColorRange = new Color[] { new Color(0,0,0), new Color(1,1,1) };

    // --- Main ---
    public Mesh mesh { get; private set; }
    public List<WorldSite> worldSites { get; private set; }
    public List<MeshSiteEdge> surfaceEdges { get; private set; }
    public ColorMode currentColorMode { get; private set; } = ColorMode.NONE;
    

    private void Update()
    {
        if (toUpdate)
        {
            SafeGenerate();
            toUpdate = false;
        }
    }


    #region Generation Pipeline

    [ContextMenu("Generate World")]
    public void SafeGenerate()
    {
        // Keep trying Generate and catch errors
        int tries = 0;
        while (tries < pipelineMaxTries)
        {
            try
            {
                Generate();
                break;
            }
            catch (Exception e)
            {
                tries++;
                Debug.LogException(e);
                Debug.LogWarning("World Pipeline threw an error " + tries + "/" + pipelineMaxTries + ".");
            }
        }

        // Hit max number of tries
        if (tries == pipelineMaxTries)
        {
            Debug.LogException(new Exception("World Pipeline hit maximum number of tries (" + tries + "/" + pipelineMaxTries + ")."));
        }
    }
   
    public void Generate()
    {
        // Set seed to preset or random
        chosenSeed = (setSeed != -1) ? setSeed : (int)DateTime.Now.Ticks;
        UnityEngine.Random.InitState(chosenSeed);

        // Clear then run stages
        ClearMain();
        _GenerateMesh();
        _ProcessSites();

        // Run other managers
        foliageManager.Generate();

        // Generated so set initial colour
        SetColorMode(ColorMode.STANDARD);
    }

    public void ClearMain()
    {
        // Reset main variables
        if (mesh != null) mesh.Clear();
        foliageManager.ClearMain();
        mesh = null;
        worldSites = null;
        surfaceEdges = null;
        currentColorMode = ColorMode.NONE;
    }


    private void _GenerateMesh()
    {
        // Generate polygon
        if (!doNotGenerateShape) planetPolygonGenerator.Generate(outsidePolygon, planetShapeInfo);

        // Generate mesh
        meshGenerator.Generate(meshFilter, outsidePolygon, seedCount, seedMinDistance);
        meshRenderer.material = meshMaterial;
        mesh = meshGenerator.mesh;

        // Generate world sites
        worldSites = new List<WorldSite>();
        foreach (MeshSite meshSite in meshGenerator.meshSites) worldSites.Add(new WorldSite(meshSite));
    }

    private void _ProcessSites()
    {
        // Calculate external edges
        HashSet<MeshSiteEdge> surfaceEdgesUnordered = new HashSet<MeshSiteEdge>();
        foreach (WorldSite worldSite in worldSites)
        {
            if (worldSite.meshSite.isOutside)
            {
                foreach (MeshSiteEdge edge in worldSite.meshSite.edges)
                {
                    if (edge.isOutside) surfaceEdgesUnordered.Add(edge);
                }
            }
        }

        // - Grab random first edge from unordered
        surfaceEdges = new List<MeshSiteEdge>();
        MeshSiteEdge first = surfaceEdgesUnordered.First();
        surfaceEdges.Add(first);
        surfaceEdgesUnordered.Remove(first);

        // - While sites left unordered
        while (surfaceEdgesUnordered.Count > 0)
        {
            MeshSiteEdge current = surfaceEdges.Last();
            MeshSite currentSite = worldSites[current.siteIndex].meshSite;
            MeshSiteEdge picked = null;
            
            // - Find first edge that matches in either direction
            foreach (MeshSiteEdge checkEdge in surfaceEdgesUnordered)
            {
                MeshSite checkSite = worldSites[checkEdge.siteIndex].meshSite;
                MeshSiteVertex toVertex = currentSite.vertices[current.siteToVertexI];
                MeshSiteVertex fromVertex = checkSite.vertices[checkEdge.siteFromVertexI];
                if (toVertex.vertexUID == fromVertex.vertexUID) { picked = checkEdge; break; }
            }
            if (picked == null)
            {
                throw new Exception("Could not find next edge for site " + current.siteIndex + " vertex " + current.siteFromVertexI + " to " + current.siteToVertexI + ", " + surfaceEdges.Count + " edges left");
            }

            // - Add to ordered
            surfaceEdges.Add(picked);
            surfaceEdgesUnordered.Remove(picked);
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
        foreach (WorldSite worldSite in worldSites)
        {
            // - Generate using perlin noise
            Vector2 centre = mesh.vertices[worldSite.meshSite.meshCentroidI];
            worldSite.maxEnergy = energyMaxNoise.GetNoise(centre);
            float pct = energyPctNoise.GetNoise(centre);

            // - Random chance empty site
            if (UnityEngine.Random.value < 0.02f) pct = 0.0f;

            // - set final energy
            worldSite.energy = pct * worldSite.maxEnergy;

        }
    }

    #endregion


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
        Color[] meshColors = new Color[mesh.vertexCount];
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
                float pct = site.energy / site.maxEnergy;
                col = Color.Lerp(groundColorRange[0], groundColorRange[1], pct);
            }
            meshColors[site.meshSite.meshCentroidI] = col;
            foreach (int v in site.meshSite.meshVerticesI) meshColors[v] = col;
        }
        mesh.colors = meshColors;

        // Update foliage manager
        foliageManager.UpdateColours();
    }
}
