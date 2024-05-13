public class PartHighlightable : Part
{
    public OutlineController HighlightOutline { get; protected set; } = null;
    public bool Highlighted { get => HighlightOutline.enabled; set => HighlightOutline.enabled = value; }

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
}
