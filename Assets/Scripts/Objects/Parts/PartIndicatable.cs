using System.Collections.Generic;
using UnityEngine;

public class PartIndicatable : Part
{
    public enum IconType
    { General, Ingredient, Resource }

    public static bool ShowIndicators { get; set; } = false;
    public Vector3 TargetOffset { get; set; }
    public float PositionLerpSpeed { get; set; } = 4.0f;
    public float ColorLerpSpeed { get; set; } = 10.0f;
    public float WobbleMagnitude { get; set; } = 0.22f;
    public float WobbleFrequency { get; set; } = 3.0f;
    public bool ToShow { get; set; } = false;
    public bool ToHide { get; set; } = false;
    public bool IsVisible => (ToShow || showTimer > 0) && !ToHide;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);

        // Create indicator game object
        GameObject indicatorGO = new GameObject();
        indicator = indicatorGO.transform;
        indicator.name = "Indicator";
        indicator.gameObject.SetActive(false);

        // Add sprite renderer
        indicatorSR = indicatorGO.AddComponent<SpriteRenderer>();
        indicatorSR.color = INVISIBLE_COLOR;
    }

    public void SetIcon(IconType type)
    {
        indicatorSR.sprite = SpriteSet.GetSprite(ICON_PATHS[type]);
        isInitialized = true;
    }

    public void SetOffset(Vector3 offset)
    {
        TargetOffset = offset.normalized * Composable.Bounds.extents.magnitude * 3f;
        currentOffset = TargetOffset;
        indicator.transform.position = Composable.transform.position + currentOffset;
        wobbleTimeStart = Time.time;
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
    private static Color VISIBLE_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.3f);
    private static float OFFSET_WOBBLE_SCALE = 0.06f;
    private static Dictionary<IconType, string> ICON_PATHS = new Dictionary<IconType, string>
    {
        { IconType.General, "indicator_general" },
        { IconType.Ingredient, "indicator_ingredient" },
        { IconType.Resource, "indicator_resource" }
    };

    private float showTimer = 0.0f;
    private float wobbleTimeStart = 0.0f;
    private bool isInitialized = false;
    private Transform indicator;
    private SpriteRenderer indicatorSR;
    private Vector3 currentOffset;

    private void Update()
    {
        if (!isInitialized) return;

        // Update show timer
        showTimer = Mathf.Max(0, showTimer - Time.deltaTime);

        // Lerp color towards target color
        if (IsVisible)
        {
            indicatorSR.color = Color.Lerp(indicatorSR.color, VISIBLE_COLOR, ColorLerpSpeed * Time.deltaTime);
            indicator.gameObject.SetActive(true);
        }
        else
        {
            indicatorSR.color = Color.Lerp(indicatorSR.color, INVISIBLE_COLOR, ColorLerpSpeed * Time.deltaTime);

            // If reached target color hide
            if (Mathf.Abs(indicatorSR.color.a - INVISIBLE_COLOR.a) < 0.01f)
            {
                indicator.gameObject.SetActive(false);
            }
        }

        // Lerp offset towards target offset
        currentOffset = Vector3.Lerp(currentOffset, TargetOffset.normalized, PositionLerpSpeed * Time.deltaTime);
        float wobble = Mathf.Sin((Time.time - wobbleTimeStart) * (Mathf.PI * 2.0f) / WobbleFrequency);
        Vector3 pos = Composable.transform.position + currentOffset + currentOffset.normalized * WobbleMagnitude * wobble;
        pos.z = -2.0f;
        indicator.transform.position = pos;
        indicator.transform.up = currentOffset;
    }
}
