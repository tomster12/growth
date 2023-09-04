
using System.Linq;
using UnityEngine;


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

    public float CursorSpacing { get; private set; }

    private LineHelper controlLimitLH;
    private LineHelper targetDirLH;
    private Vector2 hoverPos;
    private float hoverDistance;
    private ComposableObject targetComposable;
    private float targetDistance;
    private bool isControlling;
    private Interaction currentInteraction;
    private bool IsInteracting => currentInteraction != null;
    private bool IsTargeting => targetComposable != null;


    public void SetInteractionEmphasis(float emphasis) => this.CursorSpacing = emphasis;

    public void SetInteraction(Interaction interaction) => this.currentInteraction = interaction;
    
    public void SetControlled(bool toControl) => SetTargetControlled(toControl);


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
        GameManager.Instance.SubscribeOnIsPausedChange(OnGameManagerIsPausedChange);
    }

    private void Update()
    {
        if (GameManager.IsPaused) return;
        HandleInput();
        UpdateHovering();
        UpdateControlling();
        UpdateInteracting();
    }

    private void HandleInput()
    {
        // Update cursor pos
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        hoverDistance = (hoverPos - (Vector2)playerController.Transform.position).magnitude;
    }

    private void UpdateHovering()
    {
        // Only retarget if not controlling or interacting
        if (!isControlling && !IsInteracting)
        {
            ComposableObject hoveredComposable = null;

            // - Mouse close enough to hover new
            if (hoverDistance < maxHoverDistance)
            {
                // Raycast at mouse on all ComposableObjects
                RaycastHit2D[] hits = Physics2D.RaycastAll(hoverPos, Vector2.zero);
                foreach (RaycastHit2D hit in hits)
                {
                    ComposableObject hitComposable = hit.transform.GetComponent<ComposableObject>();
                    if (hitComposable != null) { hoveredComposable = hitComposable; break; }
                }
            }

            // - Overwrite current target
            if (hoveredComposable != targetComposable)
            {
                targetComposable?.PartInteractable?.StopViewingInteractions();
                targetComposable?.PartHighlightable?.SetHighlighted(false);
                targetComposable = hoveredComposable;
                targetComposable?.PartHighlightable?.SetHighlighted(true);
                targetComposable?.PartInteractable?.StartViewingInteractions(this);
                UpdateInteractionsList();
            }
        }
    }

    private void UpdateControlling()
    {
        // Currently controlling
        if (isControlling)
        {
            // Calcoulate target distance
            Vector2 targetDir = targetComposable.Position - (Vector2)playerController.Transform.position;
            targetDistance = targetDir.magnitude;
            
            // - Recalculate hoverDir and set control position
            Vector2 hoverDir = hoverPos - (Vector2)playerController.Transform.position;
            Vector2 limitedHoverDir = Vector2.ClampMagnitude(hoverDir, maxControlDistance);
            Vector2 limitedHoverPos = (Vector2)playerController.Transform.position + limitedHoverDir;
            targetComposable.PartControllable.SetControlPosition(limitedHoverPos, controlForce);

            // - Mouse outside control length so show circle
            float outsidePct = Mathf.Min(1.0f - (maxControlDistance - targetDistance) / controlWarningDistance, 1.0f);
            if (outsidePct > 0.0f)
            {
                controlLimitLH.SetActive(true);
                Color lerpColor;
                if (outsidePct < 1.0f) lerpColor = new Color(controlWarningColor.r, controlWarningColor.g, controlWarningColor.b, controlWarningColor.a * outsidePct);
                else lerpColor = controlOutsideColor;
                controlLimitLH.DrawCircle(playerController.Transform.position, maxControlDistance, lerpColor, LineHelper.LineFill.DOTTED);
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
            if (!IsInteracting
                && targetComposable != null
                && targetComposable.HasPartControllable
                && targetComposable.PartControllable.CanControl
                && Input.GetMouseButtonDown(0))
            {
                SetTargetControlled(true);
            }
        }
    }

    private void UpdateInteracting()
    {
        // Point legs at controlled or interacted
        if (IsInteracting || isControlling)
        {
            // - Update leg variables
            playerLegs.IsPointing = true;
            playerLegs.PointingLeg = 2;
            playerLegs.PointingPos = targetComposable.Position;

            // - Draw line
            targetDirLH.SetActive(true);
            Vector2 pathStart = playerLegs.GetLegEnd(playerLegs.PointingLeg);
            Vector2 pathEnd = targetComposable.Position;
            Vector3 pathStartFixed = new Vector3(pathStart.x, pathStart.y, playerController.Transform.position.z + 0.1f);
            Vector3 pathEndFixed = new Vector3(pathEnd.x, pathEnd.y, playerController.Transform.position.z + 0.1f);
            Color col = IsInteracting ? legDirInteractColor : legDirControlColor;
            if (isControlling) col.a *= 0.1f + 0.9f * Mathf.Max(1.0f - targetDistance / maxControlDistance, 0.0f);
            targetDirLH.DrawLine(pathStartFixed, pathEndFixed, col, LineHelper.LineFill.SOLID);
        }

        // Do not point legs
        else
        {
            playerLegs.IsPointing = false;
            targetDirLH.SetActive(false);
        }

        // Update slowdown
        playerController.MovementSlowdown = IsInteracting ? interactSlowdown : isControlling ? controlSlowdown : 0.0f;
    }
    
    private void UpdateInteractionsList()
    {
        // Clear organiser, add all children, update organiser
        promptOrganiser.Clear();
        if (targetComposable?.PartInteractable != null)
        {
            foreach (Interaction interaction in targetComposable.PartInteractable.Interactions)
            {
                GameObject promptGO = Instantiate(promptPfb);
                Prompt prompt = promptGO.GetComponent<Prompt>();
                prompt.SetInteraction(interaction);
                promptOrganiser.AddChild(prompt);
            }
            promptOrganiser.UpdateChildren();
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        FixedUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        // Enable centre indicator optionally
        centreIndicator.gameObject.SetActive(isControlling);

        // Hovering / controlling so surround
        if (targetComposable != null)
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

    private void LateUpdate()
    {
        if (GameManager.IsPaused) return;
        LateUpdateCursor();
    }

    private void LateUpdateCursor()
    {
        // Hovering / controlling so surround
        if (targetComposable != null)
        {
            // Calculate targets
            Bounds b = targetComposable.Bounds;
            Vector2 targetPos = b.center;
            float gap = isControlling ? cursorControlGap : (cursorHoverGap * (1.0f - CursorSpacing));
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
            (IsInteracting) ? cursorColorInteracting
            : (GetAvailableInteractionsCount() > 0) ? cursorColorInteractable
            : isControlling ? cursorColorControl
            : (hoverDistance > maxHoverDistance) ? cursorColorFar
            : (targetComposable != null) ? cursorColoHover
            : cursorColorIdle;

        // Lerp colours
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);

        // Move prompt organiser
        promptOrganiser.transform.localPosition = Vector3.right * promptOffset;
    }

    private int GetAvailableInteractionsCount()
    {
        if (targetComposable == null) return 0;
        if (targetComposable.PartInteractable == null) return 0;
        return targetComposable.PartInteractable.Interactions.Where(i => i.IsEnabled && !i.IsBlocked).Count();
    }

    private bool SetTargetControlled(bool toControl)
    {
        if (targetComposable == null || toControl == isControlling) return false;

        // Try begin controlling (pickup)
        if (toControl)
        {
            if (targetComposable.PartControllable.SetControlled(true))
            {
                isControlling = true;
                targetComposable.PartControllable.SetControlPosition(targetComposable.Position, controlForce);
                return true;
            }
        }

        // Try stop controlling (drop)
        else
        {
            if (targetComposable.PartControllable.SetControlled(false))
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
