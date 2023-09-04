
using UnityEngine;


public class BendyBase : MonoBehaviour
{
    [SerializeField] private BendyNode baseNode;


    private void Awake()
    {
        baseNode.Awake();
    }

    private void Update()
    {
        baseNode.Update();
    }
}
