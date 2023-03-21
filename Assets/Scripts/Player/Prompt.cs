
using UnityEngine;


public class Prompt : MonoBehaviour, IOrganiserChild
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRendererInput;
    [SerializeField] private SpriteRenderer spriteRendererIcon;

    private Interaction interaction;
    public bool isSet => interaction != null;


    private void Update()
    {
        UpdateElements();
    }

    private void UpdateElements()
    {
        // Enabled so update
        if (isSet && interaction.isEnabled)
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


    public bool GetVisible() => isSet && interaction.isEnabled;

    public Transform GetTransform() => transform;

    public float GetHeight() => spriteRendererInput.size.y;


    public void SetInteraction(Interaction interaction)
    {
        this.interaction = interaction;
        UpdateElements();
    }

}
