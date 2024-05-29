using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public partial class PlayerController : MonoBehaviour, IInteractor
{
    public enum TargettingState
    { None, Hovering, Controlling, Interacting }

    public static PlayerController Instance { get; private set; }

    public Action OnMoveEvent { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLegs playerLegs;
    [SerializeField] private PlayerCursor playerCursor;
    [SerializeField] private ChildOrganiser promptOrganiser;

    [Header("Prefabs")]
    [SerializeField] private GameObject promptPfb;
    [SerializeField] private GameObject dropPsysPfb;

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

    [Header("Cursor Config")]
    [SerializeField] private float cursorHoverGap = 0.2f;
    [SerializeField] private float cursorControlGap = 0.05f;
    [SerializeField] private Color cursorColorFar = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color cursorColorIdle = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color cursorColorHover = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color cursorColorControl = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private Color cursorColorInteractable = new Color(1.0f, 0.3f, 0.3f);
    [SerializeField] private Color cursorColorInteracting = new Color(0.5f, 0.5f, 0.5f);

    private Vector2 inputMousePosition;
    private float inputMouseDistance;
    private TargettingState targetingState;
    private CompositeObject targettingCO;
    private float targetDistance;
    private LineHelper targetControlLH;
    private LineHelper targetControlLimitLH;
    private int targetControlLeg;
    private Color targetControlLimitColor;
    private Vector2 targetControlLimitedDir;
    private Vector2 targetControlLimitedPos;
    private float targetControlDropTimer;
    private Interaction currentInteraction;
    private CompositeObject equippedCO;
    private int equippedLeg;
    private float CursorSqueeze => interactionCursorSqueeze;
    private bool CanControl => IsTargeting && targettingCO.HasPart<PartControllable>() && targettingCO.GetPart<PartControllable>().CanControl;
    private bool CanEquip => IsTargeting && targettingCO.HasPart<PartEquipable>() && targettingCO.GetPart<PartEquipable>().CanEquip;
    private bool CanInteractAny => IsTargeting && targettingCO.HasPart<PartInteractable>() && targettingCO.GetPart<PartInteractable>().CanInteractAny(this);
    private bool IsTargeting => targetingState != TargettingState.None;
    private bool IsEquipped => equippedCO != null;

    private void Awake()
    {
        Instance = this;

        // Initialize line helpers
        GameObject controlLimitLHGO = new GameObject();
        controlLimitLHGO.transform.parent = transform;
        targetControlLimitLH = controlLimitLHGO.AddComponent<LineHelper>();

        GameObject controlDirLHGO = new GameObject();
        controlDirLHGO.transform.parent = transform;
        targetControlLH = controlDirLHGO.AddComponent<LineHelper>();

        GameObject interactionPullingLHGO = new GameObject();
        interactionPullingLHGO.transform.parent = transform;
        interactionPullingLH = interactionPullingLHGO.AddComponent<LineHelper>();

        // Init cursor, snap cursor to current mouse pos
        playerCursor.InitIndicators(2);
        inputMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        playerCursor.SetTargetPosition(inputMousePosition, true);

        // Listen to zoom change and update cursor
        PlayerCamera.OnZoomChangeEvent += (zoom) => { UpdateCursor(); };
    }

    private void Update()
    {
        if (GameManager.IsPaused) return;
        HandleInput();
        UpdateTargetHovering();
        UpdateTargetControlling();
        UpdateTargetInteracting();
        UpdateEquipped();
        UpdateIndicating();
        UpdateCursor();
    }

    private void HandleInput(bool adjust = true)
    {
        inputMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        inputMouseDistance = (inputMousePosition - (Vector2)playerMovement.Transform.position).magnitude;
    }

    private void UpdateTargetHovering()
    {
        // Handle retargetting if [None] or [Hovering]
        if (targetingState == TargettingState.None || targetingState == TargettingState.Hovering)
        {
            CompositeObject hovered = null;

            // Mouse within range to hover so raycast for new target
            if (inputMouseDistance < maxHoverDistance)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(inputMousePosition, Vector2.zero);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.transform.TryGetComponent<CompositeObject>(out var hitComposable))
                    {
                        if (!CanTarget(hitComposable)) continue;
                        hovered = hitComposable;
                        break;
                    }
                }
            }

            // Update with new target which can be null
            if (hovered != targettingCO)
            {
                targettingCO?.GetPart<PartHighlightable>()?.SetHighlighted(false);
                SetTarget(hovered);
                targetingState = hovered ? TargettingState.Hovering : TargettingState.None;
                targettingCO?.GetPart<PartHighlightable>()?.SetHighlighted(true);
                targettingCO?.GetPart<PartIndicatable>()?.Hide();
            }
        }
    }

    private void UpdateTargetControlling()
    {
        // [Hovering] and LMB down so begin controlling
        if (targetingState == TargettingState.Hovering && CanControl && Input.GetMouseButtonDown(0))
        {
            StartControlling();
            return;
        }

        // Currently [Controlling] so update
        else if (targetingState == TargettingState.Controlling)
        {
            // Drop on LMB click
            if (Input.GetMouseButtonDown(0))
            {
                StopControlling();
                return;
            }

            // Calculate distances
            Vector2 targetDir = targettingCO.Position - (Vector2)playerMovement.Transform.position;
            Vector2 mouseDir = inputMousePosition - (Vector2)playerMovement.Transform.position;
            targetDistance = targetDir.magnitude;
            inputMouseDistance = mouseDir.magnitude;

            // Limit control dir and set control position
            targetControlLimitedDir = Vector2.ClampMagnitude(mouseDir, maxControlDistance - 0.1f);
            targetControlLimitedPos = (Vector2)playerMovement.Transform.position + targetControlLimitedDir;
            targettingCO.GetPart<PartControllable>().SetControlPosition(targetControlLimitedPos, controlForce);

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
            playerLegs.SetOverrideLeg(targetControlLeg, targettingCO.Position);
            Vector2 pathStart = playerLegs.GetFootPos(targetControlLeg);
            Vector2 pathEnd = targettingCO.Position;
            float z = playerMovement.Transform.position.z + 0.1f;
            Color col = legDirControlColor;
            col.a *= 0.1f + 0.9f * Mathf.Max(1.0f - targetDistance / maxControlDistance, 0.0f);
            targetControlLH.DrawLine(Utility.WithZ(pathStart, z), Utility.WithZ(pathEnd, z), col);

            // Drop on too far away timer finished
            if (targetBoundaryPct == 1.0f)
            {
                targetControlDropTimer += Time.deltaTime;
                if (targetControlDropTimer > controlDropTimerDuration)
                {
                    Vector3 dropPos = targettingCO.Position;
                    GameObject particlesGO = Instantiate(dropPsysPfb);
                    particlesGO.transform.position = dropPos;
                    StopControlling();
                    return;
                }
            }
            else targetControlDropTimer = 0.0f;
        }
    }

    private void UpdateTargetInteracting()
    {
        // Currently [Interacting] so check for finished
        if (targetingState == TargettingState.Interacting)
        {
            if (currentInteraction.IsActive && (PollInput(currentInteraction.RequiredInput) == InputEvent.Inactive))
            {
                currentInteraction.StopInteracting();
            }
            if (!currentInteraction.IsActive)
            {
                targetingState = TargettingState.Hovering;
                OnInteractionFinished();
            }
        }

        // [Hovering] so check for interactions
        else if (targetingState == TargettingState.Hovering && CanInteractAny)
        {
            foreach (Interaction interaction in targettingCO.GetPart<PartInteractable>().Interactions)
            {
                // Can interact and input polled down begin interaction
                if (interaction.CanInteract(this) && (PollInput(interaction.RequiredInput) == InputEvent.Active))
                {
                    targetingState = TargettingState.Interacting;
                    currentInteraction = interaction;
                    interaction.StartInteracting(this);
                    break;
                }
            }
        }
    }

    private void UpdateEquipped()
    {
        // [Controlling] RMB down so equip
        if (targetingState == TargettingState.Controlling && CanEquip && Input.GetMouseButtonDown(1))
        {
            StartEquipping();
            return;
        }

        // [Equipped] So check for dropping
        else if (IsEquipped && Input.GetMouseButtonDown(1))
        {
            StopEquipping();
            return;
        }

        // [Equipped] Set leg position
        else if (IsEquipped)
        {
            if (playerMovement.IsGrounded)
            {
                // Calculate leg dir based on angle
                Vector2 legTarget;
                Vector2 playerPos = (Vector2)playerMovement.Transform.position;
                Vector2 gripDir = inputMousePosition - playerPos;
                float gripAngle = Vector2.SignedAngle(Vector2.up, gripDir);
                float gripAngleAbs = Mathf.Abs(gripAngle);
                float legLength = playerLegs.LegLengths[equippedLeg];
                float legSpacing = playerLegs.LegSpacing * -Mathf.Sign(gripAngle);

                // 105+ - 80: Lerp between soft side angles
                if (gripAngleAbs > 80.0f)
                {
                    float lerp = 1.0f - (Mathf.Min(105.0f, gripAngleAbs) - 80.0f) / 25.0f;
                    lerp = Utility.Easing.EaseOutSine(lerp);
                    float lerpSpacing = Mathf.Lerp(legSpacing * 3.6f, legSpacing * 2.0f, lerp);
                    float lerpLength = Mathf.Lerp(legLength * 0.45f, legLength * 0.6f, lerp);
                    legTarget = playerPos;
                    legTarget += Vector2.right * lerpSpacing + Vector2.up * lerpLength;
                }

                // 70 - 35: Lerp between soft and direct
                else if (gripAngleAbs > 35.0f)
                {
                    float lerp = 1.0f - (gripAngleAbs - 35.0f) / 35.0f;
                    lerp = Utility.Easing.EaseInSine(lerp);
                    Vector2 lerpFrom = playerPos + Vector2.right * legSpacing * 2.0f + Vector2.up * legLength * 0.6f;
                    Vector2 lerpTo = playerPos + (inputMousePosition - playerPos).normalized * legLength * 1.1f;
                    legTarget = Vector2.Lerp(lerpFrom, lerpTo, lerp);
                }

                // 35 - 0: Direct
                else legTarget = inputMousePosition;

                // Set leg target
                playerLegs.SetOverrideLeg(equippedLeg, legTarget);
            }

            // Not grounded so point directly to mouse
            else playerLegs.SetOverrideLeg(equippedLeg, inputMousePosition);

            // [Equipped] Update grip position
            if (IsEquipped)
            {
                Vector2 gripPos = playerLegs.GetFootPos(equippedLeg);
                Vector2 gripDir = inputMousePosition - gripPos;
                equippedCO.GetPart<PartEquipable>().SetGrip(gripPos, gripDir);
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
                if (obj == targettingCO) continue;
                if (obj == equippedCO) continue;
                var part = obj.GetPart<PartIndicatable>();
                float dist = (obj.Position - (Vector2)playerMovement.Transform.position).magnitude;
                if (dist < indicateDistance) StartCoroutine(showIndicator(part, dist / indicateSpeed, indicateTimerMax));
            }
        }
    }

    private void UpdateCursor()
    {
        // Follow controlled object in Update()
        if (targetingState == TargettingState.Controlling)
        {
            playerCursor.SetTargetBounds(targettingCO.Bounds, cursorControlGap * (1.0f - CursorSqueeze), true);
            playerCursor.SetIndicator(0, true, inputMousePosition);
            playerCursor.SetIndicator(1, true, targetControlLimitedPos);
            playerCursor.SetCornerColor(cursorColorControl);
            playerCursor.SetIndicatorColor(0, cursorColorIdle);
            playerCursor.SetIndicatorColor(1, cursorColorFar);
        }
        if (targetingState == TargettingState.Hovering || targetingState == TargettingState.Interacting)
        {
            playerCursor.SetTargetBounds(targettingCO.Bounds, cursorHoverGap * (1.0f - CursorSqueeze), false);
            playerCursor.SetIndicator(0, false);
            playerCursor.SetIndicator(1, false);
            if (targetingState == TargettingState.Interacting) playerCursor.SetCornerColor(cursorColorInteracting);
            else if (CanInteractAny) playerCursor.SetCornerColor(cursorColorInteractable);
            else playerCursor.SetCornerColor(cursorColorHover);
        }
    }

    private void FixedUpdate()
    {
        FixedUpdateCursor();
    }

    private void FixedUpdateCursor()
    {
        if (targetingState == TargettingState.None)
        {
            playerCursor.SetTargetPosition(inputMousePosition, true);
            playerCursor.SetIndicator(0, false);
            playerCursor.SetIndicator(1, false);
            if (inputMouseDistance < maxHoverDistance) playerCursor.SetCornerColor(cursorColorIdle);
            else playerCursor.SetCornerColor(cursorColorFar);
        }
    }

    private void SetTarget(CompositeObject newTarget)
    {
        // Update target composable
        targettingCO = newTarget;

        // Update prompt organiser with new interactions
        promptOrganiser.Clear();
        if (targettingCO != null && targettingCO.HasPart<PartInteractable>())
        {
            foreach (Interaction interaction in targettingCO.GetPart<PartInteractable>().Interactions)
            {
                GameObject promptGO = Instantiate(promptPfb);
                InteractionPrompt prompt = promptGO.GetComponent<InteractionPrompt>();
                prompt.SetInteraction(this, interaction);
                promptOrganiser.AddChild(prompt);
            }
            promptOrganiser.UpdateChildren();
        }
    }

    private void StartControlling()
    {
        if (!CanControl) throw new System.Exception("Cannot control target.");
        targetingState = TargettingState.Controlling;
        if (!targettingCO.GetPart<PartControllable>().StartControlling()) throw new System.Exception("Failed to control target.");
        targettingCO.GetPart<PartControllable>().SetControlPosition(targettingCO.Position, controlForce);
        targetControlLH.SetActive(true);
        targetControlLimitLH.SetActive(true);
        targetControlLimitColor = new Color(controlLimitColorWarning.r, controlLimitColorWarning.g, controlLimitColorWarning.b, 0.0f);

        // Set controlling leg
        Vector2 targetDir = targettingCO.Position - (Vector2)playerMovement.Transform.position;
        float rightPct = Vector2.Dot(playerMovement.GroundRightDir, targetDir);
        if (equippedCO != null) targetControlLeg = equippedLeg == 1 ? 2 : 1;
        else targetControlLeg = rightPct > 0.0f ? 2 : 1;
    }

    private void StopControlling()
    {
        if (targetingState != TargettingState.Controlling) throw new System.Exception("Not controlling target.");
        targetingState = TargettingState.Hovering;
        if (!targettingCO.GetPart<PartControllable>().StopControlling()) throw new System.Exception("Failed to stop controlling target.");
        targetControlLH.SetActive(false);
        targetControlLimitLH.SetActive(false);
        playerLegs.UnsetOverrideLeg(targetControlLeg);
        targetControlDropTimer = 0.0f;
        UpdateTargetHovering();
    }

    private void StartEquipping()
    {
        if (!CanEquip) throw new System.Exception("Cannot equip target.");
        equippedCO = targettingCO;
        equippedLeg = targetControlLeg;
        StopControlling();
        if (!equippedCO.GetPart<PartEquipable>().StartEquipping()) throw new System.Exception("Failed to equip target.");
    }

    private void StopEquipping()
    {
        if (!IsEquipped) throw new System.Exception("No equipped object to unequip.");
        if (!equippedCO.GetPart<PartEquipable>().StopEquipping()) throw new System.Exception("Failed to unequip target.");
        playerLegs.UnsetOverrideLeg(equippedLeg);
        equippedCO = null;
        equippedLeg = -1;
    }

    private bool CanTarget(CompositeObject compositeObject)
    {
        if (equippedCO == compositeObject) return false;
        return compositeObject.CanTarget;
    }

    private void OnDrawGizmos()
    {
        // Draw inputMousePosition
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(inputMousePosition, 0.1f);
    }
}

