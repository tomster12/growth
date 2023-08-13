
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


[CustomEditor(typeof(GeneratorController))]
public class GeneratorControllerEditor : Editor
{
    private GeneratorController controller => (GeneratorController)target;
    private VisualTreeAsset xuml;


    public override VisualElement CreateInspectorGUI()
    {
        VisualElement inspector = new VisualElement();
        
        if (xuml == null) xuml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GeneratorControllerEditorXUML.uxml");
        if (xuml == null) return inspector;
        xuml.CloneTree(inspector);

        inspector.Q<Button>("ButtonFindGenerators").clicked += () => controller.FindGenerators();
        inspector.Q<Button>("ButtonGenerate").clicked += () => controller.Generate();
        inspector.Q<Button>("ButtonClear").clicked += () => controller.Clear();

        return inspector;
    }
}
