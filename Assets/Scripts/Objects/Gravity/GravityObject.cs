
using System.Collections.Generic;
using UnityEngine;


public class GravityObject : MonoBehaviour
{
    static public HashSet<GravityObject> gravityObjects = new HashSet<GravityObject>();

    [Header("References")]
    [SerializeField] public Rigidbody2D RB;

    public Vector2 Centre => RB.transform.position;
    public bool IsEnabled { get; set; } = true;
    public Vector2 gravityDir { get; protected set; }


    public void AddForce(Vector2 force)
    {
        RB.AddForce(force);
        gravityDir = force;
    }


    private void Start()
    {
        gravityObjects.Add(this);
    }
}
