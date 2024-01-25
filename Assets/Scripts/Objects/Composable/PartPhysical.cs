using UnityEngine;

public class PartPhysical : Part
{
    [Header("Config")]
    [SerializeField] public float density = 1.0f;

    public Rigidbody2D RB { get; protected set; } = null;
    public GravityObject GRO { get; protected set; } = null;

    public override void InitPart(ComposableObject composable)
    {
        base.InitPart(composable);
        RB = gameObject.GetComponent<Rigidbody2D>();
        GRO = gameObject.GetComponent<GravityObject>();
        if (RB == null) RB = gameObject.AddComponent<Rigidbody2D>();
        if (GRO == null) GRO = gameObject.AddComponent<GravityObject>();
        RB.gravityScale = 0;
        GRO.IsEnabled = true;
        GRO.RB = RB;
        Composable.CL.isTrigger = false;
        InitMass(density);
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }

    public void InitMass(float density)
    {
        this.density = density;
        RB.useAutoMass = true;
        RB.useAutoMass = false;
        RB.mass *= density;
    }
}
