
using UnityEngine;


public class ComponentHoverable : MonoBehaviour, IHoverable
{
    public static Color WHITE = new Color(1.0f, 1.0f, 1.0f);

    [Header("Hoverable References")]
    [SerializeField] private OutlineController outline;
    [SerializeField] private Collider2D hoverCollider;

    public bool canHover = true;
    public Color hoverColor = WHITE;
    public bool isHovered { get; private set; }


    public void SetHovered(bool isHovered)
    {
        if (this.isHovered == isHovered) return;
        if (!canHover && isHovered) return;

        // Update variables
        this.isHovered = isHovered;
        outline.enabled = this.isHovered;
    }


    public Bounds GetBounds() => hoverCollider.bounds;
    
    public GameObject GetGameObject() => gameObject;
}
