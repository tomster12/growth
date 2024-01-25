using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BendyNode
{
    public BendyNode(float length, float windAmplitude, float offsetAngle = 0.0f)
    {
        this.length = length;
        this.windAmplitude = windAmplitude;
        this.offsetAngle = offsetAngle;
    }

    public void Start()
    {
        Update();
    }

    public void Update()
    {
        // Set angle
        Vector2 windDir = GlobalWind.GetWind(transform.position + transform.up * length * 0.5f);
        float windAmount = Vector2.Dot(transform.right, windDir);
        float angle = offsetAngle + windAmount * windAmplitude;
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, angle);

        // Set position
        if (parent != null)
        {
            transform.parent = parent.transform;
            transform.localPosition = Vector2.up * parent.length;
        }

        // Recurse
        foreach (BendyNode child in children)
        {
            child.parent = this;
            child.Update();
        }
    }

    public void AddChild(BendyNode child)
    {
        children.Add(child);
        child.Update();
    }

    private BendyNode parent;
    [SerializeField] private Transform transform;
    [SerializeField] private float length = 1.0f;
    [SerializeField] private float windAmplitude = 10.0f;
    [SerializeField] private float offsetAngle = 0.0f;
    [SerializeField] private List<BendyNode> children = new List<BendyNode>();
};
