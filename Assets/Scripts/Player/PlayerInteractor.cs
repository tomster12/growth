
using UnityEngine;


public class HoverInfo
{
    public bool isHovering;
    public IHoverable cursorHoveredIHoverable;
    public GameObject cursorHoveredGameObject;

    public void Hover(IHoverable newIHoverable)
    {
        // Not cursorHovering new
        if (cursorHoveredIHoverable == newIHoverable) return;

        // UncursorHover old
        if (cursorHoveredIHoverable != null)
        {
            cursorHoveredIHoverable.SetHovered(false);
            cursorHoveredIHoverable = null;
            cursorHoveredGameObject = null;
            isHovering = false;
        }

        // Hover new
        if (newIHoverable != null)
        {
            cursorHoveredIHoverable = newIHoverable;
            cursorHoveredGameObject = cursorHoveredIHoverable.GetGameObject();
            cursorHoveredIHoverable.SetHovered(true);
            isHovering = true;
        }
    }
}


public class PlayerInteractor : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer cursorCornerTL, cursorCornerTR, cursorCornerBL, cursorCornerBR;

    [Header("Config")]
    [SerializeField] private Color cursorIdleColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private float cursorIdleDistance = 0.75f;
    [SerializeField] private float cursorIdleMovementSpeed = 50.0f;
    [SerializeField] private Color cursorHoverColor = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private float cursorHoverMovementSpeed = 20.0f;
    [SerializeField] private float cursorHoverGap = 0.2f;
    [SerializeField] private float cursorColorLerpSpeed = 3.0f;

    public Vector2 hoverPos { get; private set; }
    public HoverInfo hoverInfo { get; private set; } = new HoverInfo();


    private void Start()
    {
        Focus();

        // Move cursor to mouse pos immediately
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = hoverPos;
    }


    private void Update()
{
    // Focus on click window
    if (Input.GetMouseButtonDown(0)) Focus();

    // Main update
    UpdateHover();
}

    private void FixedUpdate()
    {
        // Main update
        UpdateCursor();
    }

    private void UpdateHover()
    {
        // Calculate cursorHover pos
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Raycast at mouse on all cursorHoverables
        RaycastHit2D[] hits = Physics2D.RaycastAll(hoverPos, Vector2.zero);

        // Get new cursorHovered object
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

    private void UpdateCursor()
    {
    // Surround cursorHover object
    if (hoverInfo.isHovering)
    {
        Bounds b = hoverInfo.cursorHoveredIHoverable.GetBounds();

        // Move to centre
        Vector2 targetPos = b.center;
        cursorContainer.position = Vector2.Lerp(cursorContainer.position, targetPos, Time.deltaTime * cursorHoverMovementSpeed);

        // Surround with cursorCorners
        cursorCornerTL.transform.position = Vector2.Lerp(cursorCornerTL.transform.position, new Vector2(b.center.x - b.extents.x - cursorHoverGap, b.center.y + b.extents.y + cursorHoverGap), Time.deltaTime * cursorHoverMovementSpeed);
        cursorCornerTR.transform.position = Vector2.Lerp(cursorCornerTR.transform.position, new Vector2(b.center.x + b.extents.x + cursorHoverGap, b.center.y + b.extents.y + cursorHoverGap), Time.deltaTime * cursorHoverMovementSpeed);
        cursorCornerBL.transform.position = Vector2.Lerp(cursorCornerBL.transform.position, new Vector2(b.center.x - b.extents.x - cursorHoverGap, b.center.y - b.extents.y - cursorHoverGap), Time.deltaTime * cursorHoverMovementSpeed);
        cursorCornerBR.transform.position = Vector2.Lerp(cursorCornerBR.transform.position, new Vector2(b.center.x + b.extents.x + cursorHoverGap, b.center.y - b.extents.y - cursorHoverGap), Time.deltaTime * cursorHoverMovementSpeed);

        // Set colours
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
    }

    // Is idling
    else
    {
        // Move to mouse
        cursorContainer.position = hoverPos;

        // Spread out cursorCorners
        cursorCornerTL.transform.localPosition = Vector2.Lerp(cursorCornerTL.transform.localPosition, new Vector2(-cursorIdleDistance, cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);
        cursorCornerTR.transform.localPosition = Vector2.Lerp(cursorCornerTR.transform.localPosition, new Vector2(cursorIdleDistance, cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);
        cursorCornerBL.transform.localPosition = Vector2.Lerp(cursorCornerBL.transform.localPosition, new Vector2(-cursorIdleDistance, -cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);
        cursorCornerBR.transform.localPosition = Vector2.Lerp(cursorCornerBR.transform.localPosition, new Vector2(cursorIdleDistance, -cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);

        // Set colours
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorIdleColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorIdleColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorIdleColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorIdleColor, Time.deltaTime * cursorColorLerpSpeed);
    }
}


    void Focus()
    {
        Cursor.visible = false;
    }
}
