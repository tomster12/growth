
using UnityEngine;
using System;


[Serializable]
public class IGeneratorProxy
{
    [SerializeField] public IGenerator IGenerator;
    [SerializeField] public string name;
    [SerializeField] public bool IsGenerated => IGenerator.IsGenerated();


    public IGeneratorProxy(IGenerator IGenerator_)
    {
        IGenerator = IGenerator_;
        name = IGenerator.GetName();
    }
}
