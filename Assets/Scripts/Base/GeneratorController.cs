using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeneratorController : MonoBehaviour
{
    [SerializeField] public int Seed = 0;
    [SerializeField] public bool ToRandomizeSeed = false;
    [SerializeField] public bool ToOverwriteSeed = false;

    public void FindGenerators()
    {
        Generator[] allGenerators = GetComponents<Generator>();

        generators = allGenerators
            .Where(g1 => !allGenerators.Any(g2 => g2.ContainsGenerator(g1))).ToArray();
    }

    public void Generate(bool overwriteSeed = false, bool randomizeSeed = false)
    {
        if (generators.Length == 0) FindGenerators();
        if (ToRandomizeSeed || randomizeSeed) RandomizeSeed();
        if (ToOverwriteSeed || overwriteSeed) UnityEngine.Random.InitState(Seed);
        foreach (Generator generator in generators) generator.Generate();
    }

    public void Clear()
    {
        foreach (Generator generator in generators) generator.Clear();
    }

    public void RandomizeSeed() => Seed = (int)DateTime.Now.Ticks;

    [SerializeField] private Generator[] generators = new Generator[0];

    private void OnValidate()
    {
        FindGenerators();
    }
}
