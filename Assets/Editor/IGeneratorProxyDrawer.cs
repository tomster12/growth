
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections;


[CustomPropertyDrawer(typeof(IGeneratorProxy))]
public class IGeneratorProxyDrawer : PropertyDrawer
{
    private VisualTreeAsset singleXuml;
    private VisualTreeAsset compositeXuml;
    
    
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement inspector = new VisualElement();

        IGeneratorProxy proxy = IGeneratorProxy(property);
        if (proxy == null || proxy.IGenerator == null) return inspector;

        if (proxy.CheckIsComposite())
        {
            if (compositeXuml == null) compositeXuml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/IGeneratorProxyCompositeDrawerXUML.uxml");
            if (compositeXuml == null) return inspector;
            compositeXuml.CloneTree(inspector);
        }
        else
        {
            if (singleXuml == null) singleXuml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/IGeneratorProxyDrawerXUML.uxml");
            if (singleXuml == null) return inspector;
            singleXuml.CloneTree(inspector);
        }

        Label labelName = inspector.Q<Label>("LabelName");
        labelName.text = proxy.CheckName();
        
        PropertyField indicator = inspector.Q<PropertyField>("Indicator");
        indicator.SetEnabled(false);
        indicator.RemoveFromClassList("unity-disabled");
        indicator.RegisterCallback<ChangeEvent<bool>>((evt) => {
            indicator.style.backgroundColor = evt.newValue
                ? new Color(0.13f, 0.87f, 0.13f)
                : new Color(0.87f, 0.13f, 0.13f);
        });
        
        inspector.Q<Button>("ButtonGenerate").clicked += () => proxy.Generate();
        inspector.Q<Button>("ButtonClear").clicked += () => proxy.Clear();

        return inspector;
    }


    private IGeneratorProxy IGeneratorProxy(SerializedProperty property)
    {
        var split = property.propertyPath.Split(".");
        object currentObject = property.serializedObject.targetObject;
        for (int i = 0; i < split.Length; i++)
        {
            if (split[i] == "Array") currentObject = ((IList)currentObject)[int.Parse(split[++i].Split('[', ']')[1])];
            else currentObject = currentObject.GetType().GetField(split[i]).GetValue(currentObject);
        }
        return (IGeneratorProxy)currentObject;
    }
}
