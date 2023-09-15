
using UnityEngine;


public class PluckableStone : ComposableObject
{
    [Header("Pluck Config")]
    [SerializeField] private float  density= 2.0f;
    [SerializeField] private float pluckTimerMax = 2.0f;
    [SerializeField] private float pluckVelocity = 12.5f;
    [SerializeField] private GameObject pluckPsysPfb;

    public Vector2 PopDir { get; set; }
    public bool IsPlucked { get; private set; }
    
    private InteractionPluck interactionPluck;


    protected void Awake()
    {

        // Add parts
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
            PopDir  = (Position - (Vector2)World.GetClosestWorld(Position).GetCentre()).normalized;
        }
        
        // Initialize indicator
        GetPart<PartIndicatable>().Init("Indicator", PopDir);

        // Initialize interaction
        interactionPluck = new InteractionPluck(OnPluck, pluckTimerMax);
        GetPart<PartInteractable>().Interactions.Add(interactionPluck);
    }
    
    protected void Update()
    {
        if (IsPlucked && GetPart<PartIndicatable>().IsVisible)
        {
            GetPart<PartIndicatable>().TargetOffset = -GetPart<PartPhysical>().GRO.gravityDir;
        }
    }

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
        GetPart<PartPhysical>().RB.AddForce(PopDir.normalized * pluckVelocity * GetPart<PartPhysical>().RB.mass, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = GetPart<PartPhysical>().RB.transform.position;
        particlesGO.transform.up = PopDir.normalized;
        IsPlucked = true;
    }
}
