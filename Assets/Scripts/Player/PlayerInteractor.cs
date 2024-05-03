using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public partial class PlayerInteractor : MonoBehaviour, IInteractor
{
    public enum TargetState
    { None, Hovering, Controlling, Interacting }

    public static PlayerInteractor Instance { get; private set; }

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

    private Vector2 inputMousePos;
    private float inputMouseDistance;

    private TargetState targetState;
    private CompositeObject target;
    private float targetDistance;
    private LineHelper targetDirLH;
    private LineHelper targetControlLimitLH;
    private int targetControllingLeg;
    private Color targetControlLimitColor;
    private Vector2 targetLimitedControlDir;
    private Vector2 targetLimitedControlPos;
    private float targetControlDropTimer;
    private Interaction currentInteraction;

    private float CursorSqueeze => interactionCursorSqueeze;
    private PartControllable TargetControllable => target?.GetPart<PartControllable>();
    private PartInteractable TargetInteractable => target?.GetPart<PartInteractable>();
    private PartHighlightable TargetHighlightable => target?.GetPart<PartHighlightable>();
    private PartIndicatable TargetIndicatable => target?.GetPart<PartIndicatable>();

    private void Awake()
    {
        Instance = this;

        // Initialize line helpers
        GameObject controlLimitLHGO = new GameObject();
        controlLimitLHGO.transform.parent = transform;
        targetControlLimitLH = controlLimitLHGO.AddComponent<LineHelper>();

        GameObject controlDirLHGO = new GameObject();
        controlDirLHGO.transform.parent = transform;
        targetDirLH = controlDirLHGO.AddComponent<LineHelper>();

        GameObject interactionPullingLHGO = new GameObject();
        interactionPullingLHGO.transform.parent = transform;
        interactionPullingLH = interactionPullingLHGO.AddComponent<LineHelper>();
    }

    private void Start()
    {
        // Move cursor to mouse pos immediately
        inputMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorContainer.position = new Vector2(inputMousePos.x, inputMousePos.y);

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
        UpdateTargetHovering();
        UpdateTargetControlling();
        UpdateTargetInteracting();
        UpdateIndicating();
    }

    private void UpdateHandleInput()
    {
        inputMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        inputMouseDistance = (inputMousePos - (Vector2)playerMovement.Transform.position).magnitude;
    }

    private void UpdateTargetHovering()
    {
        if (targetState == TargetState.None || targetState == TargetState.Hovering)
        {
            CompositeObject hovered = null;

            // Mouse within range to hover so raycast for new target
            if (inputMouseDistance < maxHoverDistance)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(inputMousePos, Vector2.zero);
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
            if (hovered != target)
            {
                if (TargetHighlightable) TargetHighlightable.Highlighted = false;
                SetTarget(hovered);
                targetState = hovered ? TargetState.Hovering : TargetState.None;
                if (TargetHighlightable) TargetHighlightable.Highlighted = true;
                if (TargetIndicatable) TargetIndicatable.Hide();
            }
        }
    }

    private void UpdateTargetControlling()
    {
        if (targetState == TargetState.Controlling)
        {
            // Drop on mouse click
            if (Input.GetMouseButtonDown(0))
            {
                SetTargetControlled(false);
                return;
            }

            // Calculate distances
            Vector2 targetDir = target.Position - (Vector2)playerMovement.Transform.position;
            Vector2 mouseDir = inputMousePos - (Vector2)playerMovement.Transform.position;
            targetDistance = targetDir.magnitude;
            inputMouseDistance = mouseDir.magnitude;

            // Limit control dir and set control position
            targetLimitedControlDir = Vector2.ClampMagnitude(mouseDir, maxControlDistance - 0.1f);
            targetLimitedControlPos = (Vector2)playerMovement.Transform.position + targetLimitedControlDir;
            TargetControllable.SetControlPosition(targetLimitedControlPos, controlForce);

            // Mouse outside control length so show circle
            float hoverBoundaryPct = Mathf.Min(1.0f, 1.0f - (maxControlDistance - inputMouseDistance) / maxControlWarningThreshold);
            float targetBoundaryPct = Mathf.Min(1.0f, 1.0f - (maxControlDistance - targetDistance) / maxControlWarningThreshold);
            float maxBoundaryPct = Mathf.Max(hoverBoundaryPct, targetBoundaryPct);
            Color targetLimitColor;
            if (maxBoundaryPct > 0.0f)
            {
                if (targetBoundaryPct == 1.0f) targetLimitColor = controlLimitColorOutside;
                else targetLimitColor = new Color(controlLimitColorWarning.r, controlLimitColorWarning.g, controlLimitColorWarning.b, controlLimitColorWarning.a * maxBoundaryPct);
            }
            else targetLimitColor = new Color(controlLimitColorWarning.r, controlLimitColorWarning.g, controlLimitColorWarning.b, 0.0f);
            targetControlLimitColor = Color.Lerp(targetControlLimitColor, targetLimitColor, controlLimitColorLerpSpeed * Time.deltaTime);
            targetControlLimitLH.DrawCircle(playerMovement.Transform.position, maxControlDistance, targetControlLimitColor, lineFill: LineFill.Dotted);

            // Draw control line and point
            playerLegs.SetOverrideFoot(targetControllingLeg, target.Position);
            targetDirLH.SetActive(true);
            Vector2 pathStart = playerLegs.GetFootPos(targetControllingLeg);
            Vector2 pathEnd = target.Position;
            Vector3 pathStartFixed = new Vector3(pathStart.x, pathStart.y, playerMovement.Transform.position.z + 0.1f);
            Vector3 pathEndFixed = new Vector3(pathEnd.x, pathEnd.y, playerMovement.Transform.position.z + 0.1f);
            Color col = legDirControlColor;
            col.a *= 0.1f + 0.9f * Mathf.Max(1.0f - targetDistance / maxControlDistance, 0.0f);
            targetDirLH.DrawLine(pathStartFixed, pathEndFixed, col);

            // Drop on too far away timer finished
            if (targetBoundaryPct == 1.0f)
            {
                targetControlDropTimer += Time.deltaTime;
                if (targetControlDropTimer > controlDropTimerDuration)
                {
                    Vector3 dropPos = target.Position;
                    GameObject particlesGO = Instantiate(dropPsysPfb);
                    particlesGO.transform.position = dropPos;
                    SetTargetControlled(false);
                    return;
                }
            }
            else targetControlDropTimer = 0.0f;
        }

        // If not controlling then check for control input
        else if (targetState == TargetState.Hovering
            && TargetControllable != null
            && TargetControllable.CanControl
            && Input.GetMouseButtonDown(0))
        {
            SetTargetControlled(true);
        }
    }

    private void UpdateTargetInteracting()
    {
        // Currently interacting so update
        if (targetState == TargetState.Interacting)
        {
            if (currentInteraction.IsActive && (PollInput(currentInteraction.Input) == InputEvent.Inactive))
            {
                currentInteraction.StopInteracting();
            }
            if (!currentInteraction.IsActive)
            {
                targetState = TargetState.Hovering;
                OnInteractionFinished();
            }
        }

        // Hovering interactable object so check for interaction
        else if (targetState == TargetState.Hovering && TargetInteractable != null)
        {
            foreach (Interaction interaction in TargetInteractable.Interactions)
            {
                // Can interact and input polled down so begin interaction
                if (interaction.CanInteract && (PollInput(interaction.Input) == InputEvent.Active))
                {
                    targetState = TargetState.Interacting;
                    currentInteraction = interaction;
                    interaction.StartInteracting(this);
                    break;
                }
            }
        }
    }

    private void UpdateIndicating()
    {
        // Show indicators on tab press of nearby objects
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
        if (targetState == TargetState.None)
        {
            cursorContainer.position = new Vector2(Mathf.Round(inputMousePos.x * 12) / 12, Mathf.Round(inputMousePos.y * 12) / 12);
            cursorCornerTL.transform.localPosition = Vector2.Lerp(cursorCornerTL.transform.localPosition, new Vector2(-cursorIdleSize, cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerTR.transform.localPosition = Vector2.Lerp(cursorCornerTR.transform.localPosition, new Vector2(cursorIdleSize, cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBL.transform.localPosition = Vector2.Lerp(cursorCornerBL.transform.localPosition, new Vector2(-cursorIdleSize, -cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
            cursorCornerBR.transform.localPosition = Vector2.Lerp(cursorCornerBR.transform.localPosition, new Vector2(cursorIdleSize, -cursorIdleSize), Time.fixedDeltaTime * cursorIdleMovementSpeed);
        }

        // Controlling: Update needed indicators to mouse position
        if (targetState == TargetState.Controlling)
        {
            centreIndicator.gameObject.SetActive(true);
            centreIndicator.transform.position = new Vector2(inputMousePos.x, inputMousePos.y);
            if (inputMouseDistance > maxControlDistance)
            {
                limitedCentreIndicator.gameObject.SetActive(true);
                limitedCentreIndicator.transform.position = targetLimitedControlPos;
            }
            else limitedCentreIndicator.gameObject.SetActive(false);
        }
        else
        {
            centreIndicator.gameObject.SetActive(false);
            limitedCentreIndicator.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (GameManager.IsPaused) return;
        LateUpdateCursor();
    }

    private void LateUpdateCursor()
    {
        // Targeting object so surround with cursor
        if (target != null)
        {
            Bounds b = target.Bounds;
            Vector2 targetPos = b.center;
            float gap = targetState == TargetState.Controlling ? cursorControlGap : (cursorHoverGap * (1.0f - CursorSqueeze));
            Vector2 targetTLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y + b.extents.y + gap);
            Vector2 targetTRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y + b.extents.y + gap);
            Vector2 targetBLPos = new Vector2(b.center.x - b.extents.x - gap, b.center.y - b.extents.y - gap);
            Vector2 targetBRPos = new Vector2(b.center.x + b.extents.x + gap, b.center.y - b.extents.y - gap);

            // Controlling: Set positions for snappiness
            if (targetState == TargetState.Controlling)
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
            (targetState == TargetState.Interacting) ? cursorColorInteracting
            : (TargetInteractable != null ? TargetInteractable.CanInteract : false) ? cursorColorInteractable
            : (targetState == TargetState.Controlling) ? cursorColorControl
            : (inputMouseDistance > maxHoverDistance) ? cursorColorFar
            : (target != null) ? cursorColorHover
            : cursorColorIdle;
        cursorCornerTL.color = Color.Lerp(cursorCornerTL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerTR.color = Color.Lerp(cursorCornerTR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBL.color = Color.Lerp(cursorCornerBL.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);
        cursorCornerBR.color = Color.Lerp(cursorCornerBR.color, cursorColor, Time.deltaTime * cursorColorLerpSpeed);

        // Move prompt organiser based on cursor
        promptOrganiser.transform.localPosition = Vector3.right * promptOffset;
    }

    private void SetTarget(CompositeObject newTarget)
    {
        // Update target composable
        target = newTarget;

        // Update prompt organiser with new interactions
        promptOrganiser.Clear();
        if (TargetInteractable != null)
        {
            foreach (Interaction interaction in TargetInteractable.Interactions)
            {
                GameObject promptGO = Instantiate(promptPfb);
                InteractionPrompt prompt = promptGO.GetComponent<InteractionPrompt>();
                prompt.SetInteraction(interaction);
                promptOrganiser.AddChild(prompt);
            }
            promptOrganiser.UpdateChildren();
        }
    }

    private void SetTargetControlled(bool toControl)
    {
        if (toControl == (targetState == TargetState.Controlling)) return;
        Assert.IsNotNull(TargetControllable);

        // Begin controlling
        if (toControl)
        {
            Assert.IsTrue(TargetControllable.CanControl);
            TargetControllable.SetControlled(true);
            targetState = TargetState.Controlling;
            TargetControllable.SetControlPosition(target.Position, controlForce);
            targetControlLimitColor = new Color(controlLimitColorWarning.r, controlLimitColorWarning.g, controlLimitColorWarning.b, 0.0f);
            targetControlLimitLH.SetActive(true);

            // Set controlling leg
            Vector2 targetDir = target.Position - (Vector2)playerMovement.Transform.position;
            float rightPct = Vector2.Dot(playerMovement.GroundRightDir, targetDir);
            targetControllingLeg = rightPct > 0.0f ? 2 : 1;
        }

        // Stop controlling
        else
        {
            TargetControllable.SetControlled(false);
            targetState = TargetState.Hovering;
            targetControlLimitLH.SetActive(false);
            targetDirLH.SetActive(false);
            playerLegs.UnsetOverrideFoot(targetControllingLeg);
            targetControlDropTimer = 0.0f;
            UpdateTargetHovering();
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

public partial class PlayerInteractor // IInteractor
{
    private float interactionCursorSqueeze = 0.0f;
    private bool interactionIsPulling = false;
    private LineHelper interactionPullingLH;
    private int interactionPullingLeg = 0;

    public InputEvent PollInput(InteractionInput input)
    {
        switch (input.Type)
        {
            case InputType.Key:
                if (Input.GetKey(input.Code)) return InputEvent.Active;
                return InputEvent.Inactive;

            case InputType.Mouse:
                if (Input.GetMouseButton(input.MouseButton)) return InputEvent.Active;
                return InputEvent.Inactive;

            default:
                return InputEvent.Inactive;
        }
    }

    public void SetInteractionEmphasis(float amount) => interactionCursorSqueeze = amount;

    public void SetInteractionSlowdown(float amount) => playerMovement.SetMovementSlowdown = amount;

    public void SetInteractionPulling(Vector2 target, float amount)
    {
        Vector2 player2 = (Vector2)playerMovement.Transform.position;

        // Not currently pulling so set variables
        if (!interactionIsPulling)
        {
            interactionIsPulling = true;
            float rightPct = Vector2.Dot(playerMovement.GroundRightDir, target - player2);
            interactionPullingLeg = rightPct > 0.0f ? 2 : 1;
            interactionPullingLH.SetActive(true);
        }

        // Calculate pulling line positions then draw
        Vector2 start2 = playerLegs.GetFootPos(interactionPullingLeg);
        Vector2 controlDir2 = target - player2;
        float controlUpAmount2 = 0.2f + 0.2f * Utility.Easing.EaseOutCubic(amount);
        Vector2 controlUp2 = playerMovement.GroundUpDir.normalized * (target - player2).magnitude * controlUpAmount2;
        Vector2 controlPoint2 = player2 + controlDir2 * 0.75f + controlUp2;
        Vector3 pathStart = new Vector3(start2.x, start2.y, playerMovement.Transform.position.z + 0.1f);
        Vector3 pathEnd = new Vector3(target.x, target.y, playerMovement.Transform.position.z + 0.1f);
        Vector3 controlPoint = new Vector3(controlPoint2.x, controlPoint2.y, playerMovement.Transform.position.z + 0.1f);
        interactionPullingLH.DrawCurve(pathStart, pathEnd, controlPoint, legDirInteractColor);

        // Set leg to override and point to control point
        playerLegs.SetOverrideFoot(interactionPullingLeg, controlPoint);

        // Lean as to show strength of pull
        playerMovement.SetVerticalLean = 1.0f;
    }

    private void OnInteractionFinished()
    {
        // Clean up interaction variables
        currentInteraction = null;
        interactionCursorSqueeze = 0.0f;
        playerMovement.SetMovementSlowdown = 0.0f;
        if (interactionIsPulling)
        {
            interactionIsPulling = false;
            interactionPullingLH.SetActive(false);
            playerLegs.UnsetOverrideFoot(interactionPullingLeg);
            playerMovement.SetVerticalLean = 0;
        }
    }
}
