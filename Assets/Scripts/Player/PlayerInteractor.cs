
using System;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;


public class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor instance;

    [Header("References")]
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer cursorCornerTL, cursorCornerTR, cursorCornerBL, cursorCornerBR;
    [SerializeField] private PlayerLegs playerLegs;

    [Header("Config")]
    [SerializeField] private Color cursorIdleColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private float cursorIdleDistance = 0.75f;
    [SerializeField] private float cursorIdleMovementSpeed = 50.0f;
    [SerializeField] private Color cursorHoverColor = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private float cursorHoverMovementSpeed = 20.0f;
    [SerializeField] private float cursorHoverGap = 0.2f;
    [SerializeField] private float cursorColorLerpSpeed = 3.0f;

    public Vector2 hoverPos { get; private set; }
    public WorldObject hoveredWO { get; private set; }
    public WorldObject controlledWO { get; private set; }


    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Focus();

        // Move cursor to mouse pos immediately
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = new Vector2(hoverPos.x, hoverPos.y);
    }


    private void Update()
    {
        // Focus on click window
        if (Input.GetMouseButtonDown(0)) Focus();

        // Main update
        UpdateHover();
        UpdateControlled();
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
        WorldObject newHoveredWO = null;
        foreach (RaycastHit2D hit in hits)
        {
            WorldObject hitWO = hit.transform.GetComponent<WorldObject>();
            if (hitWO != null) { newHoveredWO = hitWO; break; }
        }

        // Update hovered WO
        if (newHoveredWO != hoveredWO)
        {
            if (hoveredWO != null) hoveredWO.SetHovered(false);
            hoveredWO = newHoveredWO;
            if (hoveredWO != null) hoveredWO.SetHovered(true);
        }
    }

    private void UpdateControlled()
    {
        if (controlledWO != null)
        {
            controlledWO.controlPosition = hoverPos;
            if (Input.GetMouseButtonDown(0))
            {
                controlledWO.SetControlled(false);
            }
        }
        else if (hoveredWO != null && !hoveredWO.canControl)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hoveredWO.SetControlled(true)) controlledWO = hoveredWO;
            }
        }
        

        // Point leg
        if (Input.GetMouseButton(0))
        {
            playerLegs.isPointing = true;
            playerLegs.pointingLeg = 2;
            playerLegs.pointingPos = hoverPos;
        }
        else playerLegs.isPointing = false;

    }

    private void UpdateCursor()
    {
        // Surround cursorHover object
        if (hoveredWO != null)
        {
            Bounds b = hoveredWO.GetHoverBounds();

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
            cursorContainer.position = new Vector2(hoverPos.x, hoverPos.y);

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
        UnityEngine.Cursor.visible = false;
    }
}
