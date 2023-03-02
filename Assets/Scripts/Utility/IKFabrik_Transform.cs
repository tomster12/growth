
using UnityEngine;


public class IKFabrik_Transform : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private IKFabrik_Seperated IK;
    [SerializeField] private Transform target;
    [SerializeField] private Transform pole;


    private void Update()
    {
        // Update IK variables
        IK.targetPos = target.position;
        IK.targetRot = target.rotation;
        IK.polePos = pole.position;
        IK.poleRot = pole.rotation;
    }
}
