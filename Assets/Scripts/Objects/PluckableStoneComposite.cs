using UnityEngine;

public class PluckableStoneComposite : CompositeObject
{
    public Vector2 PopDir { get; set; }
    public bool IsPlucked { get; private set; }

    protected void Awake()
    {
        AddPart<PartInteractable>();
        AddPart<PartHighlightable>();
        AddPart<PartIndicatable>();
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
        GetPart<PartIndicatable>().Init("indicator", PopDir);

        // Initialize interaction
        interactionPluck = new InteractionPluck(OnPluck, pluckTimerMax);
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
    private InteractionPluck interactionPluck;

    private void OnPluck()
    {
        // Become physical and controllable
        AddPart<PartPhysical>();
        AddPart<PartControllable>();
        GetPart<PartControllable>().SetCanControl(true);
        GetPart<PartPhysical>().InitMass(density);

        // Move out of ground
        GetPart<PartPhysical>().RB.transform.position += (Vector3)(PopDir.normalized * CL.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        GetPart<PartPhysical>().RB.AddForce(GetPart<PartPhysical>().RB.mass * pluckVelocity * PopDir.normalized, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = GetPart<PartPhysical>().RB.transform.position;
        particlesGO.transform.up = PopDir.normalized;
        IsPlucked = true;
    }
}
