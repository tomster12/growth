using System.Collections.Generic;
using UnityEngine;
using System;

public class CraftingResultObject : CompositeObject
{
    public Action OnClick = delegate { };

    public void SetRecipe(CraftingRecipe recipe)
    {
        if (recipe == this.recipe) return;
        this.recipe = recipe;

        // Update sprite
        if (recipe != null) spriteRenderer.sprite = recipe.result.ingredient.sprite;
        else spriteRenderer.sprite = AssetManager.GetSprite("null_recipe");

        // Update polygon collider to sprite physics shape
        List<Vector2> points = new List<Vector2>();
        spriteRenderer.sprite.GetPhysicsShape(0, points);
        polygonCollider.points = points.ToArray();
    }

    public void CreateResult()
    {
        if (recipe == null) return;
        CraftingIngredient result = recipe.result.ingredient;
        GameObject resultGO = Instantiate(result.pfb, transform.position, Quaternion.identity);
        GameLayers.SetLayer(resultGO.transform, GameLayer.Foreground);
        SetRecipe(null);
    }

    protected override void Awake()
    {
        base.Awake();

        // Add parts
        AddPart<PartHighlightable>();
        partInteractable = AddPart<PartInteractable>();

        // Initialize interaction
        interactionCraft = new InteractionClick("Craft", "", OnInteract);
        interactionCraft.SetVisibility(false, false, true, false);
        partInteractable.AddInteraction(interactionCraft);

        CL.isTrigger = true;
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PolygonCollider2D polygonCollider => (PolygonCollider2D)CL;
    [SerializeField] private LineHelper lineHelper;

    [Header("Config")]
    [SerializeField] public Color noCraftColor = new Color(210, 210, 210);
    [SerializeField] public Color craftColor = Color.white;
    [SerializeField] public float noCraftOffsetSpeed = 0.7f;
    [SerializeField] public float craftOffsetSpeed = 1.2f;
    [SerializeField] public float repeatMult = 1.6f;

    private PartInteractable partInteractable;
    private InteractionClick interactionCraft;
    private CraftingRecipe recipe;
    private float repeatOffset;

    private void Update()
    {
        // Update interaction
        interactionCraft.IsEnabled = recipe != null;

        // Update line helper
        repeatOffset += (recipe == null ? noCraftOffsetSpeed : craftOffsetSpeed) * Time.deltaTime;
        Color color = recipe == null ? noCraftColor : craftColor;
        lineHelper.DrawCircle(transform.position, 0.75f, color, 0.1f, LineFill.Dotted);
        lineHelper.repeatOffset = repeatOffset;
        lineHelper.repeatMult = repeatMult;
    }

    private void OnInteract() => OnClick();
}
