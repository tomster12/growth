
using UnityEditor;
using UnityEngine;


public class PlayerLegs : MonoBehaviour
{

    [Header("References")]
    [SerializeField] PlayerController playerController;
    
    [Header("Config")]
    [SerializeField] private bool drawGizmos = false;


    private void Update()
    {
        
    }


    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (playerController != null)
        {
            Gizmos.DrawSphere(playerController.groundPosition, 0.1f);
        }
    }
}
