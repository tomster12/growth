
using UnityEngine;
using System;
using static WorldGenerator;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

public class WorldBiomeGenerator : MonoBehaviour, IGenerator
{
    private class BiomeGenEdge
    {
        public BiomeRequirement req;
        public int index;
        public float length;
        public float lengthToBiomeStart;
        public float biomeTotalLength;
        public int biomeEndNextIndex;
        public bool IsAssigned => req != null;

        public BiomeGenEdge(int index, float length, float lengthToBiomeStart)
        {
            this.index = index;
            this.length = length;
            this.lengthToBiomeStart = lengthToBiomeStart;
            this.biomeTotalLength = 0.0f;
            this.biomeEndNextIndex = 0;
        }
    };

    [Serializable]
    private class BiomeRequirement
    {
        public SurfaceBiome biome;
        public int requiredCount;
        public float minimumSize;
    };

    public class RuleInstance
    {
        public EdgeRule rule;
        public IFeature IFeature;
        
        public RuleInstance(EdgeRule rule, IFeature feature)
        {
            this.rule = rule;
            this.IFeature = feature;
        }
    };


    [Header("References")]
    [SerializeField] private WorldGenerator worldGenerator;

    [Header("Config")]
    [SerializeField] private BiomeRequirement[] biomeRequirements;
    [SerializeField] private UndergroundBiome undergroundBiome;
    [SerializeField] private int maxBiomeCount = 4;

    public bool isGenerated { get; private set; } = false;
    public bool IsGenerated() => isGenerated;

    private List<RuleInstance> ruleInstances = new List<RuleInstance>();


    public void Clear()
    {
        ruleInstances.Clear();
        isGenerated = false;
    }

    public void Generate()
    {
        Clear();
        Step_AssignBiomes();
        Step_GenerateEnergy();
        Step_PopulateBiomes();
        Step_SetColors();
        isGenerated = true;
    }

    public string GetName() => "World Biome";


