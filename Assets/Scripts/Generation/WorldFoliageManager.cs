
using System.Collections.Generic;
using UnityEngine;
using static VoronoiMeshGenerator;
using static WorldManager;


public class WorldFoliageManager : MonoBehaviour
{
    // -- Parameters --
    [Header("Parameters")]
    [SerializeField] WorldManager worldManager;
    [SerializeField] Transform foliageContainer;

    // -- Output --
    public List<SpriteRenderer> surfaceFoliage { get; private set; }


    [ContextMenu("Generate Foliage")]
    public void Generate()
    {
        _GenerateFoliage();
    }

    [ContextMenu("Clear Output")]
    public void ClearOutput()
    {
        // Delete foliage container children
        for (int i = foliageContainer.childCount - 1; i >= 0; i--) GameObject.DestroyImmediate(foliageContainer.GetChild(i).gameObject);
        if (surfaceFoliage == null) surfaceFoliage = new List<SpriteRenderer>();
        surfaceFoliage.Clear();
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
            if (toFlipX) grass.transform.position = transform.TransformPoint(a + dir);
            else grass.transform.position = transform.TransformPoint(a);
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
