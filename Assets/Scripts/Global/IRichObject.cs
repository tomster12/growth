
using System.Collections.Generic;
using UnityEngine;


public interface IRichObject
{
    bool CanControl { get; }

    List<Interaction> GetInteractions();
    Vector2 GetPosition();
    Bounds GetHoverBounds();
    void SetControlPosition(Vector3 position, float force);
    void SetControlAngle(float angle);
    void SetHovered(bool isHovered);
    void SetCanControl(bool canControl);
    bool SetControlled(bool isControlled);
    void OnControl();
    void OnDrop();
};
