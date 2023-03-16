
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.PlayerLoop;

public class PlayerInteractor : MonoBehaviour, IInteractor
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
    [SerializeField] private float maxHoverDistance = 15.0f;
    [SerializeField] private float maxControlDistance = 9.0f;
    [SerializeField] private float controlWarningDistance = 3.0f;
    [SerializeField] private Color controlWarningColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color controlOutsideColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color controlDirColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);

    private LineHelper controlLimitLH;
    private LineHelper controlDirLH;
    private Vector2 hoverPos;
    private float hoverDistance;
    private WorldObject targetWO;
    private List<Interaction> targetInteractions;
    private bool isControlling;
    private bool isInteracting;
    public float squeezeAmount { get; private set; }


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
        UpdateInteraction();
    }

    private void HandleInput()
    {
        // Update cursor pos
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        hoverDistance = (hoverPos - (Vector2)playerController.transform.position).magnitude;

        // Focus on click window
        if (Input.GetMouseButtonDown(0)) Focus();
    }

    private void UpdateHover()
    {
        // Only retarget if not controlling or interacting
        if (!isControlling && !isInteracting)
        {
            WorldObject newHoveredWO = null;

            // - Mouse close enough to hover new
            if (hoverDistance < maxHoverDistance)
            {
                // Raycast at mouse on all cursorHoverables
                RaycastHit2D[] hits = Physics2D.RaycastAll(hoverPos, Vector2.zero);
                foreach (RaycastHit2D hit in hits)
                {
                    WorldObject hitWO = hit.transform.GetComponent<WorldObject>();
                    if (hitWO != null) { newHoveredWO = hitWO; break; }
                }
            }

            // - Overwrite current hover
            if (newHoveredWO != targetWO)
            {
                if (targetWO != null) targetWO.SetHovered(false);
                targetWO = newHoveredWO;
                if (targetWO != null)
                {
                    targetWO.SetHovered(true);
                    targetInteractions = targetWO.GetInteractions();
                }
                else targetInteractions = null;
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
            Vector2 limitedHoverDir = Vector2.ClampMagnitude(hoverDir, maxControlDistance);
            Vector2 limitedHoverPos = (Vector2)playerController.transform.position + limitedHoverDir;
            targetWO.controlPosition = limitedHoverPos;
            targetWO.controlForce = controlForce;

            // - Point leg
            playerLegs.isPointing = true;
            playerLegs.pointingLeg = 2;
            playerLegs.pointingPos = targetWO.transform.position;

            // - Mouse outside control length so show circle
            float outsidePct = Mathf.Min(1.0f - (maxControlDistance - hoverDir.magnitude) / controlWarningDistance, 1.0f);
            if (outsidePct > 0.0f)
            {
                controlLimitLH.SetActive(true);
                Color lerpColor;
                if (outsidePct < 1.0f) lerpColor = new Color(controlWarningColor.r, controlWarningColor.g, controlWarningColor.b, controlWarningColor.a * outsidePct);
                else lerpColor = controlOutsideColor;
                controlLimitLH.DrawCircle(playerController.transform.position, maxControlDistance, lerpColor, LineHelper.LineFill.DOTTED);
            }
            else
            {
                controlLimitLH.SetActive(false);
            }

            // - Line renderer to controlled
            controlDirLH.SetActive(true);
            Vector2 legEnd = playerLegs.GetLegEnd(playerLegs.pointingLeg);
            Vector3 controlDirLHFrom = new Vector3(legEnd.x, legEnd.y, playerController.transform.position.z + 0.1f);
            Vector3 controlDirLHTo = new Vector3(targetWO.physicalRB.position.x, targetWO.physicalRB.position.y, playerController.transform.position.z + 0.1f);
            controlDirLH.DrawLine(controlDirLHFrom, controlDirLHTo, controlDirColor, LineHelper.LineFill.SOLID);

            // - Drop object
            if (Input.GetMouseButtonDown(0)) SetTargetControlled(false);
        }

        // Not controlling
        else
        {
            // - Stop pointing legs
            playerLegs.isPointing = false;

            // - Disable line renderers
            controlLimitLH.SetActive(false);
            controlDirLH.SetActive(false);

            // - Not interacting, hovering and clicked
            if (!isInteracting && targetWO != null && targetWO.canControl && Input.GetMouseButtonDown(0)) SetTargetControlled(true);
        }
    }

    private void UpdateInteraction()
    {
        // Update interactions
        if (targetInteractions != null)
        {
            foreach (Interaction interaction in targetInteractions) interaction.TryInteract(this);
        }
    }


    private void FixedUpdate()
    {
        FixedUpdateCursor();
    }

    private void LateUpdate()
    {
        LateUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        // Enable centre indicator optionally
        centreIndicator.gameObject.SetActive(isControlling);

        // Hovering / controlling so surround
        if (targetWO != null)
        {
            // - Controlling so set centre
            if (isControlling)
            {
                centreIndicator.transform.position = new Vector2(hoverPos.x, hoverPos.y);
            }
        }

        // Is idling so set to base square
        else
        {
            cursorContainer.position = new Vector2(Mathf.Round(hoverPos.x * 12) / 12, Mathf.Round(hoverPos.y * 12) / 12);
            cursorCornerTL.transform.localPosition = Vector2.Lerp(cursorCornerTL.transform.localPosition, new Vector2(-cursorIdleDistance, cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerTR.transform.localPosition = Vector2.Lerp(cursorCornerTR.transform.localPosition, new Vector2(cursorIdleDistance, cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBL.transform.localPosition = Vector2.Lerp(cursorCornerBL.transform.localPosition, new Vector2(-cursorIdleDistance, -cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBR.transform.localPosition = Vector2.Lerp(cursorCornerBR.transform.localPosition, new Vector2(cursorIdleDistance, -cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
        }
    }

    private void LateUpdateCursor()
    {
        // Hovering / controlling so surround
        if (targetWO != null)
        {
            // Calculate targets
            Bounds b = targetWO.GetHoverBounds();
            Vector2 targetPos = b.center;
            float gap = cursorHoverGap * (1.0f - squeezeAmount);
            Vector2 targetTLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y + b.extents.y + gap);
            Vector2 targetTRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y + b.extents.y + gap);
            Vector2 targetBLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y - b.extents.y - gap);
            Vector2 targetBRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y - b.extents.y - gap);

            // - Controlling so set positions
            if (isControlling)
            {
                cursorContainer.position = targetPos;
                cursorCornerTL.transform.position = targetTLPos;
                cursorCornerTR.transform.position = targetTRPos;
                cursorCornerBL.transform.position = targetBLPos;
                cursorCornerBR.transform.position = targetBRPos;
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


        // Calculate correct color
        Color cursorColor =
            (isControlling) ? cursorColorControl
            : (hoverDistance > maxHoverDistance) ? cursorColorFar
            : (targetWO != null) ? cursorColoHover
            : cursorColorIdle;

        // Lerp colours
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
    }


    private void Focus()
    {
        UnityEngine.Cursor.visible = false;
    }


    private bool SetTargetControlled(bool toControl)
    {
        if (targetWO == null || toControl == isControlling) return false;

        // Try begin controlling (pickup)
        if (toControl)
        {
            if (targetWO.SetControlled(true))
            {
                isControlling = true;
                targetWO.controlPosition = targetWO.physicalRB.transform.position;
                targetWO.controlForce = controlForce;
                return true;
            }
        }

        // Try stop controlling (drop)
        else
        {
            if (targetWO.SetControlled(false))
            {
                isControlling = false;
                return true;
            }
        }

        // Somethings gone wrong
        return false;
    }


    #region IInteractor

    public void Interaction_SetSqueezeAmount(float squeezeAmount) => this.squeezeAmount= squeezeAmount;

    public void Interaction_SetInteracting(bool isInteracting) => this.isInteracting = isInteracting;
    
    public void Interaction_SetControlled(bool toControl) => SetTargetControlled(toControl);

    #endregion
}
