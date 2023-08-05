
using System;
using UnityEngine;


[Serializable]
public class IGeneratorWrapper : UnityEngine.Object
{
    [SerializeField] private string generatorName;
    [SerializeField] private bool isGenerated = true;

    private IGenerator IGenerator;


    public IGeneratorWrapper(IGenerator IGenerator_)
    {
        IGenerator = IGenerator_;
        generatorName = IGenerator.GetName();
    }
}
