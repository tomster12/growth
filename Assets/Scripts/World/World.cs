
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static VoronoiMeshGenerator;


[ExecuteInEditMode]
public class World : Generator
{
    public static List<World> worlds = new List<World>();
    
    public static World GetClosestWorld(Vector2 pos, out Vector2 groundPosition)
    {
        // Loop over and find the closest world
        World closestWorld = null;
        float closestDst = float.PositiveInfinity;
        groundPosition = pos;
        foreach (World world in World.worlds)
        {
            Vector2 closestGroundPosition = world.GetClosestOverallPoint(pos);
            float dst = (closestGroundPosition - pos).magnitude;
            if (dst < closestDst)
            {
                closestWorld = world;
                groundPosition = closestGroundPosition;
                closestDst = dst;
            }
        }
        return closestWorld;
    }

    public enum ColorMode { NONE, STANDARD, RANDOM, DEPTH };

    public class WorldSite
    {
        public MeshSite meshSite;
        public int outsideDistance = -1;
        public float maxEnergy = 0, energy = 0;
        public GroundMaterial groundMaterial;

        public WorldSite(MeshSite meshSite)
        {
            this.meshSite = meshSite;
        }
    }


    // --- Editor ---
    [Header("====== References ======", order = 0)]
    [Header("Generators", order = 1)]
    [SerializeField] private PlanetPolygonGenerator planetPolygonGenerator;
    [SerializeField] private VoronoiMeshGenerator meshGenerator;
    [SerializeField] private WorldFoliager foliageManager;
    [Space(6, order = 0)]
    [Header("Containers", order = 1)]
    [SerializeField] private Transform _featureContainer;
    [SerializeField] private Transform _backgroundContainer;
    [SerializeField] private Transform _foregroundContainer;
    [SerializeField] private Transform _terrainContainer;
    [SerializeField] private Transform _foliageContainer;
    [Space(6, order = 0)]
    [Header("Components", order = 1)]
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private GravityAttractor gravityAttractor;
    [Space(6, order = 0)]
    [Header("Prefabs", order = 1)]
    [SerializeField] private GameObject atmospherePfb;
    [Space(20, order = 0)]

    [Header("====== Pipeline Config ======", order = 1)]
    [Header("Overall", order = 2)]
    [SerializeField] private int pipelineMaxTries = 5;
    [SerializeField] private bool doNotGenerateShape;
    [Header("Stage: Planet Polygon", order = 1)]
    [SerializeField] private PlanetPolygonGenerator.PlanetShapeInfo planetShapeInfo;
    [Header("Stage: Voronoi Mesh", order = 1)]
    [SerializeField] private Material meshMaterial;
    [SerializeField] private int seedCount = 300;
    [SerializeField] private float seedMinDistance = 0.02f;
    [Header("Stage: Site Processing", order = 1)]
    [SerializeField] private GroundMaterial materialDirt;
    [SerializeField] private GroundMaterial materialStone;
    [SerializeField] private NoiseData energyMaxNoise = new NoiseData(new float[2] { 40, 200 });
    [SerializeField] private NoiseData energyPctNoise = new NoiseData();
    [Header("Stage: Component Initialization", order = 1)]
    [SerializeField] private Material atmosphereMaterial;
    [SerializeField] private float atmosphereSizeMin = 150.0f;
    [SerializeField] private float atmosphereSizeMax = 200.0f;
    [SerializeField] private float gravityForce = 10.0f;
    
    // --- Main ---
    public Mesh mesh { get; private set; }
    public List<WorldSite> worldSites { get; private set; }
    public List<MeshSiteEdge> surfaceEdges { get; private set; }
    public ColorMode currentColorMode { get; private set; } = ColorMode.NONE;
    public Transform worldTransform => outsidePolygon.transform;

    public Transform featureContainer => _featureContainer;
    public Transform backgroundContainer => _backgroundContainer;
    public Transform foregroundContainer => _foregroundContainer;
    public Transform terrainContainer => _terrainContainer;
    public Transform foliageContainer => _foliageContainer;

    private SpriteRenderer atmosphere;


    private void Awake()
    {
        worlds.Add(this);
    }


    #region Generation Pipeline

    [ContextMenu("Stage/- Safe Generate")]
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

    [ContextMenu("Stage/- Generate")]
    public override void Generate()
    {
        // Clear then run stages
        ClearOutput();
        _GenerateMesh();
        _ProcessSites();
        _InitializeComponents();

        // Run other managers
        foliageManager.Generate();

        // Generated so set initial colour
        SetColourMode(ColorMode.STANDARD);
    }

