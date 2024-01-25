using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(GeneratorController))]
public class GeneratorControllerEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();

        if (XUML == null) XUML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GeneratorControllerEditorXUML.uxml");
        if (XUML == null) return inspector;
        XUML.CloneTree(inspector);

        inspector.Q<Button>("ButtonFindGenerators").clicked += () => Controller.FindGenerators();
        inspector.Q<Button>("ButtonGenerate").clicked += () => Controller.Generate();
        inspector.Q<Button>("ButtonClear").clicked += () => Controller.Clear();
        inspector.Q<Button>("ButtonRandomize").clicked += () => Controller.RandomizeSeed();

        return inspector;
    }

    private VisualTreeAsset XUML;
    private GeneratorController Controller => (GeneratorController)target;
}