    private void Step_AssignBiomes()
    {
        // Sanity checks
        int requiredBiomeCount = biomeRequirements.Length;
        if (requiredBiomeCount == 0) return;
        if (requiredBiomeCount > maxBiomeCount) throw new Exception("Cannot fit " + requiredBiomeCount + " in " + maxBiomeCount + " max biomes.");
        if (maxBiomeCount == 0) return;

        // Calculate useful variables
        int totalEdges = worldGenerator.surfaceEdges.Count;
        float totalLength = worldGenerator.surfaceEdges.Aggregate(0.0f, (acc, edge) => acc + edge.length);
        int targetBiomeCount = UnityEngine.Random.Range(requiredBiomeCount, maxBiomeCount + 1);
        BiomeGenEdge[] edgeInfos = new BiomeGenEdge[totalEdges];
        for (int i = 0; i < totalEdges; i++) edgeInfos[i] = new BiomeGenEdge(i, worldGenerator.surfaceEdges[i].length, totalLength);
        
        // Check whether is even possible
        float requiredLength = biomeRequirements.Aggregate(0.0f, (acc, req) => req.minimumSize + acc);
        float smallestBiomeLength = biomeRequirements.Aggregate(float.MaxValue, (acc, req) => Mathf.Min(acc, req.minimumSize));
        if (totalLength < requiredLength) throw new Exception("Could not fit required length " + requiredLength + " into total world length " + totalLength + ".");

        // Setup loop variables
        float totalLengthLeft = totalLength;
        float requiredLengthLeft = requiredLength;
        int assignedBiomeCount = 0;

        // Setup helper functions
        int modAdd(int index, int change)
        {
            return (index + edgeInfos.Length + change) % edgeInfos.Length;
        };
        Func<BiomeRequirement, int, bool> PlaceBiome = (BiomeRequirement req,  int startIndex) =>
        {
            float lengthLeft = req.minimumSize;
            int index = startIndex;
            List<BiomeGenEdge> biomeEdges = new();
            while (lengthLeft > 0.0f)
            {
                if (edgeInfos[index].IsAssigned) throw new Exception ("PlaceBiome tried to place on an edge with a biome! " + lengthLeft + " left to place / " + req.minimumSize + " minimum at index " + index + ".");
                lengthLeft -= edgeInfos[index].length;
                edgeInfos[index].req = req;
                biomeEdges.Add(edgeInfos[index]);
                index = modAdd(index, 1);
            }
            float biomeLength = req.minimumSize - lengthLeft;
            foreach (BiomeGenEdge edge in biomeEdges)
            {
                edge.lengthToBiomeStart = 0.0f;
                edge.biomeTotalLength = biomeLength;
                edge.biomeEndNextIndex = index;
            }
            for (int o = startIndex; !edgeInfos[o].IsAssigned;)
            {
                edgeInfos[o].lengthToBiomeStart = edgeInfos[modAdd(o, 1)].lengthToBiomeStart + edgeInfos[o].length;
                o = modAdd(o, -1);
            }
            totalLengthLeft -= biomeLength;
            requiredLengthLeft -= req.minimumSize;
            assignedBiomeCount++;
            return true;
        };

        bool MakeSpace(int index, int capIndex, float length)
        {
            float lengthLeft = length;
            while (lengthLeft > 0.0f && !edgeInfos[index].IsAssigned)
            {
                lengthLeft -= edgeInfos[index].length;
                index = modAdd(index, 1);
            }
            if (lengthLeft < 0.0f) return true;
            if (edgeInfos[index].IsAssigned)
            {
                float previousLength = edgeInfos[index].biomeTotalLength;
                BiomeRequirement previousReq = edgeInfos[index].req;
                float choppedAmount = 0.0f;
                int newStartIndex = index; 
                for (; choppedAmount < lengthLeft; newStartIndex = modAdd(newStartIndex, 1))
                {
                    choppedAmount += edgeInfos[newStartIndex].length;
                }
                float movedAmount = 0.0f;
                int newEndIndex = edgeInfos[index].biomeEndNextIndex;
                for (; movedAmount < choppedAmount; newEndIndex = modAdd(newEndIndex, 1))
                {
                    if (newEndIndex == capIndex)
                    {
                        Debug.Log("Cannot move biome into cap at index " + newEndIndex + ", " + (choppedAmount - movedAmount) + " left to move.");
                        return false;
                    }
                    if (edgeInfos[newEndIndex].IsAssigned)
                    {
                        Debug.Log(newEndIndex + " is another biome so recursing down to move by " + (choppedAmount - movedAmount));
                        bool moveNext = MakeSpace(newEndIndex, capIndex, choppedAmount - movedAmount);
                        if (!moveNext) return false;
                    }
                    movedAmount += edgeInfos[newEndIndex].length;
                }
                float newBiomeLength = previousLength - choppedAmount + movedAmount;
                for (int i = index; i != newEndIndex; i = modAdd(i, 1))
                {
                    if (i < newStartIndex)
                    {
                        edgeInfos[i].req = null;
                        edgeInfos[i].biomeTotalLength = 0.0f;
                        edgeInfos[i].biomeEndNextIndex = 0;
                    }
                    else
                    {
                        edgeInfos[i].req = previousReq;
                        edgeInfos[i].biomeTotalLength = newBiomeLength;
                        edgeInfos[i].biomeEndNextIndex = newEndIndex;
                    }
                    edgeInfos[i].lengthToBiomeStart = 0.0f;
                }
                for (int o = newStartIndex; !edgeInfos[o].IsAssigned;)
                {
                    edgeInfos[o].lengthToBiomeStart = edgeInfos[modAdd(o, 1)].lengthToBiomeStart + edgeInfos[o].length;
                    o = modAdd(o, -1);
                }
            }
            return true;
        };

        bool PushAndPlace(BiomeRequirement req, int index, float lengthRequired)
        {
            BiomeGenEdge edge = edgeInfos[index];
            if (!MakeSpace(index, index, lengthRequired)) return false;
            PlaceBiome(req, index);
            return true;
        };

        // Begin main biome assignment
        while (assignedBiomeCount < targetBiomeCount)
        {
            // Pick next / random biome and ensure theres space
            BiomeRequirement req = null;
            while (req == null)
            {
                if (assignedBiomeCount < biomeRequirements.Length) req = biomeRequirements[assignedBiomeCount];
                else req = biomeRequirements[(int)UnityEngine.Random.Range(0, biomeRequirements.Length)];
                if (totalLengthLeft < req.minimumSize) req = null;
            }

            // Find all viable edges and place / push and place
            int[] openEdgesI = edgeInfos.Select((e, i) => i).Where(i => !edgeInfos[i].IsAssigned).ToArray();
            int[] viableEdgesI = openEdgesI.Where(i => edgeInfos[i].lengthToBiomeStart > req.minimumSize).ToArray();
            if (viableEdgesI.Length > 0)
            {
                int index = viableEdgesI[UnityEngine.Random.Range(0, viableEdgesI.Length)];
                PlaceBiome(req, index);
            }
            else
            {
                int index = openEdgesI[UnityEngine.Random.Range(0, openEdgesI.Length)];
                PushAndPlace(req, (index + 1) % totalEdges, req.minimumSize);
            }

            // ERRROR: Biomes left to place but no space
            if ((assignedBiomeCount < requiredBiomeCount) && (totalLengthLeft < requiredLengthLeft))
            {
                throw new Exception("Could not finish assignment of biomes, placed " + assignedBiomeCount + " / " + biomeRequirements.Length  + ", " + requiredLengthLeft + " needed / " + totalLengthLeft + ".");
            }

            // BREAK: no more length left
            else if (totalLengthLeft <= smallestBiomeLength)
            {
                Assert.IsTrue(assignedBiomeCount >= requiredBiomeCount);
                break;
            }

            // BREAK: placed all biomes
            if (assignedBiomeCount >= targetBiomeCount) break;
        }

        // Temporary half and half
        for (int i = 0; i < worldGenerator.surfaceEdges.Count; i++)
        {
            if (i < worldGenerator.surfaceEdges.Count / 2.0f)
            {
                worldGenerator.surfaceEdges[i].worldSite.biome = biomeRequirements[0].biome;
            }
            else
            {
                worldGenerator.surfaceEdges[i].worldSite.biome = biomeRequirements[1].biome;
            }
        }
        
        // Flood fill biomes down
        Queue<WorldSite> floodFillOpenSet = new Queue<WorldSite>();
        foreach (WorldSurfaceEdge edge in worldGenerator.surfaceEdges) floodFillOpenSet.Enqueue(edge.worldSite);
        while (floodFillOpenSet.Count > 0)
        {
            WorldSite current = floodFillOpenSet.Dequeue();
            foreach (int siteIndex in current.meshSite.neighbouringSites)
            {
                WorldSite other = worldGenerator.sites[siteIndex];
                if (other.biome != null) continue;
                if (other.outsideDistance >= undergroundBiome.depth) continue;
                if (floodFillOpenSet.Contains(other)) continue;
                other.biome = current.biome;
                floodFillOpenSet.Enqueue(other);
            }
        }

        // Overwrite underground
        foreach (WorldSite site in worldGenerator.sites)
        {
            if (site.outsideDistance < (undergroundBiome.depth - undergroundBiome.gradientOffset)) continue;
            else if (site.outsideDistance < (undergroundBiome.depth))
            {
                if (UnityEngine.Random.value < undergroundBiome.gradientPct) site.biome = undergroundBiome;
            }
            else site.biome = undergroundBiome;
        }
    }
     
