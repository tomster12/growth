
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;


[CustomEditor(typeof(IGeneratorController))]
public class IGeneratorControllerEditor : Editor
{
    [SerializeField] private VisualTreeAsset xuml;
    private IGeneratorController controller => (IGeneratorController)target;


    public override VisualElement CreateInspectorGUI()
    {
        Debug.Log("IGeneratorControllerInspector::CreateInspectorGUI() Start");
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
}
