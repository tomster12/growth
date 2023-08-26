
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;
using static VoronoiMeshGenerator;


[ExecuteInEditMode]
public class WorldGenerator : MonoBehaviour, IGenerator
{
    public class WorldSite
    {
        public World world;
        public MeshSite meshSite;
        public int outsideDistance = -1;
        public float maxEnergy = 0, energy = 0;
        public Biome biome;

        public WorldSite(World world, MeshSite meshSite)
        {
            this.world = world;
            this.meshSite = meshSite;
        }
    }

    public class WorldSurfaceEdge
    {
        public WorldSite worldSite;
        public MeshSiteEdge meshSiteEdge;
        public Vector3 a, b;
        public float length;

        public WorldSurfaceEdge(WorldSite worldSite, MeshSiteEdge meshSiteEdge)
        {
            Assert.AreEqual(worldSite.meshSite.siteIndex, meshSiteEdge.siteIndex);
            this.worldSite = worldSite;
            this.meshSiteEdge = meshSiteEdge;
        }
    };


    [Header("====== References ======", order = 0)]
    [Header("Generators", order = 1)]
    [SerializeField] private PlanetPolygonGenerator planetPolygonGenerator;
    [SerializeField] private VoronoiMeshGenerator meshGenerator;
    [SerializeField] private WorldBiomeGenerator biomeGenerator;
    [Space(6, order = 0)]
    [Header("Containers", order = 1)]
    [SerializeField] private Transform _backDecorContainer;
    [SerializeField] private Transform _backgroundContainer;
    [SerializeField] private Transform _foregroundContainer;
    [SerializeField] private Transform _terrainContainer;
    [SerializeField] private Transform _frontDecorContainer;
    [Space(6, order = 0)]
    [Header("Components", order = 1)]
    [SerializeField] private World world;
    [SerializeField] private PolygonCollider2D outsidePolygon;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private GravityAttractor gravityAttractor;
    [Space(6, order = 0)]
    [Header("Prefabs", order = 1)]
    [SerializeField] private GameObject atmospherePfb;
    [SerializeField] private Material meshMaterial;
    [Space(20, order = 0)]

    [Header("====== Pipeline Config ======", order = 1)]
    [SerializeField] private bool doNotGenerateShape = false;
    [SerializeField] private int pipelineMaxTries = 5;
    [SerializeField] private float atmosphereSizeMin = 150.0f;
    [SerializeField] private float atmosphereSizeMax = 200.0f;
    [SerializeField] private float gravityForce = 10.0f;
    
    public Mesh mesh { get; private set; }
    public List<WorldSite> sites { get; private set; }
    public List<WorldSurfaceEdge> surfaceEdges { get; private set; }
    public Transform worldTransform => outsidePolygon.transform;
    public bool isGenerated { get; private set; } = false;
    public Transform backDecorContainer => _backDecorContainer;
    public Transform backgroundContainer => _backgroundContainer;
    public Transform foregroundContainer => _foregroundContainer;
    public Transform terrainContainer => _terrainContainer;
    public Transform frontDecorContainer => _frontDecorContainer;
    public bool IsGenerated() => isGenerated;
    public bool IsComposite() => true;

    private SpriteRenderer atmosphere;


    public void Clear()
    {
        ClearContainers();
        ClearOutput();
        isGenerated = false;
    }

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
        Clear();

        Step_SetContainers();
        Step_GenerateMesh();
        Step_CalculateOutsideEdges();
        Step_CalculateSiteEdgeDistance();
        Step_InitializeComponents();
        Step_GenerateBiome();

