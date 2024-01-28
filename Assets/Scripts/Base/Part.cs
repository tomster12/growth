using UnityEngine;

public abstract class Part : MonoBehaviour
{
    public CompositeObject Composable { get; private set; }
    public bool IsInitialized => Composable != null;

    public virtual void InitPart(CompositeObject composable)
    {
        this.Composable = composable;
    }

    public virtual void DeinitPart()
    {
        this.Composable = null;
    }
}
