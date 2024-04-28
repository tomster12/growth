using System.Collections;
using UnityEngine;

public partial class PlayerInteractor : MonoBehaviour
{
    public enum InteractorState
    { Idle, Hovering, Controlling, Interacting }

    public static PlayerInteractor Instance { get; private set; }

    public float InteractionEmphasis { get; set; }

    public bool StartInteracting(PlayerInteraction interaction)
    {
        if (state == InteractorState.Interacting || !CanInteractTarget) return false;
        currentInteraction = interaction;
        state = InteractorState.Interacting;
        return true;
    }

    public bool StopInteracting(PlayerInteraction interaction)
    {
        if (state != InteractorState.Interacting || currentInteraction != interaction) return false;
        currentInteraction = null;
        state = InteractorState.Hovering;
        UpdateHovering();
        return true;
    }

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLegs playerLegs;
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer cursorCornerTL, cursorCornerTR, cursorCornerBL, cursorCornerBR;
    [SerializeField] private SpriteRenderer centreIndicator;
    [SerializeField] private SpriteRenderer limitedCentreIndicator;
    [SerializeField] private ChildOrganiser promptOrganiser;

    [Header("Prefabs")]
    [SerializeField] private GameObject promptPfb;
    [SerializeField] private GameObject dropPsysPfb;

    [Header("Cursor Config")]
    [SerializeField] private float cursorIdleMovementSpeed = 50.0f;
    [SerializeField] private float cursorHoverMovementSpeed = 20.0f;
    [SerializeField] private float cursorIdleSize = 0.75f;
    [SerializeField] private float cursorHoverGap = 0.2f;
    [SerializeField] private float cursorControlGap = 0.05f;
    [SerializeField] private float cursorColorLerpSpeed = 25.0f;
    [SerializeField] private Color cursorColorFar = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color cursorColorIdle = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color cursorColorHover = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color cursorColorControl = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private Color cursorColorInteractable = new Color(1.0f, 0.3f, 0.3f);
    [SerializeField] private Color cursorColorInteracting = new Color(0.5f, 0.5f, 0.5f);
    [Header("Controlling Config")]
    [SerializeField] private float controlForce = 25.0f;
    [SerializeField] private float maxHoverDistance = 15.0f;
    [SerializeField] private float maxControlDistance = 9.0f;
    [SerializeField] private float maxControlWarningThreshold = 2.0f;
    [SerializeField] private float controlCharacterSlowdown = 0.4f;
    [SerializeField] private float interactCharacterSlowdown = 0.85f;
    [SerializeField] private float controlDropTimerDuration = 0.5f;
    [SerializeField] private float controlLimitColorLerpSpeed = 10.0f;
    [SerializeField] private Color controlLimitColorWarning = new Color(0.774f, 0.624f, 0.624f, 0.098f);
    [SerializeField] private Color controlLimitColorOutside = new Color(0.9f, 0.2f, 0.2f, 0.184f);
    [SerializeField] private Color legDirControlColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
    [SerializeField] private Color legDirInteractColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
    [Header("Prompts Config")]
    [SerializeField] private float indicateDistance = 120.0f;
    [SerializeField] private float indicateSpeed = 80.0f;
    [SerializeField] private float indicateTimerMax = 3.0f;
    [SerializeField] private float promptOffset = 1.0f;

    private LineHelper controlLimitLH;
    private LineHelper targetDirLH;
    private Color controlLimitClearColor;
    private Color controlLimitCurrentColor;

    private InteractorState state;
    private Vector2 mousePos;
    private float mouseDistance;
    private CompositeObject target;
    private float targetDistance;
    private Vector2 limitedControlDir;
    private Vector2 limitedControlPos;
    private float controlDropTimer;
    private PlayerInteraction currentInteraction;
    private PartControllable TargetControllable => target?.GetPart<PartControllable>();
    private PartInteractable TargetInteractable => target?.GetPart<PartInteractable>();
    private PartHighlightable TargetHighlightable => target?.GetPart<PartHighlightable>();
    private bool CanInteractTarget => state == InteractorState.Hovering;
    private bool CanControlTarget => state == InteractorState.Hovering && TargetControllable != null && TargetControllable.CanControl;

