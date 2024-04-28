using UnityEngine;

public class InteractionPrompt : MonoBehaviour, IOrganiserChild
{
    public bool IsSet => interaction != null;
    public bool IsVisible => IsSet && interaction.IsEnabled;
    public Transform Transform => transform;

    public float GetOrganiserChildHeight() => spriteRendererInput.size.y;

    public void SetInteraction(Interaction interaction)
    {
        this.interaction = interaction;
        UpdateElements();
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRendererInput;
    [SerializeField] private SpriteRenderer spriteRendererIcon;
    private Interaction interaction;

    private void Update()
    {
        UpdateElements();
    }

    private void UpdateElements()
    {
        // Enabled so update
        if (IsSet && interaction.IsEnabled)
        {
            spriteRendererInput.enabled = interaction.InputSprite != "";
            spriteRendererIcon.enabled = interaction.IconSprite != "";
            spriteRendererInput.sprite = SpriteSet.GetSprite(interaction.InputSprite);
            spriteRendererIcon.sprite = SpriteSet.GetSprite(interaction.IconSprite);
        }

        // Disabled so hide
        else
        {
            spriteRendererInput.enabled = false;
            spriteRendererIcon.enabled = false;
        }
    }
}
