using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public partial class PlayerController : MonoBehaviour, IInteractor
{
    public enum TargetState
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
    [SerializeField] private GameObject craftingTargetPfb;

    [Header("Target Config")]
    [SerializeField] private float controlForce = 25.0f;
    [SerializeField] private float maxHoverDistance = 15.0f;
    [SerializeField] private float maxControlDistance = 9.0f;
    [SerializeField] private float maxControlWarningThreshold = 2.0f;
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

    [Header("Crafting Config")]
    [SerializeField] private Color craftingUnusedOutlineColor = new Color(0.62f, 0.62f, 0.62f);
    [SerializeField] private Color craftingUsedOutlineColor = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private Color craftingTargetOutlineColor = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField] private CraftingRecipeList craftingRecipeList;
    [SerializeField] private float craftingResultControlForce = 35.0f;
    [SerializeField] private float craftingResultOffset = 1.5f;
    [SerializeField] private float craftingMaximumDistance = 10.0f;

    private bool inputLMB, inputRMB;
    private Vector2 inputMousePosition;
    private float inputMouseDistance;
    private TargetState targetState;
    private CompositeObject targetObject;
    private float targetDistance;
    private LineHelper targetControlLH;
    private LineHelper targetControlLimitLH;
    private int targetControlLeg;
    private Color targetControlLimitColor;
    private Vector2 targetControlLimitedDir;
    private Vector2 targetControlLimitedPos;
    private float targetControlDropTimer;
    private Interaction currentInteraction;
    private CompositeObject equippedObject;
    private int equippedLeg;
    private List<CompositeObject> craftingIngredients = new List<CompositeObject>();
    private CraftingRecipe selectedCraftingRecipe;
    private CraftingRecipe.Usage selectedCraftingRecipeUsage;
    private CraftingResultObject craftingResult;
    private float CursorSqueeze => interactionCursorSqueeze;

    private void Awake()
    {
        Instance = this;

        // Initialize line helpers
        GameObject controlLimitLHGO = new GameObject();
        controlLimitLHGO.transform.parent = transform;
        controlLimitLHGO.name = "Control Limit LH";
        targetControlLimitLH = controlLimitLHGO.AddComponent<LineHelper>();

        GameObject controlDirLHGO = new GameObject();
        controlDirLHGO.transform.parent = transform;
        controlDirLHGO.name = "Control Dir LH";
        targetControlLH = controlDirLHGO.AddComponent<LineHelper>();

        GameObject interactionPullingLHGO = new GameObject();
        interactionPullingLHGO.transform.parent = transform;
        interactionPullingLHGO.name = "Interaction Pulling LH";
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
        UpdateCraftingInteraction();
        UpdateEquipped();
        UpdateIndicating();
        UpdateCursor();
    }

    private void FixedUpdate()
    {
        FixedUpdateCursor();
    }

    private void OnDrawGizmos()
    {
        // Draw inputMousePosition
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(inputMousePosition, 0.1f);
    }

    private void HandleInput()
    {
        // Poll input
        inputLMB = Input.GetMouseButtonDown(0);
        inputRMB = Input.GetMouseButtonDown(1);
        inputMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        inputMouseDistance = (inputMousePosition - (Vector2)playerMovement.Transform.position).magnitude;
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
                if (obj == targetObject) continue;
                if (obj == equippedObject) continue;
                var part = obj.GetPart<PartIndicatable>();
                float dist = (obj.Position - (Vector2)playerMovement.Transform.position).magnitude;
                if (dist < indicateDistance) StartCoroutine(showIndicator(part, dist / indicateSpeed, indicateTimerMax));
            }
        }
    }

    private void UpdateCursor()
    {
        // Follow controlled object in Update()
        if (targetState == TargetState.Controlling)
        {
            playerCursor.SetTargetBounds(targetObject.Bounds, cursorControlGap * (1.0f - CursorSqueeze), true);
            playerCursor.SetIndicator(0, true, inputMousePosition);
            playerCursor.SetIndicator(1, true, targetControlLimitedPos);
            playerCursor.SetCornerColor(cursorColorControl);
            playerCursor.SetIndicatorColor(0, cursorColorIdle);
            playerCursor.SetIndicatorColor(1, cursorColorFar);
        }
        if (targetState == TargetState.Hovering || targetState == TargetState.Interacting)
        {
            playerCursor.SetTargetBounds(targetObject.Bounds, cursorHoverGap * (1.0f - CursorSqueeze), false);
            playerCursor.SetIndicator(0, false);
            playerCursor.SetIndicator(1, false);
            if (targetState == TargetState.Interacting) playerCursor.SetCornerColor(cursorColorInteracting);
            else if (CanInteractAnyTarget) playerCursor.SetCornerColor(cursorColorInteractable);
            else playerCursor.SetCornerColor(cursorColorHover);
        }
    }

    private void FixedUpdateCursor()
    {
        if (targetState == TargetState.None)
        {
            playerCursor.SetTargetPosition(inputMousePosition, true);
            playerCursor.SetIndicator(0, false);
            playerCursor.SetIndicator(1, false);
            if (inputMouseDistance < maxHoverDistance) playerCursor.SetCornerColor(cursorColorIdle);
            else playerCursor.SetCornerColor(cursorColorFar);
        }
    }

    #region Target Hovering / Controlling / Interacting

    private bool CanControlTarget => targetState != TargetState.None && !IsCraftingWithTarget
        && targetObject.HasPart<PartControllable>() && targetObject.GetPart<PartControllable>().CanControl;

    private bool CanInteractAnyTarget => targetState != TargetState.None && !IsCraftingWithTarget
        && targetObject.HasPart<PartInteractable>() && targetObject.GetPart<PartInteractable>().CanInteractAny(this);

    private bool CanTarget(CompositeObject compositeObject)
    {
        if (equippedObject == compositeObject) return false;
        return compositeObject.CanTarget;
    }

    private void UpdateTargetHovering()
    {
        // Handle retargetting if [None] or [Hovering]
        if (targetState == TargetState.None || targetState == TargetState.Hovering)
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

            if (hovered != targetObject)
            {
                // Unset old target (if it is not null)
                if (!IsCraftingWithTarget)
                {
                    targetObject?.GetPart<PartHighlightable>()?.SetHighlighted(false);
                }

                // Update with new target (which can be null)
                SetTarget(hovered);
                targetState = (bool)hovered ? TargetState.Hovering : TargetState.None;
                targetObject?.GetPart<PartHighlightable>()?.SetHighlighted(true);
            }
        }
    }

    private void UpdateTargetControlling()
    {
        // [Hovering] and LMB down so begin [Controlling]
        if (targetState == TargetState.Hovering && CanControlTarget && inputLMB)
        {
            StartControlling();
            inputLMB = false;
            return;
        }

        // Currently [Controlling] so update
        else if (targetState == TargetState.Controlling)
        {
            // Drop on LMB click
            if (inputLMB)
            {
                StopControlling();
                inputLMB = false;
                return;
            }

            // Calculate distances
            Vector2 targetDir = targetObject.Position - (Vector2)playerMovement.Transform.position;
            Vector2 mouseDir = inputMousePosition - (Vector2)playerMovement.Transform.position;
            targetDistance = targetDir.magnitude;
            inputMouseDistance = mouseDir.magnitude;

            // Limit control dir and set control position
            targetControlLimitedDir = Vector2.ClampMagnitude(mouseDir, maxControlDistance - 0.1f);
            targetControlLimitedPos = (Vector2)playerMovement.Transform.position + targetControlLimitedDir;
            targetObject.GetPart<PartControllable>().SetControlPosition(targetControlLimitedPos, controlForce);

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
            playerLegs.SetOverrideLeg(targetControlLeg, targetObject.Position);
            Vector2 pathStart = playerLegs.GetFootPos(targetControlLeg);
            Vector2 pathEnd = targetObject.Position;
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
                    Vector3 dropPos = targetObject.Position;
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
        if (targetState == TargetState.Interacting)
        {
            if (currentInteraction.IsActive && (PollInput(currentInteraction.RequiredInput) == InputEvent.Inactive))
            {
                currentInteraction.StopInteracting();
            }
            if (!currentInteraction.IsActive)
            {
                targetState = TargetState.Hovering;
                OnInteractionFinished();
            }
        }

        // [Hovering] so check for interactions
        else if (targetState == TargetState.Hovering && CanInteractAnyTarget)
        {
            foreach (Interaction interaction in targetObject.GetPart<PartInteractable>().Interactions)
            {
                // Can interact and input polled down begin interaction
                if (interaction.CanInteract(this) && (PollInput(interaction.RequiredInput) == InputEvent.Active))
                {
                    targetState = TargetState.Interacting;
                    currentInteraction = interaction;
                    interaction.StartInteracting(this);
                    break;
                }
            }
        }
    }

    private void SetTarget(CompositeObject newTarget)
    {
        // Update target composable
        targetObject = newTarget;

        // Update prompt organiser with new interactions
        promptOrganiser.Clear();
        if (targetObject != null && targetObject.HasPart<PartInteractable>())
        {
            foreach (Interaction interaction in targetObject.GetPart<PartInteractable>().Interactions)
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
        if (!CanControlTarget) throw new System.Exception("Cannot control target.");
        targetState = TargetState.Controlling;

        if (!targetObject.GetPart<PartControllable>().StartControlling()) throw new System.Exception("Failed to control target.");
        targetObject.GetPart<PartControllable>().SetControlPosition(targetObject.Position, controlForce);

        targetControlLH.SetActive(true);
        targetControlLimitLH.SetActive(true);
        targetControlLimitColor = new Color(controlLimitColorWarning.r, controlLimitColorWarning.g, controlLimitColorWarning.b, 0.0f);

        targetObject.GetPart<PartIndicatable>()?.Hide();

        // Set controlling leg
        Vector2 targetDir = targetObject.Position - (Vector2)playerMovement.Transform.position;
        float rightPct = Vector2.Dot(playerMovement.GroundRightDir, targetDir);
        if (equippedObject != null) targetControlLeg = equippedLeg == 1 ? 2 : 1;
        else targetControlLeg = rightPct > 0.0f ? 2 : 1;
    }

    private void StopControlling()
    {
        if (targetState != TargetState.Controlling) throw new System.Exception("Not controlling target.");
        targetState = TargetState.Hovering;
        if (!targetObject.GetPart<PartControllable>().StopControlling()) throw new System.Exception("Failed to stop controlling target.");
        targetControlLH.SetActive(false);
        targetControlLimitLH.SetActive(false);
        playerLegs.UnsetOverrideLeg(targetControlLeg);
        targetControlDropTimer = 0.0f;
        UpdateTargetHovering();
    }

    #endregion Target Hovering / Controlling / Interacting

    #region Equipping

    private bool IsEquipped => equippedObject != null;

    private bool CanEquipTarget => targetState != TargetState.None && !IsCraftingWithTarget
        && targetObject.HasPart<PartEquipable>() && targetObject.GetPart<PartEquipable>().CanEquip;

    private void UpdateEquipped()
    {
        // [Controlling] RMB down so Equip
        if (targetState == TargetState.Controlling && CanEquipTarget && inputRMB)
        {
            StartEquippingTarget();
            inputRMB = false;
            return;
        }

        // Handle [Equipped] state
        else if (IsEquipped)
        {
            // Check for dropping
            if (inputRMB)
            {
                StopEquipping();
                inputRMB = false;
                return;
            }

            // Grounded so set leg position towards mouse
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

                // 105+ -> 80: Lerp between soft side angles
                if (gripAngleAbs > 80.0f)
                {
                    float lerp = 1.0f - (Mathf.Min(105.0f, gripAngleAbs) - 80.0f) / 25.0f;
                    lerp = Utility.Easing.EaseOutSine(lerp);
                    float lerpSpacing = Mathf.Lerp(legSpacing * 3.6f, legSpacing * 2.0f, lerp);
                    float lerpLength = Mathf.Lerp(legLength * 0.45f, legLength * 0.6f, lerp);
                    legTarget = playerPos;
                    legTarget += Vector2.right * lerpSpacing + Vector2.up * lerpLength;
                }

                // 70 -> 35: Lerp between soft and direct
                else if (gripAngleAbs > 35.0f)
                {
                    float lerp = 1.0f - (gripAngleAbs - 35.0f) / 35.0f;
                    lerp = Utility.Easing.EaseInSine(lerp);
                    Vector2 lerpFrom = playerPos + Vector2.right * legSpacing * 2.0f + Vector2.up * legLength * 0.6f;
                    Vector2 lerpTo = playerPos + (inputMousePosition - playerPos).normalized * legLength * 1.1f;
                    legTarget = Vector2.Lerp(lerpFrom, lerpTo, lerp);
                }

                // 35 -> 0: Direct
                else legTarget = inputMousePosition;

                // Set leg target
                playerLegs.SetOverrideLeg(equippedLeg, legTarget);
            }

            // Not grounded so point directly to mouse
            else playerLegs.SetOverrideLeg(equippedLeg, inputMousePosition);

            // Update grip position
            Vector2 equipGripPos = playerLegs.GetFootPos(equippedLeg);
            Vector2 equipGripDir = inputMousePosition - equipGripPos;
            equippedObject.GetPart<PartEquipable>().SetGrip(equipGripPos, equipGripDir);
        }
    }

    private void StartEquippingTarget()
    {
        if (!CanEquipTarget) throw new System.Exception("Cannot equip target.");
        equippedObject = targetObject;
        equippedLeg = targetControlLeg;
        StopControlling();
        if (!equippedObject.GetPart<PartEquipable>().StartEquipping()) throw new System.Exception("Failed to equip target.");
    }

    private void StopEquipping()
    {
        if (!IsEquipped) throw new System.Exception("No equipped object to unequip.");
        if (!equippedObject.GetPart<PartEquipable>().StopEquipping()) throw new System.Exception("Failed to unequip target.");
        playerLegs.UnsetOverrideLeg(equippedLeg);
        equippedObject = null;
        equippedLeg = -1;
    }

    #endregion Equipping

    #region Crafting

    private bool IsCraftingWithTarget => craftingIngredients.Contains(targetObject);

    private bool CanCraftWithTarget => targetState != TargetState.None && !IsCraftingWithTarget
        && targetObject.HasPart<PartCraftingIngredient>() && targetObject.GetPart<PartControllable>().CanControl == true;

    private void UpdateCraftingInteraction()
    {
        // Currently crafting
        if (craftingIngredients.Count > 0)
        {
            // Check ingredients are not too far away
            for (int i = craftingIngredients.Count - 1; i >= 0; i--)
            {
                CompositeObject ingredient = craftingIngredients[i];
                float dist = (ingredient.Position - (Vector2)playerMovement.Transform.position).magnitude;
                if (dist > craftingMaximumDistance)
                {
                    StopCraftingWith(ingredient);
                }
            }

            // Update crafting result position
            if (craftingResult != null)
            {
                Vector2 targetPos = Vector2.zero;
                foreach (CompositeObject obj in craftingIngredients) targetPos += obj.Position;
                targetPos /= craftingIngredients.Count;
                Vector2 worldDir = World.GetClosestWorldCheap(targetPos).GetCentre() - targetPos;
                targetPos += -worldDir.normalized * 1.5f;
                craftingResult.GetPart<PartControllable>().SetControlPosition(targetPos, craftingResultControlForce);
            }
        }

        // Is [Hovering] and RMB down so handle toggle crafting
        if (targetState == TargetState.Hovering && inputRMB)
        {
            // Object can be crafted with, so add to crafting
            if (CanCraftWithTarget)
            {
                inputRMB = false;
                StartCraftingWith(targetObject);
            }

            // Object is already crafting with, so remove from crafting
            else if (IsCraftingWithTarget)
            {
                inputRMB = false;
                StopCraftingWith(targetObject);
            }
        }
    }

    private void StartCraftingWith(CompositeObject ingredient)
    {
        // Check not too far away from other ingredients
        foreach (CompositeObject obj in craftingIngredients)
        {
            float dist = (obj.Position - ingredient.Position).magnitude;
            if (dist > craftingMaximumDistance) return;
        }

        craftingIngredients.Add(ingredient);
        ingredient.GetPart<PartHighlightable>().SetHighlighted(true);
        ingredient.GetPart<PartHighlightable>().OutlineController.OutlineColor = craftingUnusedOutlineColor;
        ingredient.GetPart<PartIndicatable>()?.Hide();
        Vector3 controlPosition = ingredient.Position + ingredient.GetPart<PartPhysical>().GRO.GravityDir.normalized * -craftingResultOffset;
        ingredient.GetPart<PartControllable>().StartControlling();
        ingredient.GetPart<PartControllable>().SetControlPosition(controlPosition, controlForce);

        UpdateCraftingRecipe();
    }

    private void StopCraftingWith(CompositeObject ingredient)
    {
        // Check ingredient is in crafting ingredients
        if (!craftingIngredients.Contains(ingredient)) return;

        craftingIngredients.Remove(ingredient);
        ingredient.GetPart<PartHighlightable>().SetHighlighted(false);
        ingredient.GetPart<PartHighlightable>().OutlineController.OutlineColor = Color.white;
        ingredient.GetPart<PartControllable>().StopControlling();

        UpdateCraftingRecipe();
    }

    private void UpdateCraftingRecipe()
    {
        // Clear currently selected crafting recipe
        if (selectedCraftingRecipe != null) ClearCurrentCraftingRecipe();

        // If not crafting delete result and exit early
        if (craftingIngredients.Count == 0)
        {
            if (craftingResult != null) Destroy(craftingResult.gameObject);
            craftingResult = null;
            return;
        }

        // From here we are crafting with some object, so initialize target if doesn't exist
        if (craftingResult == null)
        {
            Vector2 initialPos = craftingIngredients[0].Position;
            GameObject craftingTargetGO = Instantiate(craftingTargetPfb, initialPos, Quaternion.identity);
            craftingResult = craftingTargetGO.GetComponent<CraftingResultObject>();
            craftingResult.GetPart<PartControllable>().StartControlling();
            craftingResult.GetPart<PartHighlightable>().OutlineController.OutlineColor = craftingTargetOutlineColor;
            craftingResult.OnClick += OnCraftingResultClick;
            craftingResult.GetPart<PartControllable>().SetControlPosition(initialPos, craftingResultControlForce);
        }
        else craftingResult.SetRecipe(null);

        // Aggregate crafting ingredient <-> object dictionary
        Dictionary<CraftingIngredient, List<CompositeObject>> ingredientObjDict = new Dictionary<CraftingIngredient, List<CompositeObject>>();
        foreach (CompositeObject obj in craftingIngredients)
        {
            CraftingIngredient ingredient = obj.GetPart<PartCraftingIngredient>().Ingredient;
            if (!ingredientObjDict.ContainsKey(ingredient)) ingredientObjDict[ingredient] = new List<CompositeObject>();
            ingredientObjDict[ingredient].Add(obj);
        }

        // Find available nearby crafting objects
        List<CompositeObject> craftingObjects = CompositeObject.FindObjectsWithPart<PartCraftingObject>();
        List<(CraftingObject, CompositeObject)> craftingObjectPairs = new();
        foreach (CompositeObject obj in craftingObjects)
        {
            CraftingObject craftingObject = obj.GetPart<PartCraftingObject>().CraftingObject;
            float dist = (obj.Position - (Vector2)playerMovement.Transform.position).magnitude;
            if (dist < craftingMaximumDistance) craftingObjectPairs.Add((craftingObject, obj));
        }

        // Get available tool type
        ToolType tool = IsEquipped ? equippedObject.GetPart<PartEquipable>().ToolType : ToolType.None;

        // Find all available recipes
        List<(CraftingRecipe, CraftingRecipe.Usage)> viableRecipes = new();
        foreach (CraftingRecipe recipe in craftingRecipeList.recipes)
        {
            CraftingRecipe.Usage usage = recipe.CanCraft(ingredientObjDict, craftingObjectPairs, tool);
            if (usage.canCraft) viableRecipes.Add((recipe, usage));
        }

        // If there is some viable recipes pick the first one
        if (viableRecipes.Count > 0) SelectCraftingRecipe(viableRecipes[0].Item1, viableRecipes[0].Item2);
    }

    private void SelectCraftingRecipe(CraftingRecipe recipe, CraftingRecipe.Usage usage)
    {
        selectedCraftingRecipe = recipe;
        selectedCraftingRecipeUsage = usage;

        foreach (CompositeObject ingredient in usage.usedIngredients)
        {
            ingredient.GetPart<PartHighlightable>().OutlineController.OutlineColor = craftingUsedOutlineColor;
        }

        foreach (CompositeObject craftingObject in usage.usedObjects)
        {
            craftingObject.GetPart<PartHighlightable>().SetHighlighted(true);
            craftingObject.GetPart<PartHighlightable>().OutlineController.OutlineColor = craftingUsedOutlineColor;
        }

        if (usage.isToolUsed)
        {
            equippedObject.GetPart<PartHighlightable>().SetHighlighted(true);
            equippedObject.GetPart<PartHighlightable>().OutlineController.OutlineColor = craftingUsedOutlineColor;
        }

        craftingResult.SetRecipe(recipe);
    }

    private void ClearCurrentCraftingRecipe()
    {
        foreach (CompositeObject ingredient in selectedCraftingRecipeUsage.usedIngredients)
        {
            ingredient.GetPart<PartHighlightable>().OutlineController.OutlineColor = craftingUnusedOutlineColor;
        }

        foreach (CompositeObject craftingObject in selectedCraftingRecipeUsage.usedObjects)
        {
            craftingObject.GetPart<PartHighlightable>().SetHighlighted(false);
        }

        if (selectedCraftingRecipeUsage.isToolUsed)
        {
            equippedObject.GetPart<PartHighlightable>().SetHighlighted(false);
        }

        selectedCraftingRecipe = null;
        selectedCraftingRecipeUsage = null;
    }

    private void OnCraftingResultClick()
    {
        foreach (CompositeObject ingredient in selectedCraftingRecipeUsage.usedIngredients)
        {
            ingredient.GetPart<PartCraftingIngredient>().UseIngredient();
            craftingIngredients.Remove(ingredient);
        }

        craftingResult.CreateResult();
        UpdateCraftingRecipe();

        targetObject = null;
        targetState = TargetState.None;
    }

    #endregion Crafting

    #region IInteractor

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
        if (IsEquipped) return equippedObject.GetPart<PartEquipable>().ToolType;
        return ToolType.Any;
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
            playerLegs.UnsetOverrideLeg(interactionPullingLeg);
            playerMovement.SetVerticalLean = 0;
        }
    }

    #endregion IInteractor
}
