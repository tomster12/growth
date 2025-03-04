using UnityEngine;

public class PluckableStoneObject : CompositeObject
{
    public bool IsPlucked { get; private set; }

    public void SetPluckDir(Vector2 pluckDir)
    {
        this.pluckDir = pluckDir;
    }

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
        if (pluckDir.Equals(Vector2.zero))
        {
            Debug.LogWarning("PluckableStone does not have a popDir");
            World closestWorld = World.GetClosestWorldByCentre(Position);
            WorldSurfaceEdge closestEdge = closestWorld.GetClosestEdge(Position);
            pluckDir = (Position - closestEdge.centre).normalized;
        }

        // Initialize indicator
        partIndicatable.SetIcon(PartIndicatable.IconType.Resource);
        partIndicatable.SetOffsetDir(pluckDir);
    }

    [Header("Pluck Config")]
    [SerializeField] private float density = 2.0f;
    [SerializeField] private float pluckTimerMax = 2.0f;
    [SerializeField] private float pluckVelocity = 12.5f;
    [SerializeField] private GameObject pluckPsysPfb;
    [SerializeField] private CraftingIngredient craftingIngredient;

    private PartIndicatable partIndicatable;
    private PartPhysical partPhysical;
    private InteractionPluck interactionPluck;
    [SerializeField] private Vector2 pluckDir;

    private void OnPluck()
    {
        // Remove interaction
        GetPart<PartInteractable>().RemoveInteraction(interactionPluck);

        // Become physical and controllable
        partPhysical = AddPart<PartPhysical>();
        AddPart<PartControllable>();
        partPhysical.InitMass(density);

        // Move out of ground
        Transform.position += (Vector3)(pluckDir.normalized * CL.bounds.extents * 1.5f);

        // Pop in a direction (ignore mass)
        partPhysical.RB.AddForce(partPhysical.RB.mass * pluckVelocity * pluckDir.normalized, ForceMode2D.Impulse);

        // Produce particles
        GameObject particlesGO = Instantiate(pluckPsysPfb);
        particlesGO.transform.position = partPhysical.RB.transform.position;
        particlesGO.transform.up = pluckDir.normalized;
        GameLayers.SetLayer(particlesGO.transform, GameLayer.Particles);
        IsPlucked = true;

        // Add ingredient part and change indicator accordingly
        AddPart<PartCraftingIngredient>();
        GetPart<PartCraftingIngredient>().SetIngredient(craftingIngredient);
        partIndicatable.SetIcon(PartIndicatable.IconType.Ingredient);
        partIndicatable.SetOffsetGravity(partPhysical.GRO);
    }
}
