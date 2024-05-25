using UnityEngine;
using UnityEngine.Assertions;

public class PartControllable : Part
{
    public bool IsControlled { get; private set; } = false;
    public bool CanControl => canControl && !IsControlled;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartPhysical>();
        Physical.RB.drag = idleDrag;
        Physical.RB.angularDrag = idleAngularDrag;
    }

    public override void DeinitPart()
    {
        if (IsControlled) StopControlling();
        base.DeinitPart();
    }

    public void SetControlPosition(Vector3 controlPosition, float controlPositionForce = 1.0f)
    {
        this.controlPosition = controlPosition;
        this.controlPositionForce = controlPositionForce;
    }

    public void SetControlAngle(float controlAngle, float controlAngleForce = 1.0f)
    {
        this.controlAngle = controlAngle;
        this.controlAngleForce = controlAngleForce;
    }

    public bool StartControlling()
    {
        if (!CanControl)
        {
            Debug.LogWarning("Trying to start controlling uncontrollable part");
            return false;
        }

        // Set variables
        IsControlled = true;
        Physical.RB.drag = controlDrag;
        Physical.RB.angularDrag = controlAngularDrag;
        Physical.GRO.IsEnabled = false;

        return true;
    }

    public bool StopControlling()
    {
        if (!IsControlled)
        {
            Debug.LogWarning("Trying to stop controlling uncontrolled part");
            return false;
        }

        // Reset variables
        IsControlled = false;
        Physical.RB.drag = idleDrag;
        Physical.RB.angularDrag = idleAngularDrag;
        Physical.GRO.IsEnabled = true;

        return true;
    }

    public void SetCanControl(bool canControl) => this.canControl = canControl;

    public void SetToSnap(bool toSnap) => this.toSnap = toSnap;

    [Header("Config")]
    [SerializeField] private float controlDrag = 10.0f;
    [SerializeField] private float controlAngularDrag = 1.0f;
    [SerializeField] private float idleDrag = 0.5f;
    [SerializeField] private float idleAngularDrag = 0.05f;

    private Vector2 controlPosition = Vector2.zero;
    private float controlPositionForce = 1.0f;
    private float controlAngle = 0.0f;
    private float controlAngleForce = 1.0f;
    private bool canControl = true;
    private bool toSnap = false;
    private PartPhysical Physical => Composable.GetPart<PartPhysical>();

    private void FixedUpdate()
    {
        if (!IsControlled) return;

        if (toSnap)
        {
            Physical.RB.position = controlPosition;
            Physical.RB.rotation = controlAngle;
        }
        else
        {
            // Angle towards desired
            float angleDelta = Mathf.DeltaAngle(Physical.transform.eulerAngles.z, controlAngle);
            Physical.RB.AddTorque(controlAngleForce * angleDelta / 360.0f);

            // Move towards target (purposefully not normalized)
            Vector2 dir = controlPosition - Physical.RB.position;
            Physical.RB.AddForce(dir * controlPositionForce);
        }
    }

    private void OnDrawGizmos()
    {
        if (IsControlled)
        {
            Vector3 controlDir = Quaternion.AngleAxis(controlAngle, Vector3.forward) * Vector2.up;
            Gizmos.DrawRay(Physical.RB.position, controlDir);
            Gizmos.DrawRay(Physical.RB.position, transform.up);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(controlPosition, 0.1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(Physical.RB.position, 0.1f);
        }
    }
}