    private void Step_GenerateEnergy()
    {
        // Generate site energy with biome
        foreach (WorldSite site in worldGenerator.sites)
        {
            Vector2 centre = worldGenerator.mesh.vertices[site.meshSite.meshCentroidI];
            site.maxEnergy = site.biome.energyMaxNoise.GetNoise(centre);
            float pct = site.biome.energyPctNoise.GetNoise(centre);
            if (UnityEngine.Random.value < site.biome.deadspotChance) pct = site.biome.deadspotPct;
            site.energy = pct * site.maxEnergy;
        }
    }

    private void Step_PopulateBiomes() 
    {
        for (int i = 0; i < worldGenerator.surfaceEdges.Count; i++)
        {
            WorldSurfaceEdge edge = worldGenerator.surfaceEdges[i];
            float pct = (float)i / worldGenerator.surfaceEdges.Count;

            // Generate frontDecor feature
            GameObject frontDecor = SpawnFeature(edge, edge.worldSite.biome.frontDecorRules, edge.a, edge.b, pct);
            if (frontDecor != null)
            {
                frontDecor.transform.parent = worldGenerator.frontDecorContainer;
                frontDecor.transform.position = new Vector3(frontDecor.transform.position.x, frontDecor.transform.position.y, worldGenerator.frontDecorContainer.position.z);
            }

            // Generate terrain feature
            GameObject terrainFeature = SpawnFeature(edge, edge.worldSite.biome.terrainRules, edge.a, edge.b, pct);
            if (terrainFeature != null)
            {
                terrainFeature.transform.parent = worldGenerator.terrainContainer;
                terrainFeature.transform.position = new Vector3(terrainFeature.transform.position.x, terrainFeature.transform.position.y, worldGenerator.terrainContainer.position.z);
                continue;
            }

            // Generate backDecor feature
            GameObject backDecor = SpawnFeature(edge, edge.worldSite.biome.backDecorRules, edge.a, edge.b, pct);
            if (backDecor != null)
            {
                backDecor.transform.parent = worldGenerator.backDecorContainer;
                backDecor.transform.position = new Vector3(backDecor.transform.position.x, backDecor.transform.position.y, worldGenerator.backDecorContainer.position.z);
            }

            // Generate foreground feature
            GameObject foreground = SpawnFeature(edge, edge.worldSite.biome.foregroundRules, edge.a, edge.b, pct);
            if (foreground != null)
            {
                foreground.transform.parent = worldGenerator.foregroundContainer;
                foreground.transform.position = new Vector3(foreground.transform.position.x, foreground.transform.position.y, worldGenerator.foregroundContainer.position.z);
                continue;
            }

            // Generate background feature
            GameObject background = SpawnFeature(edge, edge.worldSite.biome.backgroundRules, edge.a, edge.b, pct);
            if (background != null)
            {
                background.transform.parent = worldGenerator.backgroundContainer;
                background.transform.position = new Vector3(background.transform.position.x, background.transform.position.y, worldGenerator.backgroundContainer.position.z);
                continue;
            }
        }
    }

