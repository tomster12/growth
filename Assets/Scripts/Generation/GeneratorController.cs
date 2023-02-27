
using System;
using UnityEngine;


public abstract class Generator : MonoBehaviour
{
    public abstract void Generate();
    public virtual void ClearInternal() { }
    public virtual void ClearOutput() { }
}


[ExecuteInEditMode]
public class GeneratorController : MonoBehaviour
{
    // --- Static ---
    public static int globalSeed;

    [Header("Config")]
    [SerializeField] private int seed = 0;
    [SerializeField] public bool randomizeSeed = false;
    [SerializeField] public bool resetSeed = true;
    [SerializeField] public bool toUpdate = false;
    [SerializeReference] public Generator[] generators;


    private void Update()
    {
        if (toUpdate)
        {
            Generate();
            toUpdate = false;
        }
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        // Set seed to preset or random
        if (randomizeSeed) seed = (int)DateTime.Now.Ticks;
        if (resetSeed)
        {
            UnityEngine.Random.InitState(seed);
            globalSeed = seed;
        }

        // Run the generator
        foreach(Generator generator in generators) generator.Generate();
    }


    [ContextMenu("Get All Generators")]
    public void GetAllGenerators() => generators = GetComponents<Generator>();
}
