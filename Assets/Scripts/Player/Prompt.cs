
using UnityEngine;


public class Prompt : MonoBehaviour, IOrganiserChild
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteSet spriteSet;

    private bool isSet;
    private Interaction interaction;
    private Sprite spriteCross;
    private Sprite spriteUp;
    private Sprite spriteDown;


    public void SetInteraction(Interaction interaction)
    {
        this.interaction = interaction;
        spriteCross = spriteSet.GetSymbolSprite("cross");
        spriteUp = spriteSet.GetInputSprite(interaction.input.name, "up");
        spriteDown = spriteSet.GetInputSprite(interaction.input.name, "down");
        isSet = true;
        UpdateElements();
    }


    private void Update()
    {
        if (isSet) UpdateElements();
    }


    private void UpdateElements()
    {
        // Update sprite based on interaction state
        if (interaction.isEnabled)
        {
            if (!interaction.isBlocked)
            {
                if (interaction.isActive) spriteRenderer.sprite = spriteDown;
                else spriteRenderer.sprite = spriteUp;
            }
            else spriteRenderer.sprite = spriteCross;
        }
        else spriteRenderer.sprite = null;
    }


    public bool GetVisible() => isSet && interaction.isEnabled;
    public Transform GetTransform() => transform;
    public float GetHeight() => spriteRenderer.size.y;
}
