
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;


public class CompositeGeneratorController : MonoBehaviour
{
    public int seed = 0;
    [SerializeField] public IGeneratorProxy[] proxies = new IGeneratorProxy[0];


    public void FindGenerators()
    {
        proxies = GetComponents<IGenerator>().Select(g => new IGeneratorProxy(g)).ToArray();
        Debug.Log("CompositeGeneratorController::FindGenerators() Found " + proxies.Length);
    }

    public void Generate(bool setSeed=false, bool randomizeSeed=false)
    {
        Debug.Log("GeneratorController::Generate()");
        if (randomizeSeed)
        {
            seed = (int)DateTime.Now.Ticks;
        }
        if (setSeed)
        {
            UnityEngine.Random.InitState(seed);
        }
        foreach(IGeneratorProxy proxy in proxies) proxy.IGenerator.Generate();
    }
}
