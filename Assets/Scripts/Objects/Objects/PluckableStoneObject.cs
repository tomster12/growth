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
            PluckDir = (Position - World.GetClosestWorldCheap(Position).GetCentre()).normalized;
        }

        // Initialize indicator
        partIndicatable.SetIcon(PartIndicatable.IconType.Resource);
        partIndicatable.SetOffsetDir(PluckDir);
    }

    [Header("Pluck Config")]
    [SerializeField] private float density = 2.0f;
    [SerializeField] private float pluckTimerMax = 2.0f;
    [SerializeField] private float pluckVelocity = 12.5f;
    [SerializeField] private GameObject pluckPsysPfb;

    private PartIndicatable partIndicatable;
    private PartPhysical partPhysical;
    private InteractionPluck interactionPluck;

    private void OnPluck()
    {
        // Remove interaction
        GetPart<PartInteractable>().RemoveInteraction(interactionPluck);

        // Become physical and controllable
        partPhysical = AddPart<PartPhysical>();
        AddPart<PartControllable>();
        partPhysical.InitMass(density);

        // Move out of ground
        Transform.position += (Vector3)(PluckDir.normalized * CL.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        partPhysical.RB.AddForce(partPhysical.RB.mass * pluckVelocity * PluckDir.normalized, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = partPhysical.RB.transform.position;
        particlesGO.transform.up = PluckDir.normalized;
        IsPlucked = true;

        // Add ingredient part and change indicator accordingly
        AddPart<PartCraftingIngredient>();
        partIndicatable.SetIcon(PartIndicatable.IconType.Ingredient);
        partIndicatable.SetOffsetGravity(partPhysical.GRO);
    }
}