    private void Awake()
    {
        Instance = this;

        // Initialize line helpers
        GameObject controlLimitLHGO = new GameObject();
        controlLimitLHGO.transform.parent = transform;
        controlLimitLH = controlLimitLHGO.AddComponent<LineHelper>();
        GameObject controlDirLHGO = new GameObject();
        controlDirLHGO.transform.parent = transform;
        targetDirLH = controlDirLHGO.AddComponent<LineHelper>();
        controlLimitClearColor = new Color(controlLimitColorWarning.r, controlLimitColorWarning.g, controlLimitColorWarning.b, 0.0f);
    }

    private void Start()
    {
        // Move cursor to mouse pos immediately
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = new Vector2(mousePos.x, mousePos.y);

        // Hide centre indicators
        centreIndicator.gameObject.SetActive(false);
        limitedCentreIndicator.gameObject.SetActive(false);

        // Subscribe pause event
        GameManager.onIsPausedChange += OnGameManagerIsPausedChange;
    }

    private void Update()
    {
        if (GameManager.IsPaused) return;
        UpdateHandleInput();
        UpdateHovering();
        UpdateControlling();
        UpdateInteracting();
        UpdateIndicating();
    }

    private void UpdateHandleInput()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseDistance = (mousePos - (Vector2)playerMovement.Transform.position).magnitude;
    }

    private void UpdateHovering()
    {
        if (state == InteractorState.Idle || state == InteractorState.Hovering)
        {
            CompositeObject hovered = null;

            // Mouse within range to hover so raycast for new target
            if (mouseDistance < maxHoverDistance)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.transform.TryGetComponent<CompositeObject>(out var hitComposable))
                    {
                        hovered = hitComposable;
                        break;
                    }
                }
            }

            // Update with new target, can be null
            SetTarget(hovered);
            state = hovered ? InteractorState.Hovering : InteractorState.Idle;
        }
    }

    private void UpdateControlling()
    {
        if (state == InteractorState.Controlling)
        {
            // Calculate mouse statistics
            Vector2 targetDir = target.Position - (Vector2)playerMovement.Transform.position;
            Vector2 hoverDir = mousePos - (Vector2)playerMovement.Transform.position;
            targetDistance = targetDir.magnitude;
            mouseDistance = hoverDir.magnitude;

            // Limit control dir and set control position
            limitedControlDir = Vector2.ClampMagnitude(hoverDir, maxControlDistance - 0.1f);
            limitedControlPos = (Vector2)playerMovement.Transform.position + limitedControlDir;
            TargetControllable.SetControlPosition(limitedControlPos, controlForce);

            // Mouse outside control length so show circle
            float hoverBoundaryPct = Mathf.Min(1.0f, 1.0f - (maxControlDistance - mouseDistance) / maxControlWarningThreshold);
            float targetBoundaryPct = Mathf.Min(1.0f, 1.0f - (maxControlDistance - targetDistance) / maxControlWarningThreshold);
            float maxBoundaryPct = Mathf.Max(hoverBoundaryPct, targetBoundaryPct);
            Color targetLimitColor;
            if (maxBoundaryPct > 0.0f)
            {
                if (targetBoundaryPct == 1.0f) targetLimitColor = controlLimitColorOutside;
                else targetLimitColor = new Color(controlLimitColorWarning.r, controlLimitColorWarning.g, controlLimitColorWarning.b, controlLimitColorWarning.a * maxBoundaryPct);
            }
            else targetLimitColor = controlLimitClearColor;
            controlLimitCurrentColor = Color.Lerp(controlLimitCurrentColor, targetLimitColor, controlLimitColorLerpSpeed * Time.deltaTime);
            controlLimitLH.DrawCircle(playerMovement.Transform.position, maxControlDistance, controlLimitCurrentColor, lineFill: LineFill.Dotted);

            // Draw control line and point
            //playerLegs.IsPointing = true; TODO
            //playerLegs.PointingLeg = 2; TODO
            //playerLegs.PointingPos = targetComposable.Position; TODO
            targetDirLH.SetActive(true);
            Vector2 pathStart = target.Position; // playerLegs.GetLegEnd(playerLegs.PointingLeg); TODO
            Vector2 pathEnd = target.Position;
            Vector3 pathStartFixed = new Vector3(pathStart.x, pathStart.y, playerMovement.Transform.position.z + 0.1f);
            Vector3 pathEndFixed = new Vector3(pathEnd.x, pathEnd.y, playerMovement.Transform.position.z + 0.1f);
            Color col = legDirControlColor;
            col.a *= 0.1f + 0.9f * Mathf.Max(1.0f - targetDistance / maxControlDistance, 0.0f);
            targetDirLH.DrawLine(pathStartFixed, pathEndFixed, col);

            // Drop when far away
            if (targetBoundaryPct == 1.0f)
            {
                controlDropTimer += Time.deltaTime;
                if (controlDropTimer > controlDropTimerDuration)
                {
                    Vector3 dropPos = target.Position;
                    if (SetTargetControlled(false))
                    {
                        GameObject particlesGO = Instantiate(dropPsysPfb);
                        particlesGO.transform.position = dropPos;
                    }
                }
            }
            else controlDropTimer = 0.0f;

            // Drop object on click
            if (Input.GetMouseButtonDown(0)) SetTargetControlled(false);
        }

        // If not controlling then check for control input
        else if (CanControlTarget && Input.GetMouseButtonDown(0)) SetTargetControlled(true);
    }

    private void UpdateInteracting()
    {
        // TODO: ???
        if (state == InteractorState.Hovering || state == InteractorState.Interacting)
        {
            TargetInteractable?.UpdateInteracting();
        }
    }

    private void UpdateIndicating()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            IEnumerator showIndicator(PartIndicatable part, float wait, float time)
            {
                yield return new WaitForSeconds(wait);
                part.Show(time);
            }

            // Find indicatable objects within indicate distance and show
            var objects = CompositeObject.FindObjectsWithPart<PartIndicatable>();
            foreach (var obj in objects)
            {
                var part = obj.GetPart<PartIndicatable>();
                float dist = (obj.Position - (Vector2)playerMovement.Transform.position).magnitude;
                if (dist < indicateDistance) StartCoroutine(showIndicator(part, dist / indicateSpeed, indicateTimerMax));
            }
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        FixedUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        // Idle: Lerp to base square
        if (state == InteractorState.Idle)
        {
            cursorContainer.position = new Vector2(Mathf.Round(mousePos.x * 12) / 12, Mathf.Round(mousePos.y * 12) / 12);
            cursorCornerTL.transform.localPosition = Vector2.Lerp(cursorCornerTL.transform.localPosition, new Vector2(-cursorIdleSize, cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerTR.transform.localPosition = Vector2.Lerp(cursorCornerTR.transform.localPosition, new Vector2(cursorIdleSize, cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBL.transform.localPosition = Vector2.Lerp(cursorCornerBL.transform.localPosition, new Vector2(-cursorIdleSize, -cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBR.transform.localPosition = Vector2.Lerp(cursorCornerBR.transform.localPosition, new Vector2(cursorIdleSize, -cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
        }

        // Controlling: Update needed indicators to mouse position
        if (state == InteractorState.Controlling)
        {
            centreIndicator.gameObject.SetActive(true);
            centreIndicator.transform.position = new Vector2(mousePos.x, mousePos.y);
            if (mouseDistance > maxControlDistance)
            {
                limitedCentreIndicator.gameObject.SetActive(true);
                limitedCentreIndicator.transform.position = limitedControlPos;
            }
            else limitedCentreIndicator.gameObject.SetActive(false);
        }
        else centreIndicator.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (GameManager.IsPaused) return;
        LateUpdateCursor();
    }

    private void LateUpdateCursor()
    {
        // Targetting object so surround with cursor
        if (target != null)
        {
            Bounds b = target.Bounds;
            Vector2 targetPos = b.center;
            float gap = state == InteractorState.Controlling ? cursorControlGap : (cursorHoverGap * (1.0f - InteractionEmphasis));
            Vector2 targetTLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y + b.extents.y + gap);
            Vector2 targetTRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y + b.extents.y + gap);
            Vector2 targetBLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y - b.extents.y - gap);
            Vector2 targetBRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y - b.extents.y - gap);

            // Controlling: Set positions for snappiness
            if (state == InteractorState.Controlling)
            {
                cursorContainer.position = targetPos;
                cursorCornerTL.transform.position = targetTLPos;
                cursorCornerTR.transform.position = targetTRPos;
                cursorCornerBL.transform.position = targetBLPos;
                cursorCornerBR.transform.position = targetBRPos;
            }

            // Hovering: Lerp around target
            else
            {
                cursorContainer.position = targetPos;
                cursorCornerTL.transform.position = Vector2.Lerp(cursorCornerTL.transform.position, targetTLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerTR.transform.position = Vector2.Lerp(cursorCornerTR.transform.position, targetTRPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBL.transform.position = Vector2.Lerp(cursorCornerBL.transform.position, targetBLPos, Time.deltaTime * cursorHoverMovementSpeed);
                cursorCornerBR.transform.position = Vector2.Lerp(cursorCornerBR.transform.position, targetBRPos, Time.deltaTime * cursorHoverMovementSpeed);
            }
        }

        // Calculate cursor colour and lerp
        Color cursorColor =
            (state == InteractorState.Interacting) ? cursorColorInteracting
            : (TargetInteractable != null ? TargetInteractable.CanInteract : false) ? cursorColorInteractable
            : (state == InteractorState.Controlling) ? cursorColorControl
            : (mouseDistance > maxHoverDistance) ? cursorColorFar
            : (target != null) ? cursorColorHover
            : cursorColorIdle;
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);

        // Move prompt organiser based on cursor
        promptOrganiser.transform.localPosition = Vector3.right * promptOffset;
    }

    private void UpdateInteractionsList()
    {
        // Clear organiser, add all children, update organiser
        promptOrganiser.Clear();
        if (TargetInteractable != null)
        {
            foreach (PlayerInteraction interaction in TargetInteractable.Interactions)
            {
                GameObject promptGO = Instantiate(promptPfb);
                InteractionPrompt prompt = promptGO.GetComponent<InteractionPrompt>();
                prompt.SetInteraction(interaction);
                promptOrganiser.AddChild(prompt);
            }
            promptOrganiser.UpdateChildren();
        }
    }

    private void SetTarget(CompositeObject newTarget)
    {
        // Update target composable
        if (TargetHighlightable) TargetHighlightable.Highlighted = false;
        target = newTarget;
        if (TargetHighlightable) TargetHighlightable.Highlighted = true;
        UpdateInteractionsList();
    }

    private bool SetTargetControlled(bool toControl)
    {
        if (toControl == (state == InteractorState.Controlling)) return false;

        // Begin controlling (pickup)
        if (toControl)
        {
            if (!CanControlTarget) return false;
            if (!TargetControllable.SetControlled(true)) return false;
            state = InteractorState.Controlling;
            TargetControllable.SetControlPosition(target.Position, controlForce);
            controlLimitCurrentColor = controlLimitClearColor;
            controlLimitLH.SetActive(true);
            return true;
        }

        // Try stop controlling (drop)
        else
        {
            if (state != InteractorState.Controlling) return false;
            if (!TargetControllable.SetControlled(false)) return false;
            state = InteractorState.Hovering;
            controlLimitLH.SetActive(false);
            targetDirLH.SetActive(false);
            //playerLegs.IsPointing = false; TODO
            controlDropTimer = 0.0f;
            UpdateHovering();
            return true;
        }
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
