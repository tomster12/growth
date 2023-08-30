
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


public class GeneratorController : MonoBehaviour
{
    [SerializeField] public IGeneratorProxy[] proxies = new IGeneratorProxy[0];
    [SerializeField] public int seed = 0;
    [SerializeField] public bool setSeed = false;
    [SerializeField] public bool randomizeSeed = false;


    public void FindGenerators()
    {
        IGenerator[] allIGenerators = GetComponents<IGenerator>();

        List<IGeneratorProxy> compositeIGeneratorProxies = allIGenerators
            .Where(g => g.IsComposite)
            .Select(g => new IGeneratorProxy(g))
            .ToList();

        List<IGeneratorProxy> otherIGeneratorProxies = allIGenerators
            .Where(g => !compositeIGeneratorProxies.Any(cg => cg.IGenerator == g || cg.compositeIGeneratorProxies.Any(p => p.IGenerator == g)))
            .Select(g => new IGeneratorProxy(g))
            .ToList();
        
        List<IGeneratorProxy> allIGeneratorProxies = new List<IGeneratorProxy>();
        allIGeneratorProxies.AddRange(compositeIGeneratorProxies);
        allIGeneratorProxies.AddRange(otherIGeneratorProxies);
        proxies = allIGeneratorProxies.ToArray();
    }

    public void Clear()
    {
        foreach (IGeneratorProxy proxy in proxies) proxy.Clear();
    }

    public void Generate(bool setSeed=false, bool randomizeSeed=false)
    {
        if (this.randomizeSeed || randomizeSeed) RandomizeSeed();
        if (this.setSeed || setSeed) UnityEngine.Random.InitState(seed);
        foreach(IGeneratorProxy proxy in proxies) proxy.Generate();
    }

    public void RandomizeSeed() => seed = (int)DateTime.Now.Ticks;
}
