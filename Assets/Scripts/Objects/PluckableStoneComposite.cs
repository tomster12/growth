using UnityEngine;

public class PluckableStoneComposite : CompositeObject
{
    public Vector2 PopDir { get; set; }
    public bool IsPlucked { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        AddPart<PartInteractable>();
        AddPart<PartHighlightable>();
        partIndicatable = AddPart<PartIndicatable>();
    }

    protected void Start()
    {
        // Set popDir
        if (PopDir.Equals(Vector2.zero))
        {
            Debug.LogWarning("PluckableStone does not have a popDir");
            PopDir = (Position - (Vector2)World.GetClosestWorld(Position).GetCentre()).normalized;
        }

        // Initialize indicator
        partIndicatable.SetIcon(PartIndicatable.IconType.Resource);
        partIndicatable.SetOffset(PopDir);

        // Initialize interaction
        interactionPluck = new InteractionPluck(this, OnPluck, pluckTimerMax);
        GetPart<PartInteractable>().Interactions.Add(interactionPluck);
    }

    protected void Update()
    {
        if (IsPlucked && GetPart<PartIndicatable>().IsVisible)
        {
            GetPart<PartIndicatable>().TargetOffset = -GetPart<PartPhysical>().GRO.GravityDir;
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
        // Become physical and controllable
        var physical = AddPart<PartPhysical>();
        var controllable = AddPart<PartControllable>();
        physical.InitMass(density);

        // Move out of ground
        physical.RB.transform.position += (Vector3)(PopDir.normalized * CL.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        physical.RB.AddForce(physical.RB.mass * pluckVelocity * PopDir.normalized, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = physical.RB.transform.position;
        particlesGO.transform.up = PopDir.normalized;
        IsPlucked = true;

        // Add ingredient part and change indicator accordingly
        AddPart<PartIngredient>();
        partIndicatable.SetIcon(PartIndicatable.IconType.Ingredient);
    }
}
