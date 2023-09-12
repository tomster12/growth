
using UnityEngine;


public class Prompt : MonoBehaviour, IOrganiserChild
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRendererInput;
    [SerializeField] private SpriteRenderer spriteRendererIcon;

    public bool IsSet => interaction != null;

    private PlayerInteractor.Interaction interaction;


    public bool GetVisible() => IsSet && interaction.IsEnabled;

    public Transform GetTransform() => transform;

    public float GetHeight() => spriteRendererInput.size.y;

    public void SetInteraction(PlayerInteractor.Interaction interaction)
    {
        this.interaction = interaction;
        UpdateElements();
    }


    private void Update()
    {
        UpdateElements();
    }

    private void UpdateElements()
    {
        // Enabled so update
        if (IsSet && interaction.IsEnabled)
        {
            spriteRendererInput.sprite = interaction.GetCurrentSpriteInput();
            spriteRendererIcon.sprite = interaction.GetCurrentSpriteIcon();
        }

        // Disabled so hide
        else
        {
            spriteRendererInput.enabled = false;
            spriteRendererIcon.enabled = false;
        }
    }
}
