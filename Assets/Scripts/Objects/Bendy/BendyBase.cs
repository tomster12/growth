using UnityEngine;

public class BendyBase : MonoBehaviour
{
    [SerializeField] private BendyNode baseNode;

    private void Start()
    {
        baseNode.Start();
    }

    private void Update()
    {
        baseNode.Update();
    }
}
