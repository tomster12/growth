using System;
using UnityEngine.Assertions;

[Serializable]
public class Interaction
{
    public Interaction(string name, string iconSprite, InteractionInput input, ToolType toolType = ToolType.None)
    {
        this.Name = name;
        this.IconSprite = iconSprite;
        this.RequiredInput = input;
        this.RequiredTool = toolType;
        IsEnabled = true;
        IsActive = false;
    }

    public enum VisibilityType
    { Hidden, Input, Icon, Text }

    public string Name { get; private set; }
    public string IconSprite { get; private set; }
    public InteractionInput RequiredInput { get; protected set; }
    public ToolType RequiredTool { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public bool IsActive { get; protected set; }

    public virtual bool CanInteract(IInteractor interactor) => IsEnabled && !IsActive && CanUseTool(interactor);

    public virtual bool CanUseTool(IInteractor interactor) => interactor.GetInteractorToolType() == RequiredTool;

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

    protected IInteractor interactor;
}
