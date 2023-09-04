
using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class BendyNode
{
    private BendyNode parent;
    [SerializeField] private Transform transform;
    [SerializeField] private float length;
    [SerializeField] private NoiseData noiseData;
    [SerializeField] private float offsetAngle;
    [SerializeField] private List<BendyNode> children = new List<BendyNode>();


    public BendyNode(float length, NoiseData noiseData, float offsetAngle=0.0f)
    {
        this.length = length;
        this.noiseData = noiseData;
        this.offsetAngle = offsetAngle;
    }


    public void Awake()
    {
        Update();
    }

    public void Update()
    {
        // Set angle
        float angle = offsetAngle + this.noiseData.GetNoise(transform.position + Vector3.one * Time.time);
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
};
