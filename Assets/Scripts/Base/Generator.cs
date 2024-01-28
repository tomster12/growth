using System;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Generator : MonoBehaviour
{
    public virtual string Name => "N/A";
    public virtual Generator[] ComposedGenerators => new Generator[0];
    public bool IsComposite => ComposedGenerators.Length != 0;
    public bool IsGenerated { get => isGenerated; protected set => isGenerated = value; }

    public abstract void Generate();

    public abstract void Clear();

    public bool ContainsGenerator(Generator generator)
    {
        return IsComposite && (ComposedGenerators.Contains(generator) || ComposedGenerators.Any(cg => cg.ContainsGenerator(generator)));
    }

    // Required backing variables for drawer property binding
    [SerializeField] private Generator[] composedGenerators;
    [SerializeField] private bool isGenerated;

    private void OnValidate()
    {
        // Update backing variable so up to date for editor inspector binding
        composedGenerators = ComposedGenerators;
    }
}
