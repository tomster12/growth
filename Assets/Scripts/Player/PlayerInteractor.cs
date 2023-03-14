
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.PlayerLoop;

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
    [SerializeField] private Color cursorColorFar = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color cursorColorIdle = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color cursorColoHover = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color cursorColorControl = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private float cursorIdleDistance = 0.75f;
    [SerializeField] private float cursorIdleMovementSpeed = 50.0f;
    [SerializeField] private float cursorHoverMovementSpeed = 20.0f;
    [SerializeField] private float cursorHoverGap = 0.2f;
    [SerializeField] private float cursorColorLerpSpeed = 3.0f;
    [SerializeField] private float controlForce = 25.0f;
    [SerializeField] private float hoverDistance = 20.0f;
    [SerializeField] private float controlDistance = 10.0f;
    [SerializeField] private float controlWarningDistance = 3.0f;
    [SerializeField] private Color controlWarningColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color controlDirColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);

    public Vector2 hoverPos { get; private set; }
    public WorldObject hoveredWO { get; private set; }
    public float cursorDistance { get; private set; }
    public bool isControlling { get; private set; }
    private List<Interaction> hoveredInteractions;
    private LineHelper controlLimitLH;
    private LineHelper controlDirLH;


    private void Awake()
    {
        instance = this;

        // Initialize line helpers
        GameObject controlLimitLHGO = new GameObject();
        controlLimitLHGO.transform.parent = transform;
        controlLimitLH = controlLimitLHGO.AddComponent<LineHelper>();
        GameObject controlDirLHGO = new GameObject();
        controlDirLHGO.transform.parent = transform;
        controlDirLH = controlDirLHGO.AddComponent<LineHelper>();
    }

    private void Start()
    {
        Focus();

        // Move cursor to mouse pos immediately
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = new Vector2(hoverPos.x, hoverPos.y);

        // Hide centre indicator
        centreIndicator.gameObject.SetActive(false);
    }


    private void Update()
    {
        HandleInput();
        UpdateHover();
        UpdateControlled();
    }

    private void HandleInput()
    {
        // Update cursor pos
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorDistance = (hoverPos - (Vector2)playerController.transform.position).magnitude;

        // Focus on click window
        if (Input.GetMouseButtonDown(0)) Focus();
    }

    private void UpdateHover()
    {
        // Raycast at mouse on all cursorHoverables
        RaycastHit2D[] hits = Physics2D.RaycastAll(hoverPos, Vector2.zero);

        // Not controlling so could hover
        if (!isControlling)
        {
            WorldObject newHoveredWO = null;

            // - Mouse close enough to hover new
            if (cursorDistance < hoverDistance)
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
            Vector2 hoverDir = hoverPos - (Vector2)playerController.transform.position;
            Vector2 limitedHoverDir = Vector2.ClampMagnitude(hoverDir, controlDistance);
            Vector2 limitedHoverPos = (Vector2)playerController.transform.position + limitedHoverDir;
            hoveredWO.controlPosition = limitedHoverPos;
            hoveredWO.controlForce = controlForce;

            // - Point leg
            playerLegs.isPointing = true;
            playerLegs.pointingLeg = 2;
            playerLegs.pointingPos = hoveredWO.transform.position;

            // - Mouse outside control length so show circle
            float outsidePct = Mathf.Min(1.0f - (controlDistance - hoverDir.magnitude) / controlWarningDistance, 1.0f);
            if (outsidePct > 0.0f)
            {
                controlLimitLH.SetActive(true);
                Color lerpColor = new Color(controlWarningColor.r, controlWarningColor.g, controlWarningColor.b, controlWarningColor.a* outsidePct);
                controlLimitLH.DrawCircle(playerController.transform.position, controlDistance, lerpColor, LineHelper.LineFill.DOTTED);
            }
            else
            {
                controlLimitLH.SetActive(false);
            }

            // - Line renderer to controlled
            controlDirLH.SetActive(true);
            Vector2 legEnd = playerLegs.GetLegEnd(playerLegs.pointingLeg);
            Vector3 controlDirLHFrom = new Vector3(legEnd.x, legEnd.y, playerController.transform.position.z + 0.1f);
            Vector3 controlDirLHTo = new Vector3(hoveredWO.physicalRB.position.x, hoveredWO.physicalRB.position.y, playerController.transform.position.z + 0.1f);
            controlDirLH.DrawLine(controlDirLHFrom, controlDirLHTo, controlDirColor, LineHelper.LineFill.SOLID);

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

            // - Disable line renderers
            controlLimitLH.SetActive(false);
            controlDirLH.SetActive(false);

            // - Hovering and clicked
            if (hoveredWO != null && hoveredWO.canControl && Input.GetMouseButtonDown(0))
            {
                // - Try set controlled
                if (hoveredWO.SetControlled(true))
                {
                    isControlling = true;
                    hoveredWO.controlPosition = hoveredWO.physicalRB.transform.position;
                    hoveredWO.controlForce = controlForce;
                }
            }
        }

        // Update interactions
        if (hoveredInteractions != null)
        {
            foreach (Interaction interaction in hoveredInteractions) interaction.TryInteract();
        }
    }


    private void FixedUpdate()
    {
        FixedUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        // Enable centre indicator optionally
        centreIndicator.gameObject.SetActive(isControlling);

        // Hovering / controlling so surround object
        if (hoveredWO != null)
        {
            // Calculate targets
            Bounds b = hoveredWO.GetHoverBounds();
            Vector2 targetPos = b.center;
            Vector2 targetTLPos = new Vector2(b.center.x - b.extents.x - cursorHoverGap, b.center.y + b.extents.y + cursorHoverGap);
            Vector2 targetTRPos = new Vector2(b.center.x + b.extents.x + cursorHoverGap, b.center.y + b.extents.y + cursorHoverGap);
            Vector2 targetBLPos = new Vector2(b.center.x - b.extents.x - cursorHoverGap, b.center.y - b.extents.y - cursorHoverGap);
            Vector2 targetBRPos = new Vector2(b.center.x + b.extents.x + cursorHoverGap, b.center.y - b.extents.y - cursorHoverGap);
            
            // - Controlling so set positions
            if (isControlling)
            {
                cursorContainer.position = targetPos;
                cursorCornerTL.transform.position = targetTLPos;
                cursorCornerTR.transform.position = targetTRPos;
                cursorCornerBL.transform.position = targetBLPos;
                cursorCornerBR.transform.position = targetBRPos;
                centreIndicator.transform.position = new Vector2(hoverPos.x, hoverPos.y);
            }

            // - Just hovering so lerp positions
            else
            {
                cursorContainer.position = Vector2.Lerp(cursorContainer.position, targetPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerTL.transform.position = Vector2.Lerp(cursorCornerTL.transform.position, targetTLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerTR.transform.position = Vector2.Lerp(cursorCornerTR.transform.position, targetTRPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBL.transform.position = Vector2.Lerp(cursorCornerBL.transform.position, targetBLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBR.transform.position = Vector2.Lerp(cursorCornerBR.transform.position, targetBRPos, Time.deltaTime * cursorHoverMovementSpeed);
            }
        }

        // Is idling so just normal square
        else
        {
            cursorContainer.position = new Vector2(Mathf.Round(hoverPos.x * 12) / 12, Mathf.Round(hoverPos.y * 12) / 12);
            cursorCornerTL.transform.localPosition = Vector2.Lerp(cursorCornerTL.transform.localPosition, new Vector2(-cursorIdleDistance, cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);
            cursorCornerTR.transform.localPosition = Vector2.Lerp(cursorCornerTR.transform.localPosition, new Vector2(cursorIdleDistance, cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);
            cursorCornerBL.transform.localPosition = Vector2.Lerp(cursorCornerBL.transform.localPosition, new Vector2(-cursorIdleDistance, -cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);
            cursorCornerBR.transform.localPosition = Vector2.Lerp(cursorCornerBR.transform.localPosition, new Vector2(cursorIdleDistance, -cursorIdleDistance), Time.deltaTime * cursorIdleMovementSpeed);
        }

        // Calculate correct color
        Color cursorColor =
            (isControlling) ? cursorColorControl
            : (cursorDistance > hoverDistance) ? cursorColorFar
            : (hoveredWO != null) ? cursorColoHover
            : cursorColorIdle;

        // Lerp colours
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
    }


    void Focus()
    {
        UnityEngine.Cursor.visible = false;
    }
}
