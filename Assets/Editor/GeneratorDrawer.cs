using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Generator))]
public class GeneratorDrawer : PropertyDrawer
{
    // https://forum.unity.com/threads/registercallback-never-fires-for-propertyfields-if-a-custompropertydrawer-exists-in-your-project.1212732/
    // https://www.google.com/search?client=firefox-b-d&q=PropertyField+in+PropertyDrawer+RegisterCallback+not+working

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement inspector = new VisualElement();

        generator ??= (Generator)property.objectReferenceValue;
        if (generator == null) return inspector;
        generatorSO ??= new SerializedObject(generator);

        string path = generator.IsComposite ? "GeneratorCompositeDrawerXUML" : "GeneratorDrawerXUML";
        compositeXUML ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/" + path + ".uxml");
        if (compositeXUML == null) return inspector;
        compositeXUML.CloneTree(inspector);

        Label labelName = inspector.Q<Label>("LabelName");
        labelName.text = generator.Name;

        PropertyField indicator = inspector.Q<PropertyField>("Indicator");
        indicator.BindProperty(generatorSO.FindProperty("isGenerated"));
        indicator.RegisterCallback<ChangeEvent<bool>>((evt) =>
        {
            indicator.style.backgroundColor = evt.newValue
                ? new Color(0.13f, 0.87f, 0.13f)
                : new Color(0.87f, 0.13f, 0.13f);
        });

        if (generator.IsComposite)
        {
            ListView listView = inspector.Q<ListView>("ComposedGenerators");
            listView.BindProperty(generatorSO.FindProperty("composedGenerators"));
        }

        inspector.Q<Button>("ButtonGenerate").clicked += () => generator.Generate();
        inspector.Q<Button>("ButtonClear").clicked += () => generator.Clear();

        return inspector;
    }

    private Generator generator;
    private SerializedObject generatorSO;
    private VisualTreeAsset singleXUML;
    private VisualTreeAsset compositeXUML;
}
