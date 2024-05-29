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
    [SerializeField] private Color toolOutlineDisabledColor = new Color(0.53f, 0.53f, 0.53f);
    [SerializeField] private Color toolDisabledColor = new Color(0.72f, 0.33f, 0.33f);

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
            string iconSprite = GetIconSprite(interaction, interactor);
            string inputSprite = GetInputSprite(interaction, interactor);
            string toolSprite = GetToolSprite(interaction, interactor);

            spriteRendererIcon.enabled = !string.IsNullOrEmpty(iconSprite);
            spriteRendererInput.enabled = !string.IsNullOrEmpty(inputSprite);
            spriteRendererToolOutline.enabled = !string.IsNullOrEmpty(toolSprite);
            spriteRendererTool.enabled = !string.IsNullOrEmpty(toolSprite);

            if (spriteRendererIcon.enabled) spriteRendererIcon.sprite = SpriteSet.GetSprite(iconSprite);
            if (spriteRendererInput.enabled) spriteRendererInput.sprite = SpriteSet.GetSprite(inputSprite);
            if (spriteRendererTool.enabled)
            {
                spriteRendererTool.sprite = SpriteSet.GetSprite(toolSprite);
                bool canUseTool = interaction.CanUseTool(interactor);
                spriteRendererToolOutline.color = canUseTool ? Color.white : toolOutlineDisabledColor;
                spriteRendererTool.color = canUseTool ? Color.white : toolDisabledColor;
            }
        }
        else
        {
            spriteRendererInput.enabled = false;
            spriteRendererIcon.enabled = false;
        }
    }

    public virtual string GetIconSprite(Interaction interaction, IInteractor interactor)
    {
        return !interaction.IsEnabled ? ("int_disabled")
            : interaction.IsActive ? ("int_" + interaction.IconSprite + "_active")
            : ("int_" + interaction.IconSprite + "_inactive");
    }

    public virtual string GetInputSprite(Interaction interaction, IInteractor interactor)
    {
        return !interaction.IsEnabled ? ("")
            : interaction.IsActive ? ("int_" + interaction.RequiredInput.Name + "_active")
            : !interaction.CanInteract(interactor) ? ("int_disabled")
            : ("int_" + interaction.RequiredInput.Name + "_inactive");
    }

    public virtual string GetToolSprite(Interaction interaction, IInteractor interactor)
    {
        return !interaction.IsEnabled ? ("")
            : interaction.RequiredTool == ToolType.None ? ""
            : ("int_tool_" + interaction.RequiredTool.ToString().ToLower());
    }
}
