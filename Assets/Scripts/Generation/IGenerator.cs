
public interface IGenerator
{
    void Clear();
    void Generate();
    bool IsGenerated();
    string GetName();
    bool IsComposite() => false;
    IGenerator[] GetCompositeIGenerators() => new IGenerator[0];
}