        isGenerated = true;
    }

    public string GetName() => "World Composite";

    public IGenerator[] GetCompositeIGenerators() => new IGenerator[] { planetPolygonGenerator, meshGenerator, biomeGenerator };


    private void ClearContainers()
    {
        for (int i = backDecorContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = backDecorContainer.GetChild(i).gameObject;
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
        for (int i = frontDecorContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = frontDecorContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
    }

    private void ClearOutput()
    {
        // Reset other generators
        planetPolygonGenerator.Clear();
        meshGenerator.Clear();
        biomeGenerator.Clear();

        // Reset main variables
        mesh = null;
        sites = null;
        surfaceEdges = null;
        if (atmosphere == null) atmosphere = worldTransform.Find("Atmosphere")?.GetComponent<SpriteRenderer>();
        if (atmosphere != null) DestroyImmediate(atmosphere.gameObject);

        // Cleanup afterwards
        ClearContainers();
    }

    private void Step_SetContainers()
    {
        Assert.AreEqual(backDecorContainer.transform.childCount, 0);
        Assert.AreEqual(backgroundContainer.transform.childCount, 0);
        Assert.AreEqual(foregroundContainer.transform.childCount, 0);
        Assert.AreEqual(terrainContainer.transform.childCount, 0);
        Assert.AreEqual(frontDecorContainer.transform.childCount, 0);
        Layers.SetLayer(backDecorContainer, Layer.BACK_DECOR);
        Layers.SetLayer(backgroundContainer, Layer.BACKGROUND);
        Layers.SetLayer(foregroundContainer, Layer.FOREGROUND);
        Layers.SetLayer(terrainContainer, Layer.TERRAIN);
        Layers.SetLayer(frontDecorContainer, Layer.FRONT_DECOR);
    }

    private void Step_GenerateMesh()
    {
        // Generate polygon
        if (!doNotGenerateShape) planetPolygonGenerator.Generate();

        // Generate mesh
        meshGenerator.Generate();
        mesh = meshGenerator.mesh;

        // Instantiate ground material
        float noiseScale = planetPolygonGenerator.GetSurfaceRange()[0] / 30.0f;
        var groundMaterial = new Material(meshMaterial);
        meshRenderer.sharedMaterial = groundMaterial;
        meshRenderer.sharedMaterial.SetFloat("_NoiseScale", noiseScale);
        
        // Generate world sites
        sites = new List<WorldSite>();
        foreach (MeshSite meshSite in meshGenerator.meshSites) sites.Add(new WorldSite(world, meshSite));
    }

    private void Step_CalculateOutsideEdges()
    {
        // Calculate external edges
        HashSet<WorldSurfaceEdge> surfaceEdgesUnordered = new HashSet<WorldSurfaceEdge>();
        foreach (WorldSite worldSite in sites)
        {
            if (worldSite.meshSite.isOutside)
            {
                foreach (MeshSiteEdge edge in worldSite.meshSite.edges)
                {
                    if (edge.isOutside)
                    {
                        WorldSurfaceEdge WorldSurfaceEdge = new WorldSurfaceEdge(worldSite, edge);
                        WorldSurfaceEdge.a = transform.TransformPoint(mesh.vertices[worldSite.meshSite.meshVerticesI[edge.siteToVertexI]]);
                        WorldSurfaceEdge.b = transform.TransformPoint(mesh.vertices[worldSite.meshSite.meshVerticesI[edge.siteFromVertexI]]);
                        WorldSurfaceEdge.length = Vector3.Distance(WorldSurfaceEdge.a, WorldSurfaceEdge.b);
                        surfaceEdgesUnordered.Add(WorldSurfaceEdge);
                    }
                }
            }
        }

        // - Grab random first edge from unordered
        surfaceEdges = new List<WorldSurfaceEdge>();
        WorldSurfaceEdge first = surfaceEdgesUnordered.First();
        surfaceEdges.Add(first);
        surfaceEdgesUnordered.Remove(first);

        // - While sites left unordered
        while (surfaceEdgesUnordered.Count > 0)
        {
            WorldSurfaceEdge current = surfaceEdges.Last();
            MeshSite currentSite = sites[current.meshSiteEdge.siteIndex].meshSite;
            WorldSurfaceEdge picked = null;
            
            // - Find first edge that matches in either direction
            foreach (WorldSurfaceEdge checkEdge in surfaceEdgesUnordered)
            {
                MeshSite checkSite = sites[checkEdge.meshSiteEdge.siteIndex].meshSite;
                MeshSiteVertex toVertex = currentSite.vertices[current.meshSiteEdge.siteToVertexI];
                MeshSiteVertex fromVertex = checkSite.vertices[checkEdge.meshSiteEdge.siteFromVertexI];
                if (toVertex.vertexUID == fromVertex.vertexUID) { picked = checkEdge; break; }
            }
            if (picked == null)
            {
                throw new Exception("Could not find next edge for site " + current.meshSiteEdge.siteIndex + " vertex " + current.meshSiteEdge.siteFromVertexI + " to " + current.meshSiteEdge.siteToVertexI + ", " + surfaceEdges.Count + " edges left");
            }

            // - Add to ordered
            surfaceEdges.Add(picked);
            surfaceEdgesUnordered.Remove(picked);
        }
    }

    private void Step_CalculateSiteEdgeDistance()
    {
        // Calculate site edge distance using neighbours
        HashSet<int> closedSet = new HashSet<int>();
        Stack<int> openSet = new Stack<int>();

        // - Set edge site to distance 0 and add to open
        foreach (WorldSite worldSite in sites)
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
            var currentSite = sites[currentSiteIndex];
            if (closedSet.Contains(currentSiteIndex)) continue;

            // - Check each valid neighbour
            foreach (var neighbourSiteIndex in currentSite.meshSite.neighbouringSites)
            {
                var neighbourSite = sites[neighbourSiteIndex];
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
    }

    private void Step_InitializeComponents()
    {
        // Create atmosphere object
        GameObject atmosphereGO = Instantiate(atmospherePfb);
        atmosphere = atmosphereGO.GetComponent<SpriteRenderer>();
        atmosphere.gameObject.name = "Atmosphere";
        atmosphere.transform.parent = worldTransform;
        atmosphere.transform.localPosition = Vector3.zero;

        // Set global scale
        float targetScale = atmosphereSizeMax * 2.0f;
        atmosphere.transform.localScale = Vector3.one;
        atmosphere.transform.localScale = new Vector3(
            targetScale / worldTransform.lossyScale.x,
            targetScale / worldTransform.lossyScale.y,
            targetScale / worldTransform.lossyScale.z
        );

        // Set shader material and shader min and max
        Material atmosphereMaterial = new Material(atmosphere.sharedMaterial);
        atmosphere.sharedMaterial = atmosphereMaterial;
        atmosphere.sharedMaterial.SetFloat("_Min", atmosphereSizeMin);
        atmosphere.sharedMaterial.SetFloat("_Max", atmosphereSizeMax);

        // Update gravity
        gravityAttractor.gravityRadius = atmosphereSizeMax;
        gravityAttractor.gravityForce = gravityForce;
    }
    
    private void Step_GenerateBiome()
    {
        biomeGenerator.Generate();
    }
}
