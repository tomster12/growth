using UnityEngine;
using UnityEngine.Assertions;

public class PartPhysical : Part
{
    [Header("Config")]
    [SerializeField] public float density = 1.0f;

    public Rigidbody2D RB { get; protected set; } = null;
    public GravityObject GRO { get; protected set; } = null;
    public bool IsEnabled { get; private set; } = true;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        RB = Composable.GameObject.GetComponent<Rigidbody2D>();
        GRO = Composable.GameObject.GetComponent<GravityObject>();
        if (RB == null) RB = Composable.GameObject.AddComponent<Rigidbody2D>();
        if (GRO == null) GRO = Composable.GameObject.AddComponent<GravityObject>();
        RB.gravityScale = 0;
        GRO.IsKinematic = true;
        GRO.RB = RB;
        Composable.CL.isTrigger = false;
        InitMass(density);
    }

    public void InitMass(float density)
    {
        this.density = density;
        RB.useAutoMass = true;
        RB.useAutoMass = false;
        RB.mass *= density;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        RB.simulated = enabled;
        RB.velocity = Vector2.zero;
        RB.angularVelocity = 0;
        GRO.IsKinematic = enabled;
    }
}
