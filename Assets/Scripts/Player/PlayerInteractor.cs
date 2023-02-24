
using UnityEngine;


public class HoverInfo
{
    public bool isHovering;
    public IHoverable hoveredIHoverable;
    public GameObject hoveredGameObject;

    public void Hover(IHoverable newIHoverable)
    {
        // Not hovering new
        if (hoveredIHoverable == newIHoverable) return;

        // Unhover old
        if (hoveredIHoverable != null)
        {
            hoveredIHoverable.SetHovered(false);
            hoveredIHoverable = null;
            hoveredGameObject = null;
            isHovering = false;
        }

        // Hover new
        if (newIHoverable != null)
        {
            hoveredIHoverable = newIHoverable;
            hoveredGameObject = hoveredIHoverable.GetGameObject();
            hoveredIHoverable.SetHovered(true);
            isHovering = true;
        }
    }
}


public class PlayerInteractor : MonoBehaviour
{
    public Vector2 hoverPos { get; private set; }
    public HoverInfo hoverInfo { get; private set; } = new HoverInfo();


    private void Update()
    {
        // Calculate hover pos
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Raycast at mouse on all hoverables
        LayerMask mask = LayerMask.NameToLayer("Hoverable");
        RaycastHit2D[] hits = Physics2D.RaycastAll(hoverPos, Vector2.zero, Mathf.Infinity, 1 << mask);

        // Get new hovered object
        IHoverable newIHoverable = null;
        foreach (RaycastHit2D hit in hits)
        {
            IHoverable hitIHoverable = hit.transform.GetComponent<IHoverable>();
            if (hitIHoverable != null)
            {
                newIHoverable = hitIHoverable;
                break;
            }
        }

        // Hover new object
        hoverInfo.Hover(newIHoverable);
    }
}
