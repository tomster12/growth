public class PartHighlightable : Part
{
    public OutlineController HighlightOutline { get; protected set; } = null;
    public bool IsHighlighted { get; private set; } = false;

    public override void InitPart(ComposableObject composable)
    {
        base.InitPart(composable);
        HighlightOutline = gameObject.GetComponent<OutlineController>(); // TODO: I think Unity has a nicer way of doing this
        if (HighlightOutline == null)
            HighlightOutline = gameObject.AddComponent<OutlineController>();
        HighlightOutline.enabled = false;
    }

    public override void DeinitPart()
    {
        if (HighlightOutline != null) DestroyImmediate(HighlightOutline);
        base.DeinitPart();
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (IsHighlighted == isHighlighted) return;

        // Update variables
        IsHighlighted = isHighlighted;
        HighlightOutline.enabled = IsHighlighted;
    }
}
