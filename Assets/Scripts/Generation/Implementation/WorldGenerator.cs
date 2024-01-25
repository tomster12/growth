using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;
using static VoronoiMeshGenerator;

[ExecuteInEditMode]
public class WorldGenerator : Generator
{
    public override string Name => "World Generator";
    public override Generator[] ComposedGenerators => new Generator[] { planetPolygonGenerator, meshGenerator, biomeGenerator };
    public Mesh Mesh { get; private set; }
    public List<WorldSite> Sites { get; private set; }
    public List<WorldSurfaceEdge> SurfaceEdges { get; private set; }
    public Transform WorldTransform => outsidePolygon.transform;
    public Transform BackDecorContainer => _backDecorContainer;
    public Transform BackgroundContainer => _backgroundContainer;
    public Transform ForegroundContainer => _foregroundContainer;
    public Transform TerrainContainer => _terrainContainer;
    public Transform FrontDecorContainer => _frontDecorContainer;

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

    public override void Generate()
    {
        Clear();

        StepSetContainers();
        StepGenerateMesh();
        StepCalculateOutsideEdges();
        StepCalculateSiteEdgeDistance();
        StepInitializeComponents();
        StepGenerateBiome();

        IsGenerated = true;
    }

    public override void Clear()
    {
        ClearContainers();
        ClearOutput();
        IsGenerated = false;
    }

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
    private SpriteRenderer atmosphere;

