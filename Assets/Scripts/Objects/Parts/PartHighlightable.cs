using UnityEngine;

public class PartHighlightable : Part
{
    public OutlineController OutlineController { get; private set; }

    public bool CanHighlight { get; private set; } = true;

    public override void InitPart(CompositeObject composable)
    {
        base.InitPart(composable);
        OutlineController = gameObject.GetComponent<OutlineController>();
        OutlineController ??= gameObject.AddComponent<OutlineController>();
        OutlineController.enabled = false;
    }

    public override void DeinitPart()
    {
        if (OutlineController != null) DestroyImmediate(OutlineController);
        base.DeinitPart();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (!CanHighlight) return;
        OutlineController.enabled = highlighted;
    }

    public void SetCanHighlight(bool canHighlight)
    {
        CanHighlight = canHighlight;
        if (!CanHighlight) OutlineController.enabled = false;
    }
}
