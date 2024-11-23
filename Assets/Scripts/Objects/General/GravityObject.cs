using System.Collections.Generic;
using UnityEngine;

public class GravityObject : MonoBehaviour
{
    public static HashSet<GravityObject> gravityObjects = new HashSet<GravityObject>();

    [Header("References")]
    [SerializeField] public Rigidbody2D RB;

    public bool IsKinematic = true;
    public bool UseRBSurface = false;
    public Vector2 Centre => transform.position;
    public Vector2 GravityDir { get; protected set; }

    public void AddForce(Vector2 force)
    {
        GravityDir = force;
        if (IsKinematic && RB.simulated) RB.AddForce(force);
    }

    private void Awake()
    {
        gravityObjects.Add(this);
    }

    private void OnDestroy()
    {
        gravityObjects.Remove(this);
    }
}
