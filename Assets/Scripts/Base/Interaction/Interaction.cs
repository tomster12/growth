using System;
using UnityEngine.Assertions;

[Serializable]
public class Interaction
{
    public Interaction(string name, InteractionInput input, string iconSprite)
    {
        IsEnabled = true;
        IsActive = false;
        this.Name = name;
        this.Input = input;
        this.sprite = iconSprite;
    }

    public enum VisibilityType
    { Hidden, Input, Icon, Text }

    public InteractionInput Input { get; protected set; }
    public bool IsEnabled { get; protected set; }
    public bool IsActive { get; protected set; }
    public virtual bool CanInteract => IsEnabled && !IsActive;
    public string Name { get; private set; }

    public string IconSprite => !IsEnabled ? "cross" : IsActive ? (sprite + "_active") : (sprite + "_inactive");
    public string InputSprite => !IsEnabled ? "cross" : IsActive ? (Input.Name + "_active") : (Input.Name + "_inactive");

    public virtual void Update()
    { }

    public virtual void StartInteracting(IInteractor interactor)
    {
        Assert.IsTrue(CanInteract);
        this.interactor = interactor;
        IsActive = true;
    }

    public virtual void StopInteracting(IInteractor interactor)
    {
        Assert.IsTrue(IsActive);
        Assert.AreEqual(this.interactor, interactor);
        this.interactor = null;
        IsActive = false;
    }

    protected string sprite;
    protected IInteractor interactor;
}
