
using UnityEngine;
using UnityEngine.Assertions;


public class PartControllable : Part
{
    [Header("Config")]
    [SerializeField] protected float controlDrag = 10.0f;
    [SerializeField] protected float controlAngularDrag = 1.0f;
    [SerializeField] protected float idleDrag = 0.5f;
    [SerializeField] protected float idleAngularDrag = 0.05f;

    public bool IsControlled { get; private set; } = false;
    public bool CanControl { get; private set; } = true;
    
    protected Vector2 controlPosition = Vector2.zero;
    protected float controlPositionForce = 1.0f;
    protected float controlAngle = 0.0f;
    protected float controlAngleForce = 1.0f;
    protected PartPhysical Physical => Composable.GetPart<PartPhysical>();


    public override void InitPart(ComposableObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartPhysical>();
        Assert.AreEqual(IsControlled, false);
        Physical.RB.drag = idleDrag;
        Physical.RB.angularDrag = idleAngularDrag;
    }

    public override void DeinitPart()
    {
        if (IsControlled) SetControlled(false);
        base.DeinitPart();
    }

    public void SetControlPosition(Vector3 controlPosition, float controlPositionForce)
    {
        this.controlPosition = controlPosition;
        this.controlPositionForce = controlPositionForce;
    }

    public void SetControlAngle(float controlAngle, float controlAngleForce)
    {
        this.controlAngle = controlAngle;
        this.controlAngleForce = controlAngleForce;
    }

    public void SetCanControl(bool canControl)
    {
        // Update variables
        this.CanControl = canControl;
    }

    public bool SetControlled(bool isControlled)
    {
        if (isControlled == this.IsControlled) return false;
        if (isControlled && !CanControl) return false;

        // Update variables
        this.IsControlled = isControlled;
        Physical.RB.drag = IsControlled ? controlDrag : idleDrag;
        Physical.RB.angularDrag = IsControlled ? controlAngularDrag : idleAngularDrag;
        Physical.GRO.IsEnabled = !IsControlled;
        return true;
    }

    public void StartControl() => SetControlled(true);

    public void StopControl() => SetControlled(false);


    private void FixedUpdate()
    {
        if (IsControlled)
        {
            // Move towards target
            Vector2 dir = controlPosition - (Vector2)Physical.RB.transform.position;
            Physical.RB.AddForce(dir * controlPositionForce);

            // Angle towards desired
            float angleDelta = Mathf.DeltaAngle(Physical.RB.transform.eulerAngles.z, controlAngle);
            Physical.RB.AddTorque(controlAngleForce * angleDelta / 360.0f);
        }
    }

    private void OnDrawGizmos()
    {
        if (IsControlled)
        {
            Vector3 controlDir = Quaternion.AngleAxis(controlAngle, Vector3.forward) * Vector2.up;
            Gizmos.DrawRay(Physical.RB.transform.position, controlDir);
            Gizmos.DrawRay(Physical.RB.transform.position, transform.up);
        }
    }
}
