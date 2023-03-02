
using UnityEngine;


public class IKFabrik_Transform : IKFabrik
{
    [Header("Transform References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform pole;


    private void Update()
    {
        // Update IK variables
        targetPos = target.position;
        targetRot = target.rotation;
        polePos = pole.position;
        poleRot = pole.rotation;
    }
}
