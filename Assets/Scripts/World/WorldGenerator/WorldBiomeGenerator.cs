using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using static WorldGenerator;

public class WorldBiomeGenerator : Generator
{
    public override string Name => "World Biome";

    public override void Generate()
    {
        Clear();
        StepAssignBiomes();
        StepGenerateEnergy();
        StepPopulateBiomes();
        StepSetColors();
        IsGenerated = true;
    }

    public override void Clear()
    {
        ruleInstances.Clear();
        IsGenerated = false;
    }

    [Header("References")]
    [SerializeField] private WorldGenerator worldGenerator;

    [Header("Config")]
    [SerializeField] private BiomeRequirement[] biomeRequirements;
    [SerializeField] private UndergroundBiome undergroundBiome;
    [SerializeField] private int maxBiomeCount = 4;
    [SerializeField] private bool debugLog = true;

    private List<RuleInstance> ruleInstances = new List<RuleInstance>();

    private void StepAssignBiomes()
    {
        // Goal is to update worldGenerator.SurfaceEdges biomes all the way round
        // - Atleast 1 of each of the required biomes
        // - At most maxBiomeCount biomes
        // First place 1 of each required, then pick randomly
        // Place biomes at random unassigned edges with enough space
        // If cannot find edge with space then pick unassigned edge and make space
        // Once all placed, fill rightwards from each minimum biomes size

        // Sanity checks
        if (maxBiomeCount == 0) return;
        int minBiomeCount = biomeRequirements.Length;
        if (minBiomeCount == 0) return;
        if (minBiomeCount > maxBiomeCount) throw new Exception("Cannot fit " + minBiomeCount + " in " + maxBiomeCount + " max biomes.");

        // Extract edge info from world edges
        int edgeCount = worldGenerator.SurfaceEdges.Count;
        float totalEdgeLength = worldGenerator.SurfaceEdges.Aggregate(0.0f, (acc, edge) => acc + edge.length);
        BiomeGenEdge[] genEdges = new BiomeGenEdge[edgeCount];
        for (int i = 0; i < edgeCount; i++) genEdges[i] = new BiomeGenEdge(i, worldGenerator.SurfaceEdges[i].length, totalEdgeLength);

        // Extract edge info from biome requirements
        float requiredLength = biomeRequirements.Aggregate(0.0f, (acc, req) => req.minimumSize + acc);
        if (totalEdgeLength < requiredLength) throw new Exception("Could not fit required length " + requiredLength + " into total world length " + totalEdgeLength + ".");
        float smallestBiomeLength = biomeRequirements.Aggregate(float.MaxValue, (acc, req) => Mathf.Min(acc, req.minimumSize));

        // Setup loop variables
        float totalLengthLeft = totalEdgeLength;
        float requiredLengthLeft = requiredLength;
        int assignedBiomeCount = 0;

        // Setup helper functions
        int modAdd(int index, int change)
        {
            return (index + genEdges.Length + change) % genEdges.Length;
        };

        bool PlaceBiome(BiomeRequirement req, int startIndex)
        {
            if (debugLog) Debug.Log("PlaceBiome(" + startIndex + ", " + req.minimumSize + ")");

            int index = startIndex;
            List<BiomeGenEdge> biomeEdges = new();

            // Place biomes along edge until required amount reach
            float biomeLength = 0.0f;
            while (biomeLength < req.minimumSize)
            {
                if (genEdges[index].IsAssigned) throw new Exception("PlaceBiome tried to place on an edge with a biome! " + biomeLength + " placed / " + req.minimumSize + " minimum at index " + index + ".");
                biomeLength += genEdges[index].length;
                genEdges[index].req = req;
                biomeEdges.Add(genEdges[index]);
                index = modAdd(index, 1);
            }

            // Update biome edges with full information
            foreach (BiomeGenEdge edge in biomeEdges)
            {
                edge.lengthToBiomeStart = 0.0f;
                edge.biomeTotalLength = biomeLength;
                edge.biomeEndNextIndex = index;
            }

            // Update all previous gen edges length to start
            for (int o = startIndex; ;)
            {
                o = modAdd(o, -1);
                if (genEdges[o].IsAssigned) break;
                genEdges[o].lengthToBiomeStart = genEdges[modAdd(o, 1)].lengthToBiomeStart + genEdges[o].length;
            }

            // Update loop variables
            totalLengthLeft -= biomeLength;
            requiredLengthLeft -= req.minimumSize;
            assignedBiomeCount++;
            return true;
        }

        bool MakeSpace(int index, int capIndex, float length)
        {
            if (debugLog) Debug.Log("MakeSpace(" + index + ", " + capIndex + ", " + length + ")");
            float lengthLeft = length;

            // Loop through until found an edge with a biome
            int startIndex = index;
            while (lengthLeft > 0.0f && !genEdges[index].IsAssigned)
            {
                lengthLeft -= genEdges[index].length;
                index = modAdd(index, 1);
                if (index == startIndex) throw new Exception("Something gone wrong!");
            }
            if (lengthLeft < 0.0f) return true;

            if (debugLog) Debug.Log("Biome at " + index + " needs to be moved by " + lengthLeft);

            // Find how many edges to push back the start to fit
            float startPushedLength = 0.0f;
            int newStartIndex = index;
            for (; startPushedLength < lengthLeft; newStartIndex = modAdd(newStartIndex, 1))
            {
                startPushedLength += genEdges[newStartIndex].length;
            }

            if (debugLog) Debug.Log("Pushed back " + startPushedLength + " to new start " + newStartIndex);

            // Push back the end until >= amount pushed back the start
            float endPushedLength = 0.0f;
            int newEndIndex = genEdges[index].biomeEndNextIndex;
            for (; endPushedLength < startPushedLength; newEndIndex = modAdd(newEndIndex, 1))
            {
                if (newEndIndex == capIndex)
                {
                    if (debugLog) Debug.Log("Cannot move biome into cap at index " + newEndIndex + ", " + (startPushedLength - endPushedLength) + " left to move.");
                    return false;
                }
                // Recurse and push biome if hit another
                if (genEdges[newEndIndex].IsAssigned)
                {
                    if (debugLog) Debug.Log(newEndIndex + " is another biome so recursing down to move by " + (startPushedLength - endPushedLength));
                    bool moveNext = MakeSpace(newEndIndex, capIndex, startPushedLength - endPushedLength);
                    if (!moveNext) return false;
                    if (debugLog) Debug.Log("Successfully moved next biome");
                }

                endPushedLength += genEdges[newEndIndex].length;
            }

            if (debugLog) Debug.Log("End pushed back " + endPushedLength + " / start pushed back " + startPushedLength + " / required " + lengthLeft + " made new end " + newEndIndex);

            // Loop from old start to new end and update edges
            BiomeRequirement req = genEdges[index].req;
            float newBiomeLength = genEdges[index].biomeTotalLength + (endPushedLength - startPushedLength);
            for (int i = index, old = 0; i != newEndIndex; i = modAdd(i, 1))
            {
                if (i == newStartIndex) old = 1;
                if (old == 0)
                {
                    genEdges[i].req = null;
                    genEdges[i].biomeTotalLength = 0.0f;
                    genEdges[i].biomeEndNextIndex = 0;
                }
                else
                {
                    genEdges[i].req = req;
                    genEdges[i].biomeTotalLength = newBiomeLength;
                    genEdges[i].biomeEndNextIndex = newEndIndex;
                }
                genEdges[i].lengthToBiomeStart = 0.0f;
            }

            // Update length to biome start up to moved biome
            for (int o = newStartIndex; ;)
            {
                o = modAdd(o, -1);
                if (genEdges[o].IsAssigned) break;
                genEdges[o].lengthToBiomeStart = genEdges[modAdd(o, 1)].lengthToBiomeStart + genEdges[o].length;
            }

            return true;
        };

        bool PushAndPlaceBiome(BiomeRequirement req, int index, float lengthRequired)
        {
            if (debugLog) Debug.Log("PushAndPlaceBiome(" + index + ", " + lengthRequired + ")");

            BiomeGenEdge edge = genEdges[index];
            if (!MakeSpace(index, index, lengthRequired)) return false;
            PlaceBiome(req, index);
            return true;
        };

        void PrintInfo()
        {
            // Debug log info about biome placement
            Debug.Log("Length Left: " + totalLengthLeft + " / " + totalEdgeLength + " (" + requiredLengthLeft + " required left)");
            Debug.Log("Assigned " + assignedBiomeCount + " biomes.");
            string output = "\n";
            for (int i = 0; i < genEdges.Length; i++) output += genEdges[i].IsAssigned ? "O" : " ";
            output += "\n";
            for (int i = 0; i < genEdges.Length; i++) output += "-";
            Debug.Log(output);
        }

        // Assign biomes until max biomes
        while (assignedBiomeCount < maxBiomeCount)
        {
            if (debugLog) PrintInfo();

            // Pick biome requirement
            // - First, pick each requirement in order
            // - Otherwise, pick random from the fitting biomes
            BiomeRequirement req = null;
            while (req == null)
            {
                if (assignedBiomeCount < biomeRequirements.Length) req = biomeRequirements[assignedBiomeCount];
                else
                {
                    BiomeRequirement[] viableBiomes = biomeRequirements.Where(req => req.minimumSize <= totalLengthLeft).ToArray();
                    Assert.IsTrue(viableBiomes.Length > 0); // Later on break assures this should be bigger than 0
                    req = viableBiomes[(int)UnityEngine.Random.Range(0, viableBiomes.Length)];
                }
            }
            Assert.IsTrue(req.minimumSize <= totalLengthLeft);

            // Find unassigned edges, and then viable edges
            // Viable edges are ones where there is enough space from start -> next biome
            int[] openEdgesIdx = genEdges.Select((e, i) => i).Where(i => !genEdges[i].IsAssigned).ToArray();
            int[] viableEdgesIdx = openEdgesIdx.Where(i => genEdges[i].lengthToBiomeStart > req.minimumSize).ToArray();

            // Place requirement into a random viable edge
            if (viableEdgesIdx.Length > 0)
            {
                int index = viableEdgesIdx[UnityEngine.Random.Range(0, viableEdgesIdx.Length)];
                PlaceBiome(req, index);
            }
            // Force requirement into a random open edge
            // This is allowed because we know there is enough space somewhere
            else
            {
                int index = openEdgesIdx[UnityEngine.Random.Range(0, openEdgesIdx.Length)];
                PushAndPlaceBiome(req, (index + 1) % edgeCount, req.minimumSize);
            }

            // ERROR: Biomes left to place but no space
            if ((assignedBiomeCount < minBiomeCount) && (totalLengthLeft < requiredLengthLeft))
            {
                throw new Exception("Could not finish assignment of biomes, placed " + assignedBiomeCount + " / " + biomeRequirements.Length + ", " + requiredLengthLeft + " needed / " + totalLengthLeft + ".");
            }

            // BREAK: No more length left, but we have fit all required
            else if (totalLengthLeft <= smallestBiomeLength)
            {
                Assert.IsTrue(assignedBiomeCount >= minBiomeCount);
                break;
            }
        }

        if (debugLog) PrintInfo();

        // Apply biomes and fill in the gaps
        int fillStart = -1;
        Biome currentBiome = null;
        for (int i = 0; i != fillStart;)
        {
            if (genEdges[i].IsAssigned)
            {
                if (fillStart == -1) fillStart = i;
                currentBiome = genEdges[i].req.biome;
            }
            if (currentBiome != null) worldGenerator.SurfaceEdges[i].worldSite.biome = currentBiome;
            i = modAdd(i, 1);
        }

        // Breadth-first fill biomes down
        Queue<WorldSite> siteStack = new Queue<WorldSite>();
        foreach (WorldSurfaceEdge edge in worldGenerator.SurfaceEdges) siteStack.Enqueue(edge.worldSite);
        while (siteStack.Count > 0)
        {
            WorldSite current = siteStack.Dequeue();
            foreach (int siteIndex in current.meshSite.neighbouringSitesIdx)
            {
                WorldSite other = worldGenerator.Sites[siteIndex];
                if (other.biome != null) continue;
                if (other.outsideDistance >= undergroundBiome.depth) continue;
                if (siteStack.Contains(other)) continue;
                other.biome = current.biome;
                siteStack.Enqueue(other);
            }
        }

        // Overwrite underground
        foreach (WorldSite site in worldGenerator.Sites)
        {
            if (site.outsideDistance < (undergroundBiome.depth - undergroundBiome.gradientOffset)) continue;
            else if (site.outsideDistance < (undergroundBiome.depth))
            {
                if (UnityEngine.Random.value < undergroundBiome.gradientPct) site.biome = undergroundBiome;
            }
            else site.biome = undergroundBiome;
        }
    }

