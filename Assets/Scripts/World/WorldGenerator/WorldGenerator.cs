using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteInEditMode]
public partial class WorldGenerator : Generator
{
    public override string Name => "World Generator";
    public override Generator[] ComposedGenerators => new Generator[] { planetPolygonGenerator, meshGenerator, biomeGenerator };
    public Mesh Mesh { get; private set; }
    public Dictionary<GameLayer, Transform> Containers { get; private set; }
    public Transform Transform => outsidePolygon.transform;
    public List<WorldSurfaceEdge> SurfaceEdges => surfaceEdges;
    public List<WorldSite> Sites => sites;
    public float AtmosphereRadius => atmosphereSizeMax;

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
        StepCalculateSurfaceEdges();
        StepCalculateSiteEdgeDistance();
        StepInitializeComponents();
        StepGenerateBiome();

        IsGenerated = true;
    }

    public override void Clear()
    {
        // Clear required components
        planetPolygonGenerator.Clear();
        meshGenerator.Clear();
        biomeGenerator.Clear();

        // Clear atmosphere
        if (atmosphere == null) atmosphere = Transform.Find("Atmosphere")?.GetComponent<SpriteRenderer>();
        if (atmosphere != null) DestroyImmediate(atmosphere.gameObject);

        // Clear containers
        foreach (var pair in Containers)
        {
            for (int i = pair.Value.childCount - 1; i >= 0; i--)
            {
                GameObject child = pair.Value.GetChild(i).gameObject;
                if (!child.CompareTag("DoNotClear")) DestroyImmediate(child);
            }
        }

        Mesh = null;
        sites = null;
        surfaceEdges = null;
        atmosphere = null;
        IsGenerated = false;
    }

    [Header("====== References ======", order = 0)]
    [Header("Generators", order = 1)]
    [SerializeField] private PlanetPolygonGenerator planetPolygonGenerator;
    [SerializeField] private VoronoiMeshGenerator meshGenerator;
    [SerializeField] private WorldBiomeGenerator biomeGenerator;
    [Space(6, order = 0)]
    [Header("Containers", order = 1)]
    [SerializeField] private Transform frontDecorContainer;
    [SerializeField] private Transform terrainContainer;
    [SerializeField] private Transform worldContainer;
    [SerializeField] private Transform backDecorContainer;
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
    [SerializeField] private int pipelineMaxTries = 5;
    [SerializeField] private bool hasAtmosphere = true;
    [SerializeField] private float atmosphereSizeMin = 150.0f;
    [SerializeField] private float atmosphereSizeMax = 200.0f;
    [SerializeField] private float gravityForce = 10.0f;
    [Header("Gizmos", order = 1)]
    [SerializeField] private bool showGizmoWorldSurface = false;
    [SerializeField] private bool showGizmoWorldSites = false;

    // Serialize so they still exist ingame
    [HideInInspector][SerializeField] private List<WorldSurfaceEdge> surfaceEdges;
    [HideInInspector][SerializeField] private List<WorldSite> sites;
    private SpriteRenderer atmosphere;

    private void StepSetContainers()
    {
        foreach (var pair in Containers)
        {
            Assert.AreEqual(pair.Value.childCount, 0);
            GameLayers.SetLayer(pair.Value, pair.Key);
        }
    }

    private void StepGenerateMesh()
    {
        // Generate polygonv and mesh
        planetPolygonGenerator.Generate();
        meshGenerator.Generate();
        Mesh = meshGenerator.Mesh;

        // Instantiate ground material
        float noiseScale = planetPolygonGenerator.GetSurfaceRange()[0] / 40.0f;
        Material meshMaterialInst = new Material(meshMaterial);
        meshRenderer.sharedMaterial = meshMaterialInst;
        meshRenderer.sharedMaterial.SetFloat("_NoiseScale", noiseScale);

        // Generate world sites
        sites = new List<WorldSite>();
        foreach (MeshSite meshSite in meshGenerator.MeshSites) sites.Add(new WorldSite(world, meshSite));
    }

    private void StepCalculateSurfaceEdges()
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
                        WorldSurfaceEdge.InitPositions(Transform, Mesh);
                        surfaceEdgesUnordered.Add(WorldSurfaceEdge);
                    }
                }
            }
        }

        // Grab random first edge from unordered
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

            // - Find first edge that matches to == from
            foreach (WorldSurfaceEdge checkEdge in surfaceEdgesUnordered)
            {
                MeshSite checkSite = sites[checkEdge.meshSiteEdge.siteIndex].meshSite;
                MeshSiteVertex toVertex = currentSite.vertices[current.meshSiteEdge.toVertexIndex];
                MeshSiteVertex fromVertex = checkSite.vertices[checkEdge.meshSiteEdge.fromVertexIndex];
                if (toVertex.vertexUID == fromVertex.vertexUID) { picked = checkEdge; break; }
            }
            if (picked == null)
            {
                throw new Exception("Could not find next edge for site " + current.meshSiteEdge.siteIndex + " vertex " + current.meshSiteEdge.fromVertexIndex + " to " + current.meshSiteEdge.toVertexIndex + ", " + surfaceEdges.Count + " edges left");
            }

            // - Add to ordered
            surfaceEdges.Add(picked);
            surfaceEdgesUnordered.Remove(picked);
        }
    }

    private void StepCalculateSiteEdgeDistance()
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
            foreach (var neighbourSiteIndex in currentSite.meshSite.neighbouringSiteIndices)
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

    private void StepInitializeComponents()
    {
        // Create atmosphere object
        if (hasAtmosphere)
        {
            GameObject atmosphereGO = Instantiate(atmospherePfb);
            atmosphere = atmosphereGO.GetComponent<SpriteRenderer>();
            atmosphere.gameObject.name = "Atmosphere";
            atmosphere.transform.parent = Transform;
            atmosphere.transform.localPosition = Vector3.zero;

            // Set global scale
            float targetScale = atmosphereSizeMax * 2.0f;
            atmosphere.transform.localScale = Vector3.one;
            atmosphere.transform.localScale = new Vector3(
                targetScale / Transform.lossyScale.x,
                targetScale / Transform.lossyScale.y,
                targetScale / Transform.lossyScale.z
            );

            // Set shader material and shader min and max
            Material atmosphereMaterial = new Material(atmosphere.sharedMaterial);
            atmosphere.sharedMaterial = atmosphereMaterial;
            atmosphere.sharedMaterial.SetFloat("_Min", atmosphereSizeMin);
            atmosphere.sharedMaterial.SetFloat("_Max", atmosphereSizeMax);
        }

        // Update gravity
        gravityAttractor.gravityRadius = atmosphereSizeMax;
        gravityAttractor.gravityForce = gravityForce;
    }

    private void StepGenerateBiome()
    {
        biomeGenerator.Generate();
    }

    private void OnValidate()
    {
        Containers ??= new Dictionary<GameLayer, Transform>();
        Containers[GameLayer.FrontDecor] = frontDecorContainer;
        Containers[GameLayer.Terrain] = terrainContainer;
        Containers[GameLayer.World] = worldContainer;
        Containers[GameLayer.BackDecor] = backDecorContainer;
    }

    private void OnDrawGizmos()
    {
        // Draw surface edges
        if (showGizmoWorldSurface && surfaceEdges != null)
        {
            for (int i = 0; i < surfaceEdges.Count; i++)
            {
                Vector2 centrePos = surfaceEdges[i].centre;
                Vector2 labelPos = centrePos + Vector2.right * 0.25f;

                Gizmos.color = Color.white;
                Gizmos.DrawSphere(centrePos, 0.1f);

                string label = surfaceEdges[i].biome.name + "(" + i + ")";
                Handles.Label(labelPos, label);
            }
        }

        // Draw world sites
        if (showGizmoWorldSites && sites != null)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                Vector2 centrePos = Transform.TransformPoint(Mesh.vertices[sites[i].meshSite.centroidMeshIndex]);
                Vector2 labelPos = centrePos + Vector2.right * 0.25f;

                Gizmos.color = Color.white;
                Gizmos.DrawSphere(centrePos, 0.15f);

                string label = i.ToString();
                Handles.Label(labelPos, label);
            }
        }
    }
}
