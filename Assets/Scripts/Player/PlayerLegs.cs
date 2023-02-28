
using UnityEditor;
using UnityEngine;


public class PlayerLegs : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerController playerController;
    
    [Header("Config")]
    [SerializeField] private float legDistance = 0.5f;
    [SerializeField] private bool drawGizmos = false;

    private Vector2[] legTargets;
    private float[] legFullDistances;


    private void Start()
    {
        // Initialize leg variables
        legTargets = new Vector2[4];
        legFullDistances = new float[4];
        for (int i = 0; i < 4; i++)
        {
            float horizontalMult = (i <= 1) ? (2 - i) : (-1 + i);
            legTargets[i] = Vector2.zero;
            legFullDistances[i] = horizontalMult * legDistance + playerController.groundedHeight;
        }
    }


    private void Update()
    {

        for (int i = 0; i < 4; i++)
        {
            // Figure out where leg end should be
            float horizontalMult = (i <= 1) ? (-2 + i) : (-1 + i);
            GetLegEnd(horizontalMult * legDistance, out Vector2 pos, out bool isTouching);

            // Set leg to ground if touching
            if (isTouching) legTargets[i] = pos;

            // Otherwise free float
            else
            {
                Vector2 dir = legTargets[i] - (Vector2)playerController.transform.position;
                dir = Vector2.ClampMagnitude(dir, legFullDistances[i]);
                legTargets[i] = (Vector2)playerController.transform.position + dir;
            }
        }
    }

    private void GetLegEnd(float offset, out Vector2 pos, out bool isTouching)
    {
        int terrainMask = 1 << LayerMask.NameToLayer("Terrain");

        // Raycast sideways
        Vector2 sideFrom = playerController.transform.position;
        Vector2 sideDir = playerController.rightDir.normalized;
        float sideDistMax = offset;
        RaycastHit2D sideHit = Physics2D.Raycast(sideFrom, sideDir, sideDistMax, terrainMask);
        float sideDist = (sideHit.collider != null) ? sideHit.distance : sideDistMax;

        // Raycast downwards
        Vector2 downFrom = sideFrom + sideDir * sideDist;
        Vector2 downDir = playerController.groundDir.normalized;
        float downDistMax = playerController.groundedHeight;
        RaycastHit2D downHit = Physics2D.Raycast(downFrom, downDir, downDistMax, terrainMask);

        // Update out variables
        isTouching = downHit.collider != null;
        if (isTouching) pos = downHit.point;
        else pos = downFrom + downDir * downDistMax;
    }


    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (playerController != null)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(playerController.groundPosition, 0.1f);


            if (legTargets != null)
            {
                Gizmos.color = Color.blue;
                foreach (Vector2 target in legTargets)
                {
                    Gizmos.DrawSphere(target, 0.15f);
                }
            }
        }
    }
}
