
using System.Collections.Generic;
using UnityEngine;


public interface IInteractable
{
    public bool CanControl { get; }
    public List<Interaction> GetInteractions();
    public Vector2 GetPosition();
    public Bounds GetHoverBounds();
    public void SetControlPosition(Vector3 position, float force);
    public void SetControlAngle(float angle);
    public void SetHovered(bool isHovered);
    public void SetCanControl(bool canControl);
    public bool SetControlled(bool isControlled);
    public void OnControl();
    public void OnDrop();
};
