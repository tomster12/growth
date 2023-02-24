
using UnityEngine;


public interface IHoverable
{
    public void SetHovered(bool isHovered);

    public Bounds GetBounds();

    public GameObject GetGameObject();
}


public class HoverableObject : MonoBehaviour, IHoverable
{
    [Header("References")]
    [SerializeField] private OutlineController outline;
    [SerializeField] private Collider2D hoverCollider;

    public bool isHovered { get; private set; }


    public void SetHovered(bool isHovered)
    {
        if (this.isHovered == isHovered) return;

        // Update variables
        this.isHovered = isHovered;
        outline.enabled = this.isHovered;
    }

    public Bounds GetBounds()
    {
        return hoverCollider.bounds;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }
}