    private void StepGenerateEnergy()
    {
        // Generate site energy with biome
        foreach (WorldSite site in worldGenerator.Sites)
        {
            Vector2 centre = worldGenerator.Mesh.vertices[site.meshSite.meshCentroidIdx];
            site.maxEnergy = site.biome.energyMaxNoise.GetNoise(centre);
            float pct = site.biome.energyPctNoise.GetNoise(centre);
            if (UnityEngine.Random.value < site.biome.deadspotChance) pct = site.biome.deadspotPct;
            site.energy = pct * site.maxEnergy;
        }
    }

    private void StepPopulateBiomes()
    {
        // For every surface edge
        for (int i = 0; i < worldGenerator.SurfaceEdges.Count; i++)
        {
            WorldSurfaceEdge edge = worldGenerator.SurfaceEdges[i];
            SurfaceBiome biome = edge.biome;
            float pct = (float)i / worldGenerator.SurfaceEdges.Count;

            // Spawn 1 feature for each layers rules
            foreach (var pair in biome.Rules)
            {
                GameObject feature = SpawnFeature(edge, pair.Value, edge.a, edge.b, pct);
                if (feature != null)
                {
                    feature.transform.parent = worldGenerator.Containers[pair.Key];
                    feature.transform.position = new Vector3(feature.transform.position.x, feature.transform.position.y, worldGenerator.Containers[pair.Key].position.z);
                }
            }
        }
    }

