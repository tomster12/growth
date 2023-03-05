
using System.Collections.Generic;
using UnityEngine;


public class GravityObject : MonoBehaviour
{
    static public HashSet<GravityObject> gravityObjects = new HashSet<GravityObject>();

    [Header("References")]
    [SerializeField] public Rigidbody2D rb;
    public Vector2 centre => rb.transform.position;
    
    public bool isEnabled = true;


    private void Start()
    {
        // Add to global world
        gravityObjects.Add(this);
    }
}
