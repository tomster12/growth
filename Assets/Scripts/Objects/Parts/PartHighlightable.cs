using UnityEngine;

public class PartHighlightable : Part
{
    public OutlineController HighlightOutline { get; protected set; } = null;

    public bool Highlighted
    {
        get => HighlightOutline.enabled;
        set
        {
            if (!CanHighlight && value) return;
            HighlightOutline.enabled = value;
        }
    }

    public Color HighlightColor { get => HighlightOutline.OutlineColor; set => HighlightOutline.OutlineColor = value; }

    public bool CanHighlight { get; private set; } = true;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        HighlightOutline = gameObject.GetComponent<OutlineController>();
        HighlightOutline ??= gameObject.AddComponent<OutlineController>();
        Highlighted = false;
    }

    public override void DeinitPart()
    {
        if (HighlightOutline != null) DestroyImmediate(HighlightOutline);
        base.DeinitPart();
    }

    public void SetHighlighted(bool highlighted) => Highlighted = highlighted;

    public void SetHighlightColor(Color color) => HighlightColor = color;

    public void SetCanHighlight(bool canHighlight)
    {
        CanHighlight = canHighlight;
        if (!CanHighlight) Highlighted = false;
    }
}