    private void StepSetColors()
    {
        // Update colours of the mesh with energy
        Color[] meshColors = new Color[worldGenerator.Mesh.vertexCount];
        foreach (WorldSite site in worldGenerator.Sites)
        {
            float pct = site.energy / site.maxEnergy;
            Color col = Color.Lerp(site.biome.colorRange[0], site.biome.colorRange[1], pct);
            meshColors[site.meshSite.meshCentroidIdx] = col;
            foreach (int v in site.meshSite.meshVerticesIdx) meshColors[v] = col;
        }

        worldGenerator.Mesh.colors = meshColors;
    }

    private GameObject SpawnFeature(WorldSurfaceEdge edge, FeatureRule[] rules, Vector3 a, Vector3 b, float edgePct)
    {
        // Pick rule
        float r = UnityEngine.Random.value;
        FeatureRule rule = PickRule(rules, edge, r);
        if (rule == null) return null;

        // Ensure distance
        if (rule.minDistance != 0.0f)
        {
            Vector3 centre = (a + b) / 2.0f;
            RuleInstance[] matchingInstances = ruleInstances.Where(i => i.rule == rule).ToArray();
            foreach (RuleInstance instance in matchingInstances)
            {
                float dst = Vector3.Distance(centre, instance.IWorldFeature.GetPosition());
                if (dst < rule.minDistance) return null;
            }
        }

        // Spawn feature
        GameObject feature = Instantiate(rule.feature);
        IWorldFeature IWorldFeature = feature.GetComponent<IWorldFeature>();
        IWorldFeature?.Spawn(edge, edge.a, edge.b, edgePct);
        ruleInstances.Add(new RuleInstance(rule, IWorldFeature));
        return feature;
    }

