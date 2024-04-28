using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChildOrganiser : MonoBehaviour
{
    [SerializeField] public bool toCentreChildren = true;
    [SerializeField] public float gap = 0.5f;

    public void UpdateChildren()
    {
        IOrganiserChild[] activeChildren = children.Where(child => child.IsVisible).ToArray();

        float totalHeight = activeChildren.Sum(child => child.GetOrganiserChildHeight());

        float cumsum = 0.0f;
        for (int i = 0; i < activeChildren.Length; i++)
        {
            float dy = -totalHeight / 2.0f + cumsum;
            activeChildren[i].Transform.localPosition = Vector3.up * dy;
            cumsum += activeChildren[i].GetOrganiserChildHeight();
        }
    }

    public void Clear()
    {
        foreach (Transform child in transform) DestroyImmediate(child.gameObject);
        children.Clear();
    }

    public void AddChild(IOrganiserChild newChild)
    {
        children.Add(newChild);
        newChild.Transform.parent = transform;
    }

    private List<IOrganiserChild> children = new List<IOrganiserChild>();

    private void Update() => UpdateChildren();
}