    private void Step_SetColors()
    {
        // Update colours of the mesh with energy
        Color[] meshColors = new Color[worldGenerator.mesh.vertexCount];
        foreach (WorldSite site in worldGenerator.sites)
        {
            float pct = site.energy / site.maxEnergy;
            Color col = Color.Lerp(site.biome.colorRange[0], site.biome.colorRange[1], pct);
            meshColors[site.meshSite.meshCentroidI] = col;
            foreach (int v in site.meshSite.meshVerticesI) meshColors[v] = col;
        }

        worldGenerator.mesh.colors = meshColors;
    }

    private EdgeRule PickRule(EdgeRule[] rules, float r)
    {
        EdgeRule rule = null;
        float sum = 0.0f;
        for (int i = 0; i < rules.Length; i++)
        {
            rule = rules[i];
            sum += rule.chance;
            if (r < sum) break;
        }
        return r > sum ? null : rule;
    }

    private GameObject SpawnFeature(WorldSurfaceEdge edge, EdgeRule[] rules, Vector3 a, Vector3 b, float edgePct)
    {
        // Pick rule
        float r = UnityEngine.Random.value;
        EdgeRule rule = PickRule(rules, r);
        if (rule == null) return null;

        // Ensure distance
        if (rule.minDistance != 0.0f)
        {
            Vector3 centre = (a + b) / 2.0f;
            RuleInstance[] matchingInstances = ruleInstances.Where(i => i.rule == rule).ToArray();
            foreach (RuleInstance instance in matchingInstances)
            {
                float dst = Vector3.Distance(centre, instance.IFeature.GetPosition());
                if (dst < rule.minDistance) return null;
            }
        }

        // Spawn feature
        GameObject feature = Instantiate(rule.feature);
        IFeature IFeature = feature.GetComponent<IFeature>();
        IFeature?.Spawn(edge, edge.a, edge.b, edgePct);
        ruleInstances.Add(new RuleInstance(rule, IFeature));
        return feature;
    }
}
