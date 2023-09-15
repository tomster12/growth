
using System.Linq;
using UnityEngine;



public class PlayerInteractor : MonoBehaviour
{
    public enum InteractorState { NONE, HOVERING, CONTROLLING, INTERACTING };


    public static PlayerInteractor Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerLegs playerLegs;
    [SerializeField] private Transform cursorContainer;
    [SerializeField] private SpriteRenderer cursorCornerTL, cursorCornerTR, cursorCornerBL, cursorCornerBR;
    [SerializeField] private SpriteRenderer centreIndicator;
    [SerializeField] private SpriteRenderer limitedCentreIndicator;
    [SerializeField] private ChildOrganiser promptOrganiser;

    [Header("Prefabs")]
    [SerializeField] private GameObject promptPfb;
    [SerializeField] private GameObject dropPsysPfb;

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
    [SerializeField] private float controlLimitWarningDistance = 3.0f;
    [SerializeField] private float controlSlowdown = 0.4f;
    [SerializeField] private float interactSlowdown = 0.7f;
    [SerializeField] private float dropTimerMax = 1.0f;
    [SerializeField] private float indicateTimerMax = 3.0f;
    [SerializeField] private float controlLimitLerpSpeed = 3.0f;
    [SerializeField] private Color controlLimitWarningColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color controlLimitOutsideColor = new Color(0.9f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color legDirControlColor = new Color(1.0f, 1.0f, 1.0f, 0.1f);
    [SerializeField] private Color legDirInteractColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
    [SerializeField] private float promptOffset = 1.0f;

    public float InteractionEmphasis { get; set; }

    private LineHelper controlLimitLH;
    private Color controlLimitClearColor;
    private Color controlLimitCurrentColor;
    private LineHelper targetDirLH;
    private InteractorState currentState;
    private Vector2 hoverPos;
    private float hoverDistance;
    private Vector2 limitedControlDir;
    private Vector2 limitedControlPos;
    private ComposableObject targetComposable;
    private float targetDistance;
    private float dropTimer;
    private Interaction currentInteraction;
    private float indicateTimer;
    private PartControllable TargetControllable => targetComposable?.GetPart<PartControllable>();
    private PartInteractable TargetInteractable => targetComposable?.GetPart<PartInteractable>();
    private PartHighlightable TargetHighlightable => targetComposable?.GetPart<PartHighlightable>();
    private bool IsTargeting => currentState != InteractorState.NONE;
    private bool CanHover => currentState == InteractorState.NONE || currentState == InteractorState.HOVERING;
    private bool CanInteract => currentState == InteractorState.HOVERING;
    private bool CanControl => currentState == InteractorState.HOVERING && TargetControllable != null && TargetControllable.CanControl;
                

    public bool StartInteracting(Interaction interaction)
    {
        if (currentState == InteractorState.INTERACTING || !CanInteract) return false;
        currentInteraction = interaction;
        currentState = InteractorState.INTERACTING;
        return true;
    }

    public bool StopInteracting(Interaction interaction)
    {
        if (currentState != InteractorState.INTERACTING || currentInteraction != interaction) return false;
        currentInteraction = null;
        currentState = InteractorState.HOVERING;
        UpdateHovering();
        return true;
    }

 
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
        controlLimitClearColor = new Color(controlLimitWarningColor.r, controlLimitWarningColor.g, controlLimitWarningColor.b, 0.0f);
    }

