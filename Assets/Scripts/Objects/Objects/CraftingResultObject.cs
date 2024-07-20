using System.Collections.Generic;
using UnityEngine;

public class CraftingResultObject : CompositeObject
{
    public void SetRecipe(CraftingRecipe recipe)
    {
        this.recipe = recipe;
        if (recipe != null) spriteRenderer.sprite = recipe.result.ingredient.sprite;
        else spriteRenderer.sprite = AssetManager.GetSprite("null_recipe");

        // Update polygon collider to sprite physics shape
        List<Vector2> points = new List<Vector2>();
        spriteRenderer.sprite.GetPhysicsShape(0, points);
        polygonCollider.points = points.ToArray();
    }

    protected override void Awake()
    {
        base.Awake();
        CL.isTrigger = true;
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PolygonCollider2D polygonCollider => (PolygonCollider2D)CL;
    [SerializeField] private LineHelper lineHelper;

    private CraftingRecipe recipe;

    private void Update()
    {
        lineHelper.DrawCircle(transform.position, 0.75f, Color.white, 0.1f, LineFill.Dotted);
        lineHelper.repeatOffset = Time.time;
    }
}
