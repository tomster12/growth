
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;




public class IGeneratorController : MonoBehaviour
{
    public int seed = 0;
    [SerializeField] public IGenerator[] generators = new IGenerator[0];
    [SerializeReference] public IGeneratorWrapper[] wrappers = new IGeneratorWrapper[0];


    public void FindGenerators()
    {
        generators = GetComponents<IGenerator>();
        wrappers = generators.Select(g => new IGeneratorWrapper(g)).ToArray();
        Debug.Log("IGeneratorController::FindGenerators() Found " + generators.Length);
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
        foreach(IGenerator generator in generators) generator.Generate();
    }
}
