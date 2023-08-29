
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class PlayerInteractor : MonoBehaviour, IInteractor
{
    public static PlayerInteractor instance;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerLegs playerLegs;
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer cursorCornerTL, cursorCornerTR, cursorCornerBL, cursorCornerBR;
    [SerializeField] private SpriteRenderer centreIndicator;
    [SerializeField] private ChildOrganiser promptOrganiser;

    [Header("Prefabs")]
    [SerializeField] private GameObject promptPfb;

    [Header("Config")]
    [SerializeField] private Color cursorColorFar = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color cursorColorIdle = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color cursorColoHover = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color cursorColorControl = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private Color cursorColorInteractable = new Color(1.0f, 0.3f, 0.3f);
    [SerializeField] private Color cursorColorInteracting = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private float cursorIdleDistance = 0.75f;
    [SerializeField] private float cursorIdleMovementSpeed = 50.0f;
    [SerializeField] private float cursorHoverMovementSpeed = 20.0f;
    [SerializeField] private float cursorHoverGap = 0.2f;
    [SerializeField] private float cursorControlGap = 0.05f;
    [SerializeField] private float cursorColorLerpSpeed = 3.0f;
    [SerializeField] private float controlForce = 25.0f;
    [SerializeField] private float maxHoverDistance = 15.0f;
    [SerializeField] private float maxControlDistance = 9.0f;
    [SerializeField] private float controlWarningDistance = 3.0f;
    [SerializeField] private float controlSlowdown = 0.4f;
    [SerializeField] private float interactSlowdown = 0.7f;
    [SerializeField] private Color controlWarningColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color controlOutsideColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color legDirControlColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
    [SerializeField] private Color legDirInteractColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
    [SerializeField] private float promptOffset = 1.0f;

    public float squeezeAmount { get; private set; }

    private LineHelper controlLimitLH;
    private LineHelper targetDirLH;
    private Vector2 hoverPos;
    private float hoverDistance;
    private IInteractable targetIInteractable;
    private List<Interaction> targetInteractions;
    private int targetInteractionsEnabled => targetInteractions == null ? 0 : targetInteractions.Where(i => i.isEnabled).Count();
    private float targetDistance;
    private bool isControlling;
    private bool isInteracting;


    private void Awake()
    {
        instance = this;

        // Initialize line helpers
        GameObject controlLimitLHGO = new GameObject();
        controlLimitLHGO.transform.parent = transform;
        controlLimitLH = controlLimitLHGO.AddComponent<LineHelper>();
        GameObject targetDirLHGO = new GameObject();
        targetDirLHGO.transform.parent = transform;
        targetDirLH = targetDirLHGO.AddComponent<LineHelper>();
    }

    private void Start()
    {
        // Move cursor to mouse pos immediately
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = new Vector2(hoverPos.x, hoverPos.y);

        // Hide centre indicator
        centreIndicator.gameObject.SetActive(false);

        // Subscribe pause event
        GameManager.instance.SubscribeOnIsPausedChange(OnGameManagerIsPausedChange);
    }


    public void SetSqueezeAmount(float squeezeAmount) => this.squeezeAmount = squeezeAmount;

    public void SetInteracting(bool isInteracting) => this.isInteracting = isInteracting;
    
    public void SetControlled(bool toControl) => SetTargetControlled(toControl);


    private void Update()
    {
        if (GameManager.isPaused) return;
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
    }

    private void UpdateHover()
    {
        // Only retarget if not controlling or interacting
        if (!isControlling && !isInteracting)
        {
            IInteractable hoveredIInteractable = null;

            // - Mouse close enough to hover new
            if (hoverDistance < maxHoverDistance)
            {
                // Raycast at mouse on all cursorHoverables
                RaycastHit2D[] hits = Physics2D.RaycastAll(hoverPos, Vector2.zero);
                foreach (RaycastHit2D hit in hits)
                {
                    IInteractable hitIInteractable = hit.transform.GetComponent<IInteractable>();
                    if (hitIInteractable != null) { hoveredIInteractable = hitIInteractable; break; }
                }
            }

            // - Overwrite current hover
            if (hoveredIInteractable != targetIInteractable)
            {
                if (targetIInteractable != null) targetIInteractable.SetHovered(false);
                targetIInteractable = hoveredIInteractable;
                if (targetIInteractable != null) targetIInteractable.SetHovered(true);
                SetTargetInteractions(targetIInteractable == null ? null : targetIInteractable.GetInteractions());
            }
        }
    }

    private void UpdateControlled()
    {
        // Currently controlling
        if (isControlling)
        {
            targetDistance = (targetIInteractable.GetPosition() - (Vector2)playerController.transform.position).magnitude;
            
            // - Set control position
            Vector2 hoverDir = hoverPos - (Vector2)playerController.transform.position;
            Vector2 limitedHoverDir = Vector2.ClampMagnitude(hoverDir, maxControlDistance);
            Vector2 limitedHoverPos = (Vector2)playerController.transform.position + limitedHoverDir;
            targetIInteractable.SetControlPosition(limitedHoverPos, controlForce);

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

            // - Drop object
            if (Input.GetMouseButtonDown(0)) SetTargetControlled(false);
        }

        // Not controlling
        else
        {
            // - Disable line renderers
            controlLimitLH.SetActive(false);

            // - Not interacting, hovering and clicked so pickup object
            if (!isInteracting && targetIInteractable != null && targetIInteractable.CanControl && Input.GetMouseButtonDown(0)) SetTargetControlled(true);
        }
    }

    private void UpdateInteraction()
    {
        // Point legs at controlled or interacted
        if (isInteracting || isControlling)
        {
            // - lUpdate leg variables
            playerLegs.isPointing = true;
            playerLegs.pointingLeg = 2;
            playerLegs.pointingPos = targetIInteractable.GetPosition();

            // - Draw line
            targetDirLH.SetActive(true);
            Vector2 pathStart = playerLegs.GetLegEnd(playerLegs.pointingLeg);
            Vector2 pathEnd = targetIInteractable.GetPosition();
            Vector3 pathStartFixed = new Vector3(pathStart.x, pathStart.y, playerController.transform.position.z + 0.1f);
            Vector3 pathEndFixed = new Vector3(pathEnd.x, pathEnd.y, playerController.transform.position.z + 0.1f);
            Color col = isInteracting ? legDirInteractColor : legDirControlColor;
            if (isControlling) col.a *= 0.1f + 0.9f * Mathf.Max(1.0f - targetDistance / maxControlDistance, 0.0f);
            targetDirLH.DrawLine(pathStartFixed, pathEndFixed, col, LineHelper.LineFill.SOLID);
        }

        // Do not point legs
        else
        {
            playerLegs.isPointing = false;
            targetDirLH.SetActive(false);
        }

        // Update slowdown
        playerController.movementSlowdown = isInteracting ? interactSlowdown : isControlling ? controlSlowdown : 0.0f;

        // Update interactions
        if (targetInteractions != null)
        {
            foreach (Interaction interaction in targetInteractions) interaction.TryInteract(this);
        }
    }
    
    private void FixedUpdate()
    {
        if (GameManager.isPaused) return;
        FixedUpdateCursor();
    }

    private void LateUpdate()
    {
        if (GameManager.isPaused) return;
        LateUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        // Enable centre indicator optionally
        centreIndicator.gameObject.SetActive(isControlling);

        // Hovering / controlling so surround
        if (targetIInteractable != null)
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
        if (targetIInteractable != null)
        {
            // Calculate targets
            Bounds b = targetIInteractable.GetHoverBounds();
            Vector2 targetPos = b.center;
            float gap = isControlling ? cursorControlGap : (cursorHoverGap * (1.0f - squeezeAmount));
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
                cursorContainer.position = targetPos;
                cursorCornerTL.transform.position = Vector2.Lerp(cursorCornerTL.transform.position, targetTLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerTR.transform.position = Vector2.Lerp(cursorCornerTR.transform.position, targetTRPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBL.transform.position = Vector2.Lerp(cursorCornerBL.transform.position, targetBLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBR.transform.position = Vector2.Lerp(cursorCornerBR.transform.position, targetBRPos, Time.deltaTime * cursorHoverMovementSpeed);
            }

        }

        // Calculate correct color
        Color cursorColor =
            (isInteracting) ? cursorColorInteracting
            : (targetInteractionsEnabled > 0) ? cursorColorInteractable
            : (isControlling) ? cursorColorControl
            : (hoverDistance > maxHoverDistance) ? cursorColorFar
            : (targetIInteractable != null) ? cursorColoHover
            : cursorColorIdle;

        // Lerp colours
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);

        // Move prompt organiser
        promptOrganiser.transform.localPosition = Vector3.right * promptOffset;
    }

    private void SetTargetInteractions(List<Interaction> targetInteractions)
    {
        // Update variables
        this.targetInteractions = targetInteractions;

        // Clear organiser, add all children, update organiser
        promptOrganiser.Clear();
        if (targetInteractions != null)
        {
            foreach (Interaction interaction in targetInteractions)
            {
                GameObject promptGO = Instantiate(promptPfb);
                Prompt prompt = promptGO.GetComponent<Prompt>();
                prompt.SetInteraction(interaction);
                promptOrganiser.AddChild(prompt);
            }
            promptOrganiser.UpdateChildren();
        }
    }

    private bool SetTargetControlled(bool toControl)
    {
        if (targetIInteractable == null || toControl == isControlling) return false;

        // Try begin controlling (pickup)
        if (toControl)
        {
            if (targetIInteractable.SetControlled(true))
            {
                isControlling = true;
                targetIInteractable.SetControlPosition(targetIInteractable.GetPosition(), controlForce);
                return true;
            }
        }

        // Try stop controlling (drop)
        else
        {
            if (targetIInteractable.SetControlled(false))
            {
                isControlling = false;
                return true;
            }
        }

        // Somethings gone wrong
        return false;
    }

    private void OnGameManagerIsPausedChange(bool isPaused)
    {
        cursorContainer.gameObject.SetActive(!isPaused);
        if (!isPaused)
        {
            FixedUpdateCursor();
            LateUpdateCursor();
        }
    }
}
