using System.Collections.Generic;
using UnityEngine;

public class PartIndicatable : Part
{
    public enum IconType
    { General, Ingredient, Resource }

    [SerializeField] public static GameObject prefab;

    public static bool ShowIndicators { get; set; } = false;
    public Vector2 OffsetDir { get; set; }
    public float PositionLerpSpeed { get; set; } = 12.0f;
    public float ColorLerpSpeed { get; set; } = 10.0f;
    public float WobbleMagnitude { get; set; } = 0.22f;
    public float WobbleFrequency { get; set; } = 3.0f;
    public bool ToShow { get; set; } = false;
    public bool ToHide { get; set; } = false;
    public bool IsVisible => (ToShow || showTimer > 0) && !ToHide;
    public bool OffsetFromWorld = false;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);

        // Create indicator game object
        GameObject parent = GameObject.Find("Indicators Container");
        GameObject indicatorPfb = Resources.Load("Prefabs/Indicator") as GameObject;
        indicator = Instantiate(indicatorPfb).transform;
        indicator.SetParent(parent.transform);
        indicator.gameObject.SetActive(false);
        indicator.gameObject.layer = LayerMask.NameToLayer("Outside UI");
        indicatorSR = indicator.gameObject.GetComponent<SpriteRenderer>();
        indicatorSR.color = INVISIBLE_COLOR;

        // Set icon
        SetIcon(iconType);

        floatPosition = Composable.Bounds.center;

        isInitialized = true;
    }

    public void SetIcon(IconType type)
    {
        iconType = type;
        indicatorSR.sprite = SpriteSet.GetSprite(ICON_PATHS[type]);
    }

    public void SetOffsetDir(Vector2 offset)
    {
        OffsetDir = offset.normalized;
        wobbleTimeStart = Time.time;
        LerpPosition(0.0f);
    }

    public void Show(float time)
    {
        if (!ToHide)
        {
            if (showTimer == 0.0f) wobbleTimeStart = Time.time;
            showTimer = time;
        }
    }

    public void Hide() => showTimer = 0;

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

    private float showTimer = 0.0f;
    private float wobbleTimeStart = 0.0f;
    private Vector3 floatPosition;
    private bool isInitialized = false;
    private Transform indicator;
    private SpriteRenderer indicatorSR;

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
            if (Mathf.Abs(indicatorSR.color.a - INVISIBLE_COLOR.a) < 0.01f)
            {
                indicator.gameObject.SetActive(false);
            }
        }
    }

    private void LerpPosition(float dt)
    {
        // Initialize offset dir based on closest world
        if (OffsetFromWorld)
        {
            World world = World.GetClosestWorldCheap(Composable.Position);
            OffsetDir = (Composable.Position - (Vector2)world.GetCentre()).normalized;
        }

        // Calculate final position
        float wobble = Mathf.Sin((Time.time - wobbleTimeStart) * (Mathf.PI * 2.0f) / WobbleFrequency);
        float edgeDist = Composable.Bounds.extents.x * Mathf.Abs(OffsetDir.x) + Composable.Bounds.extents.y * Mathf.Abs(OffsetDir.y);
        Vector2 offset = OffsetDir * (edgeDist + WobbleMagnitude * 2 + WobbleMagnitude * wobble);
        Vector3 finalPos = Composable.Bounds.center + (Vector3)offset;
        finalPos.z = -2.0f;

        // Lerp or set position
        if (dt == 0) floatPosition = finalPos;
        else floatPosition = Vector3.Lerp(floatPosition, finalPos, PositionLerpSpeed * dt);

        // Floor to nearest world pixel (12)
        floatPosition.x = Mathf.Round(floatPosition.x * 12.0f) / 12.0f;
        floatPosition.y = Mathf.Round(floatPosition.y * 12.0f) / 12.0f;
        indicator.position = floatPosition;
        indicator.transform.up = OffsetDir;
    }
}