    [ContextMenu("Stage/- Clear")]
    public override void ClearOutput()
    {
        // Reset main variables
        if (mesh != null) mesh.Clear();
        foliageManager.ClearOutput();
        mesh = null;
        worldSites = null;
        surfaceEdges = null;
        currentColorMode = ColorMode.NONE;
        if (atmosphere == null) atmosphere = worldTransform.Find("Atmosphere")?.GetComponent<SpriteRenderer>();
        if (atmosphere != null) DestroyImmediate(atmosphere.gameObject);
        _ClearContainers();
    }

    private void _ClearContainers()
    {
        for (int i = featureContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = featureContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = backgroundContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = backgroundContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = foregroundContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = foregroundContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = terrainContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = terrainContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = foliageContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = foliageContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
    }


    [ContextMenu("Stage/1. Generate Mesh")]
    private void _GenerateMesh()
    {
        // Generate polygon
        if (!doNotGenerateShape) planetPolygonGenerator.Generate(outsidePolygon, planetShapeInfo);

        // Generate mesh
        meshGenerator.Generate(meshFilter, outsidePolygon, seedCount, seedMinDistance);
        mesh = meshGenerator.mesh;

        // Instantiate material
        meshRenderer.material = Instantiate(meshMaterial);
        float noiseScale = GetSurfaceRange()[0] / 30.0f;
        meshRenderer.material.SetFloat("_NoiseScale", noiseScale);
        
        // Generate world sites
        worldSites = new List<WorldSite>();
        foreach (MeshSite meshSite in meshGenerator.meshSites) worldSites.Add(new WorldSite(meshSite));
    }

    [ContextMenu("Stage/2. Process Sites")]
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


        // Loop over sites
        foreach (WorldSite worldSite in worldSites)
        {
            // Generate site energy
            Vector2 centre = mesh.vertices[worldSite.meshSite.meshCentroidI];
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

    [ContextMenu("Stage/3. Initialize Components")]
    private void _InitializeComponents()
    {
        // Create atmosphere object
        GameObject atmosphereGO = Instantiate(atmospherePfb);
        atmosphere = atmosphereGO.GetComponent<SpriteRenderer>();
        atmosphere.gameObject.name = "Atmosphere";
        atmosphere.transform.parent = worldTransform;
        atmosphere.transform.localPosition = Vector3.zero;

        // Instantiate material
        atmosphere.material = Instantiate(atmosphere.material);

        // Update object
        UpdateComponents();
    }

    #endregion


    [ContextMenu("Update/Components")]
    private void UpdateComponents()
    {
        if (atmosphere == null) atmosphere = worldTransform.Find("Atmosphere")?.GetComponent<SpriteRenderer>();

        // Set global scale
        float targetScale = atmosphereSizeMax * 2.0f;
        atmosphere.transform.localScale = Vector3.one;
        atmosphere.transform.localScale = new Vector3(
            targetScale / worldTransform.lossyScale.x,
            targetScale / worldTransform.lossyScale.y,
            targetScale / worldTransform.lossyScale.z
        );

        // Set shader min and max
        atmosphere.material.SetFloat("_Min", atmosphereSizeMin);
        atmosphere.material.SetFloat("_Max", atmosphereSizeMax);

        // Update gravity
        gravityAttractor.gravityRadius = atmosphereSizeMax;
        gravityAttractor.gravityForce = gravityForce;
    }

    [ContextMenu("Update/Colours")]
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
                col = Color.Lerp(site.groundMaterial.materialColorRange[0], site.groundMaterial.materialColorRange[1], pct);
            }

            meshColors[site.meshSite.meshCentroidI] = col;
            foreach (int v in site.meshSite.meshVerticesI) meshColors[v] = col;
        }
        mesh.colors = meshColors;

        // Update foliage manager
        foliageManager.UpdateColours();
    }


    [ContextMenu("Color/Set STANDARD")]
    private void SetColourModeEnergy() { SetColourMode(ColorMode.STANDARD); }

    [ContextMenu("Color/Set RANDOM")]
    private void SetColourModeRandom() { SetColourMode(ColorMode.RANDOM); }

    [ContextMenu("Color/Set DEPTH")]
    private void SetColourModeDepth() { SetColourMode(ColorMode.DEPTH); }

    private void SetColourMode(ColorMode mode)
    {
        if (currentColorMode == mode) return;
        currentColorMode = mode;
        UpdateColours();
    }


    public Vector3 GetClosestOverallPoint(Vector2 pos) => rb.ClosestPoint(pos);

    public Vector3 GetClosestSurfacePoint(Vector2 pos) => outsidePolygon.ClosestPoint(pos);

    public float[] GetSurfaceRange()
    {
        float min = 0, max = 0;
        foreach (NoiseData noiseData in planetShapeInfo.noiseData)
        {
            min += noiseData.valueRange[0];
            max += noiseData.valueRange[1];
        }
        return new float[] { min, max };
    }
}
