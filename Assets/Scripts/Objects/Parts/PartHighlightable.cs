using UnityEngine;

public class PartHighlightable : Part
{
    public bool Highlighted
    {
        get => outlineController.enabled;
        set
        {
            if (!CanHighlight && value) return;
            outlineController.enabled = value;
        }
    }

    public Color HighlightColor { get => outlineController.OutlineColor; set => outlineController.OutlineColor = value; }

    public float HighlightWidth { get => outlineController.OutlineWidth; set => outlineController.OutlineWidth = value; }

    public bool CanHighlight { get; private set; } = true;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        outlineController = gameObject.GetComponent<OutlineController2D>();
        outlineController ??= gameObject.AddComponent<OutlineController2D>();
        Highlighted = false;
    }

    public override void DeinitPart()
    {
        if (outlineController != null) DestroyImmediate(outlineController);
        base.DeinitPart();
    }

    public void SetHighlighted(bool highlighted) => Highlighted = highlighted;

    public void SetHighlightColor(Color color) => HighlightColor = color;

    public void SetCanHighlight(bool canHighlight)
    {
        CanHighlight = canHighlight;
        if (!CanHighlight) Highlighted = false;
    }

    private OutlineController2D outlineController;
}
