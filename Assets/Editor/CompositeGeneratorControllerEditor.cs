
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;


[CustomEditor(typeof(CompositeGeneratorController))]
public class CompositeGeneratorControllerEditor : Editor
{
    [SerializeField] private VisualTreeAsset xuml;
    private CompositeGeneratorController controller => (CompositeGeneratorController)target;


    public override VisualElement CreateInspectorGUI()
    {
        Debug.Log("CompositeGeneratorControllerInspector::CreateInspectorGUI() Start");
        VisualElement inspector = new VisualElement();
        xuml.CloneTree(inspector);

        Button buttonFindGenerators = inspector.Q<Button>("ButtonFindGenerators");
        Button buttonGenerate = inspector.Q<Button>("ButtonGenerate");
        buttonFindGenerators.clicked += OnButtonFindGenerators;
        buttonGenerate.clicked += OnButtonGenerate;

        return inspector;
    }

    public void OnButtonFindGenerators() => controller.FindGenerators();

    public void OnButtonGenerate() => controller.Generate();


    private VisualElement GenerateGeneratorView(IGenerator IGenerator)
    {
        VisualElement top = new VisualElement();

        Button button = new Button();
        Label buttonLabel = new Label(IGenerator.GetName());
        button.Add(buttonLabel);
        top.Add(button);

        return top;
    }
}
