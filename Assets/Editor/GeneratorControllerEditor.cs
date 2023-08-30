
using UnityEditor;
using UnityEngine.UIElements;


[CustomEditor(typeof(GeneratorController))]
public class GeneratorControllerEditor : Editor
{
    private GeneratorController Controller => (GeneratorController)target;
    private VisualTreeAsset xuml;


    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();
        
        if (xuml == null) xuml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GeneratorControllerEditorXUML.uxml");
        if (xuml == null) return inspector;
        xuml.CloneTree(inspector);

        inspector.Q<Button>("ButtonFindGenerators").clicked += () => Controller.FindGenerators();
        inspector.Q<Button>("ButtonGenerate").clicked += () => Controller.Generate();
        inspector.Q<Button>("ButtonClear").clicked += () => Controller.Clear();
        inspector.Q<Button>("ButtonRandomize").clicked += () => Controller.RandomizeSeed();

        return inspector;
    }
}