    private void Start()
    {
        // Move cursor to mouse pos immediately
        hoverPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = new Vector2(hoverPos.x, hoverPos.y);

        // Hide centre indicators
        centreIndicator.gameObject.SetActive(false);
        limitedCentreIndicator.gameObject.SetActive(false);

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
        UpdateIndicating();
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
        if (CanHover)
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
                SetTarget(hoveredComposable);
                currentState = hoveredComposable == null ? InteractorState.NONE : InteractorState.HOVERING;
            }
        }
        else if (currentState == InteractorState.HOVERING)
        {
            SetTarget(null);
            currentState = InteractorState.NONE;
        }
    }

    private void UpdateControlling()
    {
        // Currently controlling
        if (currentState == InteractorState.CONTROLLING)
        {
            // Calculate mouse statistics
            Vector2 targetDir = targetComposable.Position - (Vector2)playerController.Transform.position;
            Vector2 hoverDir = hoverPos - (Vector2)playerController.Transform.position;
            targetDistance = targetDir.magnitude;
            hoverDistance = hoverDir.magnitude;
            
            // Limit control dir and set control position
            limitedControlDir = Vector2.ClampMagnitude(hoverDir, maxControlDistance);
            limitedControlPos = (Vector2)playerController.Transform.position + limitedControlDir;
            TargetControllable.SetControlPosition(limitedControlPos, controlForce);

            // Mouse outside control length so show circle
            float hoverBoundaryPct = Mathf.Min(1.0f, 1.0f - (maxControlDistance - hoverDistance) / controlLimitWarningDistance);
            float targetBoundaryPct = Mathf.Min(1.0f, 1.0f - (maxControlDistance - targetDistance) / controlLimitWarningDistance);
            float maxBoundaryPct = Mathf.Max(hoverBoundaryPct, targetBoundaryPct);
            Color targetLimitColor;
            if (maxBoundaryPct > 0.0f)
            {
                if (targetBoundaryPct == 1.0f) targetLimitColor = controlLimitOutsideColor;
                else targetLimitColor = new Color(controlLimitWarningColor.r, controlLimitWarningColor.g, controlLimitWarningColor.b, controlLimitWarningColor.a * maxBoundaryPct);
            }
            else targetLimitColor = controlLimitClearColor;
            controlLimitCurrentColor = Color.Lerp(controlLimitCurrentColor, targetLimitColor, controlLimitLerpSpeed * Time.deltaTime);
            controlLimitLH.DrawCircle(playerController.Transform.position, maxControlDistance, controlLimitCurrentColor, lineFill: LineFill.DOTTED);

            // Draw control line and point
            playerLegs.IsPointing = true;
            playerLegs.PointingLeg = 2;
            playerLegs.PointingPos = targetComposable.Position;
            targetDirLH.SetActive(true);
            Vector2 pathStart = playerLegs.GetLegEnd(playerLegs.PointingLeg);
            Vector2 pathEnd = targetComposable.Position;
            Vector3 pathStartFixed = new Vector3(pathStart.x, pathStart.y, playerController.Transform.position.z + 0.1f);
            Vector3 pathEndFixed = new Vector3(pathEnd.x, pathEnd.y, playerController.Transform.position.z + 0.1f);
            Color col = legDirControlColor;
            col.a *= 0.1f + 0.9f * Mathf.Max(1.0f - targetDistance / maxControlDistance, 0.0f);
            targetDirLH.DrawLine(pathStartFixed, pathEndFixed, col);

            // Drop when far away
            if (targetBoundaryPct == 1.0f)
            {
                dropTimer += Time.deltaTime;
                if (dropTimer > dropTimerMax)
                {
                    Vector3 dropPos = targetComposable.Position;
                    if (SetTargetControlled(false))
                    {
                        GameObject particlesGO = Instantiate(dropPsysPfb);
                        particlesGO.transform.position = dropPos;
                    }
                }
            }
            else dropTimer = 0.0f;

            // Drop object on click
            if (Input.GetMouseButtonDown(0)) SetTargetControlled(false);
        }

        // Not controlling
        else
        {
            // Can control and clicked so pickup object
            if (CanControl && Input.GetMouseButtonDown(0)) SetTargetControlled(true);
        }
    }

    private void UpdateInteracting()
    {
        if (currentState == InteractorState.HOVERING || currentState == InteractorState.INTERACTING)
        {
            TargetInteractable?.UpdateInteracting();
        }
    }

    private void UpdateIndicating()
    {
        indicateTimer = Mathf.Max(0.0f, indicateTimer - Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Tab)) indicateTimer = indicateTimerMax;
        PartIndicatable.ShowIndicators = indicateTimer > 0.0f;
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        FixedUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        // Is idling so set to base square
        if (currentState == InteractorState.NONE)
        {
            cursorContainer.position = new Vector2(Mathf.Round(hoverPos.x * 12) / 12, Mathf.Round(hoverPos.y * 12) / 12);
            cursorCornerTL.transform.localPosition = Vector2.Lerp(cursorCornerTL.transform.localPosition, new Vector2(-cursorIdleDistance, cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerTR.transform.localPosition = Vector2.Lerp(cursorCornerTR.transform.localPosition, new Vector2(cursorIdleDistance, cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBL.transform.localPosition = Vector2.Lerp(cursorCornerBL.transform.localPosition, new Vector2(-cursorIdleDistance, -cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBR.transform.localPosition = Vector2.Lerp(cursorCornerBR.transform.localPosition, new Vector2(cursorIdleDistance, -cursorIdleDistance), Time.fixedDeltaTime * cursorIdleMovementSpeed);
        }

        // Controlling so set centre
        centreIndicator.gameObject.SetActive(currentState == InteractorState.CONTROLLING);
        limitedCentreIndicator.gameObject.SetActive(currentState == InteractorState.CONTROLLING && hoverDistance > maxControlDistance);
        if (currentState == InteractorState.CONTROLLING)
        {
            centreIndicator.transform.position = new Vector2(hoverPos.x, hoverPos.y);

            // Limiting position so draw indicator
            if (hoverDistance > maxControlDistance)
            {
                limitedCentreIndicator.transform.position = limitedControlPos;
            }
        }
    }

    private void LateUpdate()
    {
        if (GameManager.IsPaused) return;
        LateUpdateCursor();
    }

    private void LateUpdateCursor()
    {
        // Irrelevant of state if targetting surround
        if (targetComposable != null)
        {
            // Calculate targets
            Bounds b = targetComposable.Bounds;
            Vector2 targetPos = b.center;
            float gap = currentState == InteractorState.CONTROLLING ? cursorControlGap : (cursorHoverGap * (1.0f - InteractionEmphasis));
            Vector2 targetTLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y + b.extents.y + gap);
            Vector2 targetTRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y + b.extents.y + gap);
            Vector2 targetBLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y - b.extents.y - gap);
            Vector2 targetBRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y - b.extents.y - gap);

            // Controlling so set positions
            if (currentState == InteractorState.CONTROLLING)
            {
                cursorContainer.position = targetPos;
                cursorCornerTL.transform.position = targetTLPos;
                cursorCornerTR.transform.position = targetTRPos;
                cursorCornerBL.transform.position = targetBLPos;
                cursorCornerBR.transform.position = targetBRPos;
            }

            // Just hovering so lerp positions
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
            currentState == InteractorState.INTERACTING ? cursorColorInteracting
            : GetIfCanInteract() ? cursorColorInteractable
            : currentState == InteractorState.CONTROLLING ? cursorColorControl
            : hoverDistance > maxHoverDistance ? cursorColorFar
            : targetComposable != null ? cursorColoHover
            : cursorColorIdle;

        // Lerp colours
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);

        // Move prompt organiser
        promptOrganiser.transform.localPosition = Vector3.right * promptOffset;
    }

    private void UpdateInteractionsList()
    {
        // Clear organiser, add all children, update organiser
        promptOrganiser.Clear();
        if (TargetInteractable != null)
        {
            foreach (Interaction interaction in TargetInteractable.Interactions)
            {
                GameObject promptGO = Instantiate(promptPfb);
                Prompt prompt = promptGO.GetComponent<Prompt>();
                prompt.SetInteraction(interaction);
                promptOrganiser.AddChild(prompt);
            }
            promptOrganiser.UpdateChildren();
        }
    }

    private bool GetIfCanInteract()
    {
        if (targetComposable == null) return false;
        if (TargetInteractable == null) return false;
        return TargetInteractable.CanInteract;
    }

    private void SetTarget(ComposableObject newTarget)
    {
        // Update target composable
        TargetHighlightable?.SetHighlighted(false);
        targetComposable = newTarget;
        TargetHighlightable?.SetHighlighted(true);
        UpdateInteractionsList();
    }

    private bool SetTargetControlled(bool toControl)
    {
        if (toControl == (currentState == InteractorState.CONTROLLING)) return false;

        // Begin controlling (pickup)
        if (toControl)
        {
            if (!CanControl) return false;
            if (!TargetControllable.SetControlled(true)) return false;
            currentState = InteractorState.CONTROLLING;
            TargetControllable.SetControlPosition(targetComposable.Position, controlForce);
            controlLimitCurrentColor = controlLimitClearColor;
            controlLimitLH.SetActive(true);
            return true;
        }

        // Try stop controlling (drop)
        else
        {
            if (currentState != InteractorState.CONTROLLING) return false;
            if (!TargetControllable.SetControlled(false)) return false;
            currentState = InteractorState.HOVERING;
            controlLimitLH.SetActive(false);
            targetDirLH.SetActive(false);
            playerLegs.IsPointing = false;
            dropTimer = 0.0f;
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


    public class Interaction
    {
        public enum Visibility { HIDDEN, INPUT, ICON, TEXT }

        public bool IsEnabled { get; protected set; }
        public bool IsActive { get; protected set; }
        public bool CanInteract { get; protected set; }
        public string Name { get; private set; }
        public InteractionInput Input { get; private set; }
        public Visibility VisibilityState { get; protected set; }

        private Sprite blockedSprite;
        private Sprite spriteInputInactive;
        private Sprite spriteInputActive;
        private Sprite spriteIconInactive;
        private Sprite spriteIconActive;

        protected PlayerInteractor PlayerInteractor => PlayerInteractor.Instance;
        protected PlayerController PlayerController => PlayerInteractor.playerController;
        protected PlayerLegs PlayerLegs => PlayerInteractor.playerLegs;
        protected ComposableObject TargetComposable => PlayerInteractor.targetComposable;
        protected LineHelper TargetDirLH => PlayerInteractor.targetDirLH;
        protected Color LegDirInteractColor => PlayerInteractor.legDirInteractColor;
        protected float InteractSlowdown => PlayerInteractor.interactSlowdown;


        public Interaction(string name, InteractionInput input, Visibility visibility, string iconSpriteName)
        {
            IsEnabled = true;
            IsActive = false;
            CanInteract = true;

            this.Name = name;
            this.Input = input;
            this.VisibilityState = visibility;

            blockedSprite = SpriteSet.Instance.GetSprite("cross");
            spriteInputInactive = SpriteSet.Instance.GetSprite(this.Input.name + "_inactive");
            spriteInputActive  = SpriteSet.Instance.GetSprite(this.Input.name + "_active");
            if (iconSpriteName != null)
            {
                spriteIconInactive = SpriteSet.Instance.GetSprite(iconSpriteName + "_inactive");
                spriteIconActive = SpriteSet.Instance.GetSprite(iconSpriteName + "_active");
            }
        }


        public void UpdateInteracting()
        {
            if (!IsEnabled) return;
            PollPlayerInput();
            if (IsActive) UpdateAction();
        }

        public Sprite GetCurrentSpriteInput()
        {
            if (!CanInteract) return blockedSprite;
            if (!IsActive) return spriteInputInactive;
            return spriteInputActive;
        }

        public Sprite GetCurrentSpriteIcon()
        {
            if (!CanInteract) return blockedSprite;
            if (!IsActive) return spriteIconInactive;
            return spriteIconActive;
        }


        private bool PollPlayerInput()
        {
            if (Input.CheckInputDown()) OnInputDown();
            else if (Input.CheckInput()) OnHold();
            else if (Input.CheckInputUp()) OnInputUp();
            else return false;
            return true;
        }

        protected virtual void OnHold() { }

        protected virtual void OnInputDown() { }

        protected virtual void OnInputUp() { }

        protected virtual void UpdateAction() { }
    }
}
