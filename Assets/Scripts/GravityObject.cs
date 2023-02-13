
using System.Collections.Generic;
using UnityEngine;


public class GravityObject : MonoBehaviour
{
    static public HashSet<GravityObject> gravityObjects = new HashSet<GravityObject>();

    [Header("References")]
    [SerializeField] private Rigidbody2D _rb;
    public Rigidbody2D rb => _rb;
    public Vector2 centre => rb.transform.position;


    private void Start()
    {
        // Add to global world
        gravityObjects.Add(this);
    }
}
