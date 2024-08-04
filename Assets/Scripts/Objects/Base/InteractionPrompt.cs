using UnityEngine;

public class InteractionPrompt : MonoBehaviour, IOrganiserChild
{
    public bool IsSet => interaction != null;
    public bool IsVisible => IsSet && (spriteRendererInput.enabled || spriteRendererIcon.enabled);
    public Transform Transform => transform;

    public float GetOrganiserChildHeight() => spriteRendererInput.size.y;

    public void SetInteraction(IInteractor interactor, Interaction interaction)
    {
        this.interactor = interactor;
        this.interaction = interaction;
        UpdateElements();
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRendererIcon;
    [SerializeField] private SpriteRenderer spriteRendererInput;
    [SerializeField] private SpriteRenderer spriteRendererToolOutline;
    [SerializeField] private SpriteRenderer spriteRendererTool;

    [Header("Config")]
    [SerializeField] private Color darkDisabledColour = new Color(0.32f, 0.32f, 0.32f);
    [SerializeField] private Color lightDisabledColour = new Color(0.53f, 0.53f, 0.53f);

    private IInteractor interactor;
    private Interaction interaction;

    private void Update()
    {
        UpdateElements();
    }

    private void UpdateElements()
    {
        if (IsSet)
        {
            bool canUseTool = interaction.CanUseTool(interactor);
            bool canInteract = interaction.CanInteract(interactor);

            // Handle icon sprite
            spriteRendererIcon.enabled = interaction.IconVisible;
            if (spriteRendererIcon.enabled)
            {
                spriteRendererIcon.sprite = AssetManager.GetSprite(
                    !interaction.IsEnabled ? ("int_disabled")
                    : interaction.IsActive ? ("int_" + interaction.IconSprite + "_active")
                    : ("int_" + interaction.IconSprite + "_inactive")
                );
                spriteRendererIcon.color = (canInteract || interaction.IsActive) ? Color.white : darkDisabledColour;
            }

            // Handle input sprite
            spriteRendererInput.enabled = interaction.InputVisible;
            if (spriteRendererInput.enabled)
            {
                spriteRendererInput.sprite = AssetManager.GetSprite(
                    interaction.IsActive ? ("int_" + interaction.RequiredInput.Name + "_active")
                    : ("int_" + interaction.RequiredInput.Name + "_inactive")
                );
            }
            spriteRendererInput.color = (canInteract || interaction.IsActive) ? Color.white : darkDisabledColour;

            // Handle tool sprites
            spriteRendererToolOutline.enabled = interaction.RequiredTool != ToolType.Any && interaction.ToolVisible;
            spriteRendererTool.enabled = spriteRendererToolOutline.enabled;
            if (spriteRendererToolOutline.enabled)
            {
                spriteRendererTool.sprite = AssetManager.GetSprite("int_tool_" + interaction.RequiredTool.ToString().ToLower());
                spriteRendererToolOutline.color = (canInteract || interaction.IsActive) ? Color.white : lightDisabledColour;
                spriteRendererTool.color = (canInteract || interaction.IsActive) ? Color.white : darkDisabledColour;
            }
        }

        // Not set so disable all elements
        else
        {
            spriteRendererIcon.enabled = false;
            spriteRendererInput.enabled = false;
            spriteRendererToolOutline.enabled = false;
            spriteRendererTool.enabled = false;
        }
    }
}
