using UnityEngine;

public class PluckableStoneObject : CompositeObject
{
    public Vector2 PluckDir { get; set; }
    public bool IsPlucked { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        // Add parts
        AddPart<PartInteractable>();
        AddPart<PartHighlightable>();
        partIndicatable = AddPart<PartIndicatable>();

        // Initialize interaction
        interactionPluck = new InteractionPluck(this, OnPluck, pluckTimerMax);
        GetPart<PartInteractable>().AddInteraction(interactionPluck);
    }

    protected void Start()
    {
        // Set popDir if not already set
        if (PluckDir.Equals(Vector2.zero))
        {
            Debug.LogWarning("PluckableStone does not have a popDir");
            PluckDir = (Position - (Vector2)World.GetClosestWorld(Position).GetCentre()).normalized;
        }

        // Initialize indicator
        partIndicatable.SetIcon(PartIndicatable.IconType.Resource);
        partIndicatable.SetOffsetDir(PluckDir);
    }

    protected void Update()
    {
        if (IsPlucked && GetPart<PartIndicatable>().IsVisible)
        {
            GetPart<PartIndicatable>().OffsetDir = -GetPart<PartPhysical>().GRO.GravityDir;
        }
    }

    [Header("Pluck Config")]
    [SerializeField] private float density = 2.0f;
    [SerializeField] private float pluckTimerMax = 2.0f;
    [SerializeField] private float pluckVelocity = 12.5f;
    [SerializeField] private GameObject pluckPsysPfb;

    private PartIndicatable partIndicatable;
    private InteractionPluck interactionPluck;

    private void OnPluck()
    {
        // Remove interaction
        GetPart<PartInteractable>().RemoveInteraction(interactionPluck);

        // Become physical and controllable
        var physical = AddPart<PartPhysical>();
        AddPart<PartControllable>();
        physical.InitMass(density);

        // Move out of ground
        Transform.position += (Vector3)(PluckDir.normalized * CL.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        physical.RB.AddForce(physical.RB.mass * pluckVelocity * PluckDir.normalized, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = physical.RB.transform.position;
        particlesGO.transform.up = PluckDir.normalized;
        IsPlucked = true;

        // Add ingredient part and change indicator accordingly
        AddPart<PartIngredient>();
        partIndicatable.SetIcon(PartIndicatable.IconType.Ingredient);
        partIndicatable.OffsetFromWorld = true;
    }
}
