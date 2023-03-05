
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class InteractionInput
{
    public static InteractionInput LMB = new InteractionMouseInput(0);
    public static InteractionInput RMB = new InteractionMouseInput(0);

    public String name;

    public abstract bool CheckInput();
    public abstract bool CheckInputDown();
    public abstract bool CheckInputUp();
}

[Serializable]
public class InteractionKeyInput : InteractionInput
{
    public KeyCode code;

    public InteractionKeyInput(KeyCode code)
    {
        this.name = code.ToString();
        this.code = code;
    }

    public override bool CheckInput() => Input.GetKey(code);
    public override bool CheckInputDown() => Input.GetKeyDown(code);
    public override bool CheckInputUp() => Input.GetKeyUp(code);
}

[Serializable]
public class InteractionMouseInput : InteractionInput
{
    public int button;

    public InteractionMouseInput(int button)
    {
        this.name = button == 0 ? "LMB" : button == 1 ? "RMB" : button.ToString();
        this.button = button;
    }

    public override bool CheckInput() => Input.GetMouseButton(button);
    public override bool CheckInputDown() => Input.GetMouseButtonDown(button);
    public override bool CheckInputUp() => Input.GetMouseButtonUp(button);
}

[Serializable]
public class Interaction
{
    public enum Visibility { HIDDEN, ICON, FULL }
    public enum Type { CLICK, ITEM }

    public bool isEnabled;
    public Visibility visibility;
    public String name;
    public InteractionInput input;
    public Action holdCallback, downCallback, upCallback;

    public Interaction(bool isEnabled, Visibility visibility, string name, InteractionInput input, Action holdCallback, Action downCallback, Action upCallback)
    {
        this.isEnabled = isEnabled;
        this.visibility = visibility;
        this.name = name;
        this.input = input;
        this.holdCallback = holdCallback;
        this.downCallback = downCallback;
        this.upCallback = upCallback;
    }

    public bool TryInteract()
    {
        if (!isEnabled) return false;
        else if (input.CheckInput() && holdCallback != null) holdCallback();
        else if (input.CheckInputDown() && downCallback != null) downCallback();
        else if (input.CheckInputUp() && upCallback != null) upCallback();
        else return false;
        return true;
    }
}


[Serializable]
public class HoverManager
{
    public bool isHovering;
    public WorldObject currentWO;
    public List<Interaction> interactions;


    public void Hover(WorldObject newWO)
    {
        if (newWO == currentWO) return;

        // Unhover old
        if (currentWO != null) currentWO.SetHovered(false);

        // Set variables
        currentWO = newWO;
        isHovering = currentWO != null;

        // Hover new
        if (isHovering)
        {
            currentWO.SetHovered(true);
            interactions = currentWO.GetInteractions();
        }
    }
}


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
    public HoverManager hover { get; private set; } = new HoverManager();


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

        // Check interactions
        if (hover.isHovering)
        {
            foreach (Interaction interaction in hover.interactions)
            {
                if (interaction.TryInteract()) break;
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
        WorldObject hoveredWO = null;
        foreach (RaycastHit2D hit in hits)
        {
            WorldObject hitWO = hit.transform.GetComponent<WorldObject>();
            if (hitWO != null) { hoveredWO = hitWO; break; }
        }

        // Hover new object
        hover.Hover(hoveredWO);
    }

    private void UpdateCursor()
    {
        // Surround cursorHover object
        if (hover.isHovering)
        {
            Bounds b = hover.currentWO.GetHighlightBounds();

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
