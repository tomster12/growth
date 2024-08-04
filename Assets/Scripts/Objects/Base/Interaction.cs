using System;
using UnityEngine.Assertions;

[Serializable]
public class Interaction
{
    public Interaction(string name, string iconSprite, InteractionInput input, ToolType toolType = ToolType.Any)
    {
        Name = name;
        IconSprite = iconSprite;
        RequiredInput = input;
        RequiredTool = toolType;
        IsEnabled = true;
        IsActive = false;
    }

    public string Name { get; private set; }
    public string IconSprite { get; private set; }
    public InteractionInput RequiredInput { get; protected set; }
    public ToolType RequiredTool { get; protected set; }
    public bool IsActive { get; protected set; }
    public bool IsEnabled { get; set; }
    public bool SpriteVisible { get; private set; } = true;
    public bool IconVisible { get; private set; } = true;
    public bool InputVisible { get; private set; } = true;
    public bool ToolVisible { get; private set; } = true;

    public virtual bool CanInteract(IInteractor interactor) => IsEnabled && !IsActive && CanUseTool(interactor);

    public virtual bool CanUseTool(IInteractor interactor) => RequiredTool == ToolType.Any || interactor.GetInteractorToolType() == RequiredTool;

    public virtual void Update()
    { }

    public virtual void StartInteracting(IInteractor interactor)
    {
        Assert.IsTrue(CanInteract(interactor));
        this.interactor = interactor;
        IsActive = true;
    }

    public virtual void StopInteracting()
    {
        Assert.IsTrue(IsActive);
        this.interactor = null;
        IsActive = false;
    }

    public void SetVisibility(bool sprite, bool icon, bool input, bool tool)
    {
        SpriteVisible = sprite;
        IconVisible = icon;
        InputVisible = input;
        ToolVisible = tool;
    }

    protected IInteractor interactor;
}
