
using UnityEngine;
using System;
using System.Linq;


[Serializable]
public class IGeneratorProxy
{
    [SerializeField] private Component _IGenerator;
    [SerializeField] public string name;
    [SerializeField] public bool isEnabled;
    [SerializeField] public bool isGenerated;
    [SerializeField] public bool isComposite;
    [SerializeField] public IGeneratorProxy[] compositeIGeneratorProxies;

    public IGenerator IGenerator => (IGenerator)_IGenerator;


    public IGeneratorProxy(IGenerator IGenerator_, bool isEnabled_=true)
    {
        _IGenerator = (Component)IGenerator_;
        name = IGenerator.GetName();
        isEnabled = isEnabled_;
        isComposite = IGenerator.IsComposite();
        compositeIGeneratorProxies = IGenerator.GetCompositeIGenerators().Select(g => new IGeneratorProxy(g, false)).ToArray();
        IsGenerated();
    }


    public void Clear()
    {
        IGenerator.Clear();
        IsGenerated();
    }

    public void Generate()
    {
        IGenerator.Generate();
        IsGenerated();
    }

    public string GetName() => name;

    public bool IsGenerated()
    {
        isGenerated = IGenerator.IsGenerated();
        foreach (IGeneratorProxy proxy in compositeIGeneratorProxies) proxy.IsGenerated();
        return isGenerated;
    }

    public bool IsComposite() => isComposite;


    public IGenerator[] GetCompositeIGenerators() => compositeIGeneratorProxies.Select(p => p.IGenerator).ToArray();
}