    private void ClearContainers()
    {
        for (int i = BackDecorContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = BackDecorContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = BackgroundContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = BackgroundContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = ForegroundContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = ForegroundContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = TerrainContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = TerrainContainer.GetChild(i).gameObject;
            if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
        }
        for (int i = FrontDecorContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = FrontDecorContainer.GetChild(i).gameObject;
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
        Mesh = null;
        Sites = null;
        SurfaceEdges = null;
        if (atmosphere == null) atmosphere = WorldTransform.Find("Atmosphere")?.GetComponent<SpriteRenderer>();
        if (atmosphere != null) DestroyImmediate(atmosphere.gameObject);

        // Cleanup afterwards
        ClearContainers();
    }

    private void StepSetContainers()
    {
        Assert.AreEqual(BackDecorContainer.transform.childCount, 0);
        Assert.AreEqual(BackgroundContainer.transform.childCount, 0);
        Assert.AreEqual(ForegroundContainer.transform.childCount, 0);
        Assert.AreEqual(TerrainContainer.transform.childCount, 0);
        Assert.AreEqual(FrontDecorContainer.transform.childCount, 0);
        Layers.SetLayer(BackDecorContainer, Layer.BACK_DECOR);
        Layers.SetLayer(BackgroundContainer, Layer.BACKGROUND);
        Layers.SetLayer(ForegroundContainer, Layer.FOREGROUND);
        Layers.SetLayer(TerrainContainer, Layer.TERRAIN);
        Layers.SetLayer(FrontDecorContainer, Layer.FRONT_DECOR);
    }

    private void StepGenerateMesh()
    {
        // Generate polygon
        if (!doNotGenerateShape) planetPolygonGenerator.Generate();

        // Generate mesh
        meshGenerator.Generate();
        Mesh = meshGenerator.Mesh;

        // Instantiate ground material
        float noiseScale = planetPolygonGenerator.GetSurfaceRange()[0] / 30.0f;
        var groundMaterial = new Material(meshMaterial);
        meshRenderer.sharedMaterial = groundMaterial;
        meshRenderer.sharedMaterial.SetFloat("_NoiseScale", noiseScale);

        // Generate world sites
        Sites = new List<WorldSite>();
        foreach (MeshSite meshSite in meshGenerator.MeshSites) Sites.Add(new WorldSite(world, meshSite));
    }

    private void StepCalculateOutsideEdges()
    {
        // Calculate external edges
        HashSet<WorldSurfaceEdge> surfaceEdgesUnordered = new HashSet<WorldSurfaceEdge>();
        foreach (WorldSite worldSite in Sites)
        {
            if (worldSite.meshSite.isOutside)
            {
                foreach (MeshSiteEdge edge in worldSite.meshSite.edges)
                {
                    if (edge.isOutside)
                    {
                        WorldSurfaceEdge WorldSurfaceEdge = new WorldSurfaceEdge(worldSite, edge)
                        {
                            a = transform.TransformPoint(Mesh.vertices[worldSite.meshSite.meshVerticesI[edge.siteToVertexI]]),
                            b = transform.TransformPoint(Mesh.vertices[worldSite.meshSite.meshVerticesI[edge.siteFromVertexI]])
                        };
                        WorldSurfaceEdge.length = Vector3.Distance(WorldSurfaceEdge.a, WorldSurfaceEdge.b);
                        surfaceEdgesUnordered.Add(WorldSurfaceEdge);
                    }
                }
            }
        }

        // - Grab random first edge from unordered
        SurfaceEdges = new List<WorldSurfaceEdge>();
        WorldSurfaceEdge first = surfaceEdgesUnordered.First();
        SurfaceEdges.Add(first);
        surfaceEdgesUnordered.Remove(first);

        // - While sites left unordered
        while (surfaceEdgesUnordered.Count > 0)
        {
            WorldSurfaceEdge current = SurfaceEdges.Last();
            MeshSite currentSite = Sites[current.meshSiteEdge.siteIndex].meshSite;
            WorldSurfaceEdge picked = null;

            // - Find first edge that matches in either direction
            foreach (WorldSurfaceEdge checkEdge in surfaceEdgesUnordered)
            {
                MeshSite checkSite = Sites[checkEdge.meshSiteEdge.siteIndex].meshSite;
                MeshSiteVertex toVertex = currentSite.vertices[current.meshSiteEdge.siteToVertexI];
                MeshSiteVertex fromVertex = checkSite.vertices[checkEdge.meshSiteEdge.siteFromVertexI];
                if (toVertex.vertexUID == fromVertex.vertexUID) { picked = checkEdge; break; }
            }
            if (picked == null)
            {
                throw new Exception("Could not find next edge for site " + current.meshSiteEdge.siteIndex + " vertex " + current.meshSiteEdge.siteFromVertexI + " to " + current.meshSiteEdge.siteToVertexI + ", " + SurfaceEdges.Count + " edges left");
            }

            // - Add to ordered
            SurfaceEdges.Add(picked);
            surfaceEdgesUnordered.Remove(picked);
        }
    }

    private void StepCalculateSiteEdgeDistance()
    {
        // Calculate site edge distance using neighbours
        HashSet<int> closedSet = new HashSet<int>();
        Stack<int> openSet = new Stack<int>();

        // - Set edge site to distance 0 and add to open
        foreach (WorldSite worldSite in Sites)
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
            var currentSite = Sites[currentSiteIndex];
            if (closedSet.Contains(currentSiteIndex)) continue;

            // - Check each valid neighbour
            foreach (var neighbourSiteIndex in currentSite.meshSite.neighbouringSites)
            {
                var neighbourSite = Sites[neighbourSiteIndex];
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

    private void StepInitializeComponents()
    {
        // Create atmosphere object
        GameObject atmosphereGO = Instantiate(atmospherePfb);
        atmosphere = atmosphereGO.GetComponent<SpriteRenderer>();
        atmosphere.gameObject.name = "Atmosphere";
        atmosphere.transform.parent = WorldTransform;
        atmosphere.transform.localPosition = Vector3.zero;

        // Set global scale
        float targetScale = atmosphereSizeMax * 2.0f;
        atmosphere.transform.localScale = Vector3.one;
        atmosphere.transform.localScale = new Vector3(
            targetScale / WorldTransform.lossyScale.x,
            targetScale / WorldTransform.lossyScale.y,
            targetScale / WorldTransform.lossyScale.z
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

    private void StepGenerateBiome()
    {
        biomeGenerator.Generate();
    }
}
