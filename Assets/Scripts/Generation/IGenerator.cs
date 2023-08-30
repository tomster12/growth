
public interface IGenerator
{
    bool IsGenerated { get; }
    bool IsComposite { get => false; }
    string Name { get; }

    void Clear();
    void Generate();
    IGenerator[] GetCompositeIGenerators() => new IGenerator[0];
}
