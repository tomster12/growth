
using UnityEngine;


public class PartIndicatable : Part
{
    public static bool ShowIndicators { get; set; } = false;
    private static Color invisibleColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    private static Color visibleColor = new Color(1.0f, 1.0f, 1.0f, 0.3f);
    private static float offsetWobbleScale = 0.05f;
    
    public Vector3 TargetOffset { get; set; }
    public float PositionLerpSpeed { get; set; } = 4.0f;
    public float ColorLerpSpeed { get; set; } = 10.0f;
    public float WobbleMagnitude { get; set; } = 0.2f;
    public float WobbleFrequency { get; set; } = 2.0f;
    public bool IsVisible => ShowIndicators && !Composable.GetPart<PartHighlightable>().IsHighlighted && Composable.GetPart<PartInteractable>().CanInteract;

    private bool isInitialized = false;
    private Transform indicator;
    private SpriteRenderer indicatorSR;
    private Vector3 currentOffset;


    public override void InitPart(ComposableObject composable)
    {
        base.InitPart(composable);
        composable.RequirePart<PartInteractable>();
    }

    public override void DeinitPart()
    {
        base.DeinitPart();
    }

    public void Init(string iconSpriteName, Vector3 offset)
    {
        GameObject indicatorGO = new GameObject();
        indicator = indicatorGO.transform;
        indicatorSR = indicatorGO.AddComponent<SpriteRenderer>();
        indicatorSR.color = invisibleColor;
        indicatorSR.sprite = SpriteSet.Instance.GetSprite(iconSpriteName);
        TargetOffset = offset.normalized * Composable.Bounds.extents.magnitude * 3f;
        currentOffset = TargetOffset;
        indicator.transform.position = Composable.transform.position + currentOffset;
        isInitialized = true;
    }


    private void Update()
    {
        if (!isInitialized) return;
        if (IsVisible)
        {
            indicatorSR.color = Color.Lerp(indicatorSR.color, visibleColor, ColorLerpSpeed * Time.deltaTime);
        }
        else
        {
            indicatorSR.color = Color.Lerp(indicatorSR.color, invisibleColor, ColorLerpSpeed * Time.deltaTime);
        }
        currentOffset = Vector3.Lerp(currentOffset, TargetOffset.normalized, PositionLerpSpeed * Time.deltaTime);
        float wobble = Mathf.Sin(Composable.transform.position.x * offsetWobbleScale + Time.time * (Mathf.PI * 2.0f) / WobbleFrequency);
        Vector3 pos = Composable.transform.position + currentOffset + currentOffset.normalized * WobbleMagnitude * wobble;
        pos.z = -2.0f;
        indicator.transform.position = pos;
        indicator.transform.up = currentOffset;
    }
}
