using System.Collections.Generic;
using UnityEngine;

public class GravityObject : MonoBehaviour
{
    public static HashSet<GravityObject> gravityObjects = new HashSet<GravityObject>();

    [Header("References")]
    [SerializeField] public Rigidbody2D RB;

    public Vector2 Centre => RB.position;
    public bool IsEnabled { get; set; } = true;
    public Vector2 GravityDir { get; protected set; }

    public void AddForce(Vector2 force)
    {
        GravityDir = force;
        if (IsEnabled && RB.simulated) RB.AddForce(force);
    }

    private void Awake()
    {
        gravityObjects.Add(this);
    }
}
