
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Reflection;
using System.Collections;

[CustomPropertyDrawer(typeof(IGeneratorProxy))]
public class IGeneratorProxyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement inspector = new VisualElement();

        //var controller = property.serializedObject.targetObject as CompositeGeneratorController;
        //var controllerType = controller.GetType();
        //var proxyArray = controllerType.GetField("proxies").GetValue(controller) as IGeneratorProxy[];
        //var proxyArrayType = proxyArray.GetType();
        //Debug.Log(GetPropertyInstance(property));

        Debug.Log(property.FindPropertyRelative("name"));

        int a=;
        int b =;
        int c=;

        // Regex regex = new Regex(@"\[([0-9]+)\]");
        // Match match = regex.Match(property.propertyPath);
        // int index = int.Parse(match.Groups[1].Value);
        // Debug.Log(index);

        Button button = new Button();
        button.text = "Press";
        button.style.marginBottom = 2;
        button.style.marginLeft = 5;
        button.style.marginRight = 5;
        button.style.marginTop = 2;
        inspector.Add(button);

        return inspector;
    }

    public System.Object GetPropertyInstance(SerializedProperty property) {       

        string path = property.propertyPath;
        string[] fieldNames = path.Split('.');
        Debug.Log("Path: " + path);

        object curObject = property.serializedObject.targetObject;
        System.Type curType = curObject.GetType();

        for (int i = 0; i < fieldNames.Length; i++) {
            Debug.Log(":" + fieldNames[i]);
            FieldInfo field = curType.GetField(fieldNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) break;
            curObject = field.GetValue(curObject);
            curType = field.FieldType;
        }

        Debug.Log("--- Final ---");
        Debug.Log(curObject);
        return curObject;
    }
}
