
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;
using static VoronoiMeshGenerator;
using static WorldManager;

public class WorldFoliageManager : MonoBehaviour
{
    // -- Editor --
    [Header("References")]
    [SerializeField] WorldManager worldManager;
    [SerializeField] Transform grassContainer;
    [SerializeField] GameObject grassPfb;

    [Header("Config")]
    [SerializeField] private NoiseData grassHeightNoise = new NoiseData(new float[2] { 0.5f, 1.0f });
    [SerializeField] private Color[] grassColorRange = new Color[] { new Color(0, 0, 0), new Color(1, 1, 1) };

    // -- Main --
    public List<SpriteRenderer> surfaceGrass { get; private set; }


    public void Generate()
    {
        GenerateGrass();
    }

    [ContextMenu("Clear Grass")]
    public void ClearMain()
    {
        // Delete grass container children
        for (int i = grassContainer.childCount - 1; i >= 0; i--) GameObject.DestroyImmediate(grassContainer.GetChild(i).gameObject);
        if (surfaceGrass == null) surfaceGrass = new List<SpriteRenderer>();
        surfaceGrass.Clear();
    }


    private void GenerateGrass()
    {
        // For each edge (clockwise)
        surfaceGrass = new List<SpriteRenderer>();
        for (int i = 0; i < worldManager.surfaceEdges.Count; i++)
        {
            MeshSiteEdge edge = worldManager.surfaceEdges[i];
            MeshSite site = worldManager.worldSites[edge.siteIndex].meshSite;
            Vector2 a = worldManager.mesh.vertices[site.meshVerticesI[edge.siteToVertexI]];
            Vector2 b = worldManager.mesh.vertices[site.meshVerticesI[edge.siteFromVertexI]];
            Vector2 dir = (b - a);
            bool toFlipX = UnityEngine.Random.value < 0.5f;

            // Generate grass and set position
            GameObject grass = Instantiate(grassPfb);
            grass.transform.parent = grassContainer;
            if (toFlipX) grass.transform.position = transform.TransformPoint(a + dir);
            else grass.transform.position = transform.TransformPoint(a);
            grass.transform.right = dir.normalized;

            // Grow sprite to correct size
            float width = dir.magnitude + 0.04f;
            float height = grassHeightNoise.GetCyclicNoise((float)i / worldManager.surfaceEdges.Count);
            SpriteRenderer renderer = grass.GetComponent<SpriteRenderer>();
            renderer.size = new Vector2(width, renderer.size.y);
            renderer.flipX = toFlipX;
            renderer.transform.localScale = new Vector3(renderer.transform.localScale.x, height, renderer.transform.localScale.z);
            surfaceGrass.Add(renderer);
        }
    }


    public void UpdateColours()
    {
        // Update colors of the grass
        for (int i = 0; i < surfaceGrass.Count; i++)
        {
            SpriteRenderer sprite = surfaceGrass[i];
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
                col = Color.Lerp(grassColorRange[0], grassColorRange[1], pct);
            }

            sprite.color = col;
        }
    }
}
