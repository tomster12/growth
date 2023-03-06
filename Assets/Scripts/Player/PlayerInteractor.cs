
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;




public class PlayerInteractor : MonoBehaviour
{
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);
    public static PlayerInteractor instance;


    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerLegs playerLegs;
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer cursorCornerTL, cursorCornerTR, cursorCornerBL, cursorCornerBR;
    [SerializeField] private SpriteRenderer centreIndicator;

    [Header("Config")]
    [SerializeField] private Color cursorIdleColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private float cursorIdleDistance = 0.75f;
    [SerializeField] private float cursorIdleMovementSpeed = 50.0f;
    [SerializeField] private Color cursorHoverColor = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private float cursorHoverMovementSpeed = 20.0f;
    [SerializeField] private float cursorHoverGap = 0.2f;
    [SerializeField] private float cursorColorLerpSpeed = 3.0f;
    [SerializeField] private float controlForce = 25.0f;
    [SerializeField] private float hoverDistance = 20.0f;
    [SerializeField] private float controlDistance = 10.0f;

    public Vector2 hoverPos { get; private set; }
    public WorldObject hoveredWO { get; private set; }
    public bool isControlling { get; private set; }
    private List<Interaction> hoveredInteractions;
    private LineRenderer hoverDistanceRenderer;


    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Focus();

        // Initiaize hover distance renderer
        hoverDistanceRenderer = gameObject.AddComponent<LineRenderer>();

        // Move cursor to mouse pos immediately
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = new Vector2(hoverPos.x, hoverPos.y);

        // Hide centre indicator
        centreIndicator.gameObject.SetActive(false);
    }


    private void Update()
    {
        // Focus on click window
        if (Input.GetMouseButtonDown(0)) Focus();

        // Main update
        UpdateHover();
        UpdateControlled();

        // Update interactions
        if (hoveredInteractions != null)
        {
            foreach (Interaction interaction in hoveredInteractions) interaction.TryInteract();
        }
    }
    

    private void LateUpdate() => UpdateCursor();


    private void UpdateHover()
    {
        // Calculate cursorHover pos
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Raycast at mouse on all cursorHoverables
        RaycastHit2D[] hits = Physics2D.RaycastAll(hoverPos, Vector2.zero);

        // Not controlling so could hover
        if (!isControlling)
        {
            WorldObject newHoveredWO = null;

            // - Mouse close enough to hover new
            if ((hoverPos - (Vector2)playerController.transform.position).magnitude < hoverDistance)
            {
                foreach (RaycastHit2D hit in hits)
                {
                    WorldObject hitWO = hit.transform.GetComponent<WorldObject>();
                    if (hitWO != null) { newHoveredWO = hitWO; break; }
                }
            }

            // - Overwrite current hover
            if (newHoveredWO != hoveredWO)
            {
                if (hoveredWO != null) hoveredWO.SetHovered(false);
                hoveredWO = newHoveredWO;
                if (hoveredWO != null)
                {
                    hoveredWO.SetHovered(true);
                    hoveredInteractions = hoveredWO.GetInteractions();
                }
                else hoveredInteractions = null;
            }
        }
    }

    private void UpdateControlled()
    {
        // Currently controlling
        if (isControlling)
        {
            // - Set control position
            Vector2 limitedHoverDir = Vector2.ClampMagnitude(hoverPos - (Vector2)playerController.transform.position, controlDistance);
            Vector2 limitedHoverPos = (Vector2)playerController.transform.position + limitedHoverDir;
            hoveredWO.controlPosition = limitedHoverPos;
            hoveredWO.controlForce = controlForce;

            // - Point leg
            playerLegs.isPointing = true;
            playerLegs.pointingLeg = 2;
            playerLegs.pointingPos = hoveredWO.transform.position;

            // - Drop object
            if (Input.GetMouseButtonDown(0))
            {
                hoveredWO.SetControlled(false);
                isControlling = false;
            }
        }

        // Not controlling
        else
        {
            // - Stop pointing legs
            playerLegs.isPointing = false;

            // - Hovering and clicked
            if (hoveredWO != null && hoveredWO.canControl && Input.GetMouseButtonDown(0))
            {
                // - Try set controlled
                if (hoveredWO.SetControlled(true)) isControlling = true;
            }
        }
    }

    private void UpdateCursor()
    {
        // Enable centre indicator optionally
        centreIndicator.gameObject.SetActive(isControlling);

        // Surround cursorHover object
        if (hoveredWO != null)
        {
            Bounds b = hoveredWO.GetHoverBounds();

            // Move to centre
            Vector2 targetPos = b.center;
            Vector2 targetTLPos = new Vector2(b.center.x - b.extents.x - cursorHoverGap, b.center.y + b.extents.y + cursorHoverGap);
            Vector2 targetTRPos = new Vector2(b.center.x + b.extents.x + cursorHoverGap, b.center.y + b.extents.y + cursorHoverGap);
            Vector2 targetBLPos = new Vector2(b.center.x - b.extents.x - cursorHoverGap, b.center.y - b.extents.y - cursorHoverGap);
            Vector2 targetBRPos = new Vector2(b.center.x + b.extents.x + cursorHoverGap, b.center.y - b.extents.y - cursorHoverGap);
            if (isControlling)
            {
                // Set to controlling pos
                cursorContainer.position = targetPos;
                cursorCornerTL.transform.position = targetTLPos;
                cursorCornerTR.transform.position = targetTRPos;
                cursorCornerBL.transform.position = targetBLPos;
                cursorCornerBR.transform.position = targetBRPos;

                // Set centre indicator to true mouse pos
                centreIndicator.transform.position = new Vector2(hoverPos.x, hoverPos.y);
            }
            else
            {
                // Lerp centre and corners around object
                cursorContainer.position = Vector2.Lerp(cursorContainer.position, targetPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerTL.transform.position = Vector2.Lerp(cursorCornerTL.transform.position, targetTLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerTR.transform.position = Vector2.Lerp(cursorCornerTR.transform.position, targetTRPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBL.transform.position = Vector2.Lerp(cursorCornerBL.transform.position, targetBLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBR.transform.position = Vector2.Lerp(cursorCornerBR.transform.position, targetBRPos, Time.deltaTime * cursorHoverMovementSpeed);
            }
            // Set colours
            cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
            cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
            cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
            cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorHoverColor, Time.deltaTime * cursorColorLerpSpeed);
        }

        // Is idling
        else
        {
            // Set to mouse and lerp corners
            cursorContainer.position = new Vector2(hoverPos.x, hoverPos.y);
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
        UnityEngine.Cursor.visible = false;
    }
}
