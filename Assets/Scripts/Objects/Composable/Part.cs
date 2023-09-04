
using UnityEngine;


public abstract class Part : MonoBehaviour
{
    public ComposableObject Composable { get; private set; }
    public bool IsInitialized => Composable != null;


    public virtual void InitPart(ComposableObject composable)
    {
        this.Composable = composable;
    }

    public virtual void DeinitPart()
    {
        this.Composable = null;
    }
}
