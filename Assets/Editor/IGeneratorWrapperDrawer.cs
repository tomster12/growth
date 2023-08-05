
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


[CustomPropertyDrawer(typeof(IGeneratorWrapper))]
public class IGeneratorWrapperDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement inspector = new VisualElement();
        
        var button = new Button();
        button.text = "Press";
        button.style.marginBottom = 2;
        button.style.marginLeft = 5;
        button.style.marginRight = 5;
        button.style.marginTop = 2;
        inspector.Add(button);

        return inspector;
    }
}
