using UnityEngine;

public interface IOrganiserChild
{
    bool IsVisible { get; }

    Transform Transform { get; }

    float GetOrganiserChildHeight();
}
