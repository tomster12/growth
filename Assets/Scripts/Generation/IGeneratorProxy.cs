
using UnityEngine;
using System;
using System.Linq;


[Serializable]
public class IGeneratorProxy
{
    [SerializeField] private Component IGeneratorComponent;
    [SerializeField] public string name;
    [SerializeField] public bool isEnabled;
    [SerializeField] public bool isGenerated;
    [SerializeField] public bool isComposite;
    [SerializeField] public IGeneratorProxy[] compositeIGeneratorProxies;

    public IGenerator IGenerator => (IGenerator)IGeneratorComponent;
    

    public IGeneratorProxy(IGenerator IGenerator, bool isEnabled=true)
    {
        IGeneratorComponent = (Component)IGenerator;
        name = IGenerator.Name;
        this.isEnabled = isEnabled;
        isComposite = IGenerator.IsComposite;
        compositeIGeneratorProxies = IGenerator.GetCompositeIGenerators().Select(g => new IGeneratorProxy(g, false)).ToArray();
        CheckIsGenerated();
    }


    public void Clear()
    {
        IGenerator.Clear();
        CheckIsGenerated();
    }

    public void Generate()
    {
        IGenerator.Generate();
        CheckIsGenerated();
    }

    public bool CheckIsComposite()
    {
        isComposite = IGenerator.IsComposite;
        return isComposite;
    }

    public bool CheckIsGenerated()
    {
        isGenerated = IGenerator.IsGenerated;
        foreach (IGeneratorProxy proxy in compositeIGeneratorProxies) proxy.CheckIsGenerated();
        return isGenerated;
    }

    public string CheckName()
    {
        name = IGenerator.Name;
        return name;
    }

    public IGenerator[] GetCompositeIGenerators() => compositeIGeneratorProxies.Select(p => p.IGenerator).ToArray();
}
