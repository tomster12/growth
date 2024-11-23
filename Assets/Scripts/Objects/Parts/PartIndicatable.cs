using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PartIndicatable : Part
{
    public enum IconType
    { General, Ingredient, Resource }

    public enum OffsetType
    { Direction, Gravity, ClosestWorld }

    public float PositionLerpSpeed { get; set; } = 12.0f;
    public float ColorLerpSpeed { get; set; } = 10.0f;
    public float WobbleMagnitude { get; set; } = 0.22f;
    public float WobbleFrequency { get; set; } = 3.0f;
    public bool ForceShow { get; set; } = false;
    public bool ForceHide { get; set; } = false;
    public bool IsVisible => (ForceShow || showTimer > 0) && !ForceHide;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);

        // Create indicator game object
        GameObject parent = GameObject.Find("Indicators Container");
        GameObject indicatorPfb = AssetManager.GetPrefab("Indicator");
        indicator = Instantiate(indicatorPfb).transform;
        indicator.SetParent(parent.transform);
        indicator.gameObject.SetActive(false);
        indicator.gameObject.layer = LayerMask.NameToLayer("Outside UI");
        indicatorSR = indicator.gameObject.GetComponent<SpriteRenderer>();
        indicatorSR.color = INVISIBLE_COLOR;

        SetIcon(iconType);

        isInitialized = true;
    }

    public void SetIcon(IconType type)
    {
        iconType = type;
        indicatorSR.sprite = AssetManager.GetSprite(ICON_PATHS[type]);
    }

    public void SetOffsetDir(Vector2 offset, bool initial = false)
    {
        offsetType = OffsetType.Direction;
        offsetDir = offset.normalized;
        if (initial)
        {
            wobbleTimeStart = Time.time;
            LerpPosition(0.0f);
        }
    }

    public void SetOffsetGravity(GravityObject gravityObject, bool initial = false)
    {
        offsetType = OffsetType.Gravity;
        offsetGRO = gravityObject;
        if (initial)
        {
            wobbleTimeStart = Time.time;
            LerpPosition(0.0f);
        }
    }

    public void SetOffsetClosestWorld(bool initial = false)
    {
        offsetType = OffsetType.ClosestWorld;
        if (initial)
        {
            wobbleTimeStart = Time.time;
            LerpPosition(0.0f);
        }
    }

    public void Show(float time)
    {
        if (!ForceHide)
        {
            if (showTimer == 0.0f) wobbleTimeStart = Time.time;
            showTimer = time;
        }
    }

    public void Hide()
    {
        showTimer = 0;
    }

    private static Color INVISIBLE_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    private static Color VISIBLE_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.45f);
    private static Dictionary<IconType, string> ICON_PATHS = new Dictionary<IconType, string>
    {
        { IconType.General, "indicator_general" },
        { IconType.Ingredient, "indicator_ingredient" },
        { IconType.Resource, "indicator_resource" }
    };

    [Header("Config")]
    [SerializeField] private IconType iconType;

    private Transform indicator;
    private SpriteRenderer indicatorSR;
    private bool isInitialized = false;
    private float showTimer = 0.0f;
    private float wobbleTimeStart = 0.0f;
    private OffsetType offsetType = OffsetType.ClosestWorld;
    private GravityObject offsetGRO;
    private Vector2 offsetDir = Vector2.zero;

    private void OnDestroy()
    {
        if (indicator != null) DestroyImmediate(indicator.gameObject);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Update show timer
        showTimer = Mathf.Max(0, showTimer - Time.deltaTime);

        // Lerp color and position
        LerpColor(Time.deltaTime);
        LerpPosition(Time.deltaTime);
    }

    private void LerpColor(float dt)
    {
        // Lerp color towards target color
        if (IsVisible)
        {
            indicatorSR.color = Color.Lerp(indicatorSR.color, VISIBLE_COLOR, ColorLerpSpeed * dt);
            indicator.gameObject.SetActive(true);
        }
        else
        {
            indicatorSR.color = Color.Lerp(indicatorSR.color, INVISIBLE_COLOR, ColorLerpSpeed * dt);

            // If reached target color hide
            if (Mathf.Abs(indicatorSR.color.a - INVISIBLE_COLOR.a) < 0.01f) indicator.gameObject.SetActive(false);
        }
    }

    private void LerpPosition(float dt)
    {
        // Closest World: Find closest world and set offset dir
        if (offsetType == OffsetType.ClosestWorld)
        {
            World world = World.GetClosestWorldByCentre(Composable.Position);
            offsetDir = (Composable.Position - (Vector2)world.GetCentre()).normalized;
        }

        // Gravity: Set offset dir to gravity direction
        if (offsetType == OffsetType.Gravity)
        {
            offsetDir = -offsetGRO.GravityDir.normalized;
        }

        // Calculate final position
        float wobble = Mathf.Sin((Time.time - wobbleTimeStart) * (Mathf.PI * 2.0f) / WobbleFrequency);
        float edgeDist = Composable.Bounds.extents.x * Mathf.Abs(offsetDir.x) + Composable.Bounds.extents.y * Mathf.Abs(offsetDir.y);
        Vector2 offset = offsetDir * (edgeDist + WobbleMagnitude * 2 + WobbleMagnitude * wobble);
        Vector3 targetPos = Composable.Bounds.center + (Vector3)offset;
        targetPos.z = -2.0f;

        // Lerp or set position
        Vector2 currentPos = indicator.position;
        if (dt == 0) currentPos = targetPos;
        else currentPos = Vector3.Lerp(currentPos, targetPos, PositionLerpSpeed * dt);

        // Floor to nearest world pixel (12)
        currentPos.x = Mathf.Round(currentPos.x * 12.0f) / 12.0f;
        currentPos.y = Mathf.Round(currentPos.y * 12.0f) / 12.0f;
        indicator.position = currentPos;
        indicator.transform.up = offsetDir;
    }
}