public partial class PlayerController // IInteractor
{
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
        Vector2 playerPos = (Vector2)playerMovement.Transform.position;

        // Not currently pulling so set variables
        if (!interactionIsPulling)
        {
            interactionIsPulling = true;
            float rightPct = Vector2.Dot(playerMovement.GroundRightDir, target - playerPos);
            interactionPullingLeg = rightPct > 0.0f ? 2 : 1;
            interactionPullingLH.SetActive(true);
        }

        // Calculate pulling line positions
        Vector2 start = playerLegs.GetFootPos(interactionPullingLeg);
        Vector2 controlDir = target - playerPos;
        float controlUpAmount = 0.2f + 0.2f * Utility.Easing.EaseOutCubic(amount);
        Vector2 controlUp = playerMovement.GroundUpDir.normalized * (target - playerPos).magnitude * controlUpAmount;
        Vector2 controlPoint = playerPos + controlDir * 0.75f + controlUp;

        // Draw in 3D world land at correct z
        float z = playerMovement.Transform.position.z + 0.1f;
        Vector3 pathStart3 = Utility.WithZ(start, z);
        Vector3 pathEnd3 = Utility.WithZ(target, z);
        Vector3 controlPoint3 = Utility.WithZ(controlPoint, z);
        interactionPullingLH.DrawCurve(pathStart3, pathEnd3, controlPoint3, legDirInteractColor);

        // Set leg to override and point to control point
        playerLegs.SetOverrideLeg(interactionPullingLeg, controlPoint3);

        // Lean as to show strength of pull
        playerMovement.SetVerticalLean = 1.0f;
    }

    public ToolType GetInteractorToolType()
    {
        if (IsEquipped) return equippedCO.GetPart<PartEquipable>().ToolType;
        return ToolType.None;
    }

    private float interactionCursorSqueeze = 0.0f;
    private bool interactionIsPulling = false;
    private LineHelper interactionPullingLH;
    private int interactionPullingLeg = 0;

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
            playerLegs.UnsetOverrideLeg(interactionPullingLeg);
            playerMovement.SetVerticalLean = 0;
        }
    }
}
