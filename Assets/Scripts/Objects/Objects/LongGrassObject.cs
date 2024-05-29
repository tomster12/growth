using UnityEngine;

public class LongGrassObject : CompositeObject
{
    public bool IsCut { get; private set; }

    [ContextMenu("Update Collider")]
    public void UpdateCollider()
    {
        // Fit box collider size and position to sprite considering sprite pivot
        boxCollider.size = spriteRenderer.sprite.bounds.size;
        boxCollider.offset = spriteRenderer.sprite.bounds.center;
    }

    protected override void Awake()
    {
        base.Awake();

        // Add parts
        AddPart<PartInteractable>();
        partHighlightable = AddPart<PartHighlightable>();
        partIndicatable = AddPart<PartIndicatable>();

        // Initialize interaction
        interactionCut = new InteractionCut(OnCut);
        GetPart<PartInteractable>().AddInteraction(interactionCut);

        // Update collider once spawned
        GrassFeature grassFeature = GetComponent<GrassFeature>();
    }

    protected void Start()
    {
        // Initialize indicator
        partIndicatable.SetIcon(PartIndicatable.IconType.Resource);
        partIndicatable.SetOffsetDir(Transform.up);
    }

    [Header("References")]
    [SerializeField] private GameObject grassIngredientPfb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Sprite cutSprite;

    private PartIndicatable partIndicatable;
    private PartHighlightable partHighlightable;
    private InteractionCut interactionCut;

    private void OnCut()
    {
        // Change sprite
        spriteRenderer.sprite = cutSprite;

        // Spawn ingredient
        Instantiate(grassIngredientPfb, Transform.position, Quaternion.identity);

        // Disable highlight and indicate
        partHighlightable.SetCanHighlight(false);
        partIndicatable.ToHide = true;

        // Update size of collider
        UpdateCollider();

        // Set variables
        IsCut = true;
    }
}