    private FeatureRule PickRule(FeatureRule[] rules, WorldSurfaceEdge edge, float r)
    {
        float length = edge.length;
        List<FeatureRule> hitRules = new();
        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i].isGuaranteed) hitRules.Add(rules[i]);
            else if (r < (rules[i].averagePer100 * (length / 100.0f))) hitRules.Add(rules[i]);
        }
        if (hitRules.Count > 0) return hitRules[UnityEngine.Random.Range(0, hitRules.Count)];
        else return null;
    }

    private class BiomeGenEdge
    {
        public BiomeRequirement req;
        public int index;
        public float length;
        public float lengthToBiomeStart;
        public float biomeTotalLength;
        public int biomeEndNextIndex;

        public BiomeGenEdge(int index, float length, float lengthToBiomeStart)
        {
            this.index = index;
            this.length = length;
            this.lengthToBiomeStart = lengthToBiomeStart;
            this.biomeTotalLength = 0.0f;
            this.biomeEndNextIndex = 0;
        }

        public bool IsAssigned => req != null;
    };

    [Serializable]
    private class BiomeRequirement
    {
        public SurfaceBiome biome;
        public int requiredCount;
        public float minimumSize;
    };

    private class RuleInstance
    {
        public FeatureRule rule;
        public IWorldFeature IWorldFeature;

        public RuleInstance(FeatureRule rule, IWorldFeature feature)
        {
            this.rule = rule;
            this.IWorldFeature = feature;
        }
    };
}
