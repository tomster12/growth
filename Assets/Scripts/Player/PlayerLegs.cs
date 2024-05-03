using UnityEngine;

public class PlayerLegs : MonoBehaviour
{
    public Vector2 GetLegEnd(int legIndex)
    {
        if (legIndex < 0 || legIndex > 3) return Vector2.zero;
        return legIK[legIndex].Bones[legIK[legIndex].BoneCount - 1].position;
    }

    public void SetOverrideLeg(int legIndex, Vector2 pos)
    {
        overrideLegs[legIndex] = true;
        overrideLegPos[legIndex] = pos;
    }

    public void UnsetOverrideLeg(int legIndex)
    {
        overrideLegs[legIndex] = false;
    }

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Color[] gizmoLegColors;
    [SerializeField] private float[] walkOffsets;

    [Header("Prefabs")]
    [SerializeField] private GameObject stepParticlePfb;

    [Header("Config")]
    [SerializeField] private float legOffset = 0.3f;
    [SerializeField] private float legWidth = 0.15f;
    [SerializeField] private float legGap = 0.35f;
    [SerializeField] private float kneeHeight = 0.2f;
    [SerializeField] private float stepSize = 1.5f;
    [SerializeField] private float stepThreshold = 0.85f;
    [SerializeField] private float isWalkingThreshold = 0.1f;
    [SerializeField] private float walkFootLerp = 20f;
    [SerializeField] private IKFabrik[] legIK;
    [SerializeField] private int walkDir = 0;
    [SerializeField] private bool drawGizmos = false;

    private bool[] overrideLegs = new bool[4];
    private Vector2[] overrideLegPos = new Vector2[4];
    private bool legCurrentInit;
    private bool[] legGrounded = new bool[4];
    private Vector2[] legTargets = new Vector2[4];
    private Vector2[] legCurrent = new Vector2[4];
    private bool[] haveStepped = new bool[4];

    private void Update()
    {
        // Calculate how much player is walking
        float walkPct = Vector2.Dot(playerMovement.RB.velocity, playerMovement.GroundRightDir.normalized);
        walkDir = (walkPct < -isWalkingThreshold) ? -1 : (walkPct > isWalkingThreshold) ? 1 : 0;

        for (int i = 0; i < 4; i++)
        {
            // Overidden leg logic
            if (overrideLegs[i])
            {
                legTargets[i] = overrideLegPos[i];
                haveStepped[i] = false;
            }

            // Standard walking logic
            else
            {
                // Check if idle position is grounded
                GetLegEndRaycast(i, 0, out Transform idleTransform, out Vector2 idlePos, out bool idleIsTouching);
                if (idleIsTouching)
                {
                    // Walking so check forward position
                    if (walkDir != 0)
                    {
                        // Front is touching so check if need to step
                        GetLegEndRaycast(i, walkDir, out Transform frontTransform, out Vector2 frontPos, out bool frontIsTouching);
                        if (frontIsTouching)
                        {
                            float pct = (frontPos - legTargets[i]).magnitude / (stepSize - stepThreshold);
                            int offsetIndex = (walkDir < 0) ? (3 - i) : i;
                            if (!haveStepped[i]) pct += walkOffsets[offsetIndex];
                            if (pct > 1.0f)
                            {
                                legTargets[i] = frontPos;
                                haveStepped[i] = true;
                                GenerateStepParticles(frontTransform, legTargets[i]);
                            }
                        }

                        // Front leg not touching so move back to idle
                        else
                        {
                            legTargets[i] = idlePos;
                            GenerateStepParticles(idleTransform, legTargets[i]);
                        }
                    }

                    // Not walking so reset to idle position
                    else
                    {
                        legTargets[i] = idlePos;
                        haveStepped[i] = false;
                    }
                }

                // Not grounded so free float
                else
                {
                    Vector2 dir = legTargets[i] - (Vector2)playerMovement.Transform.position;
                    dir = Vector2.ClampMagnitude(dir, legIK[i].TotalLength);
                    legTargets[i] = (Vector2)playerMovement.Transform.position + dir;
                    haveStepped[i] = false;
                }

                legGrounded[i] = idleIsTouching;
            }

            // lerp leg current to target
            if (!legCurrentInit) legCurrent[i] = legTargets[i];
            else legCurrent[i] = Vector2.Lerp(legCurrent[i], legTargets[i], Time.deltaTime * walkFootLerp);

            // Update IK variables
            legIK[i].TargetPos = legCurrent[i];
            legIK[i].TargetRot = Quaternion.identity;
            legIK[i].PolePos = GetLegPole(i);
            legIK[i].PoleRot = Quaternion.identity;
        }

        legCurrentInit = true;
    }

    private void GenerateStepParticles(Transform transform, Vector2 pos)
    {
        // Only produce steps on world
        if (transform != playerMovement.ClosestWorld.WorldGenerator.WorldTransform) return;
        GameObject particleGO = Instantiate(stepParticlePfb);
        particleGO.transform.position = pos;
    }

    private void GetLegEndRaycast(int legIndex, int walkDir, out Transform transform, out Vector2 pos, out bool isTouching)
    {
        float horizontalMult = (legIndex <= 1) ? (-2 + legIndex) : (-1 + legIndex);
        int terrainMask = 1 << LayerMask.NameToLayer("Terrain");

        // Raycast sideways
        Vector2 sideFrom = playerMovement.Transform.position;
        Vector2 sideDir = playerMovement.GroundRightDir.normalized;
        float sideDistMax = horizontalMult * legGap + walkDir * stepSize * 0.5f;
        RaycastHit2D sideHit = Physics2D.Raycast(sideFrom, sideDir, sideDistMax, terrainMask);
        float sideDist = (sideHit.collider != null) ? sideHit.distance : sideDistMax;

        // Raycast downwards
        Vector2 downFrom = sideFrom + sideDir * sideDist;
        Vector2 downDir = -playerMovement.GroundUpDir.normalized;
        float downDistMax = playerMovement.GroundedBodyHeight;
        RaycastHit2D downHit = Physics2D.Raycast(downFrom, downDir, downDistMax, terrainMask);

        // Update out variables
        isTouching = downHit.collider != null;
        if (isTouching)
        {
            pos = downHit.point;
            transform = downHit.collider.transform;
        }
        else
        {
            pos = downFrom + downDir * downDistMax;
            transform = null;
        }
    }

    private Vector2 GetLegPole(int legIndex)
    {
        float horizontalMult = (legIndex <= 1) ? (-2 + legIndex) : (-1 + legIndex);
        return (Vector2)playerMovement.Transform.position
            + (playerMovement.GroundRightDir.normalized * horizontalMult * legGap)
            + (playerMovement.GroundUpDir.normalized * kneeHeight * 2);
    }

    [ContextMenu("Set Leg Lengths")]
    private void SetLegLengths()
    {
        // Init bones
        for (int i = 0; i < 4; i++)
        {
            float horizontalMult = (i <= 1) ? (-2 + i) : (-1 + i);
            legIK[i].InitBones();

            Vector2 bone0Pos = (Vector2)playerMovement.Transform.position
                + ((Vector2)playerMovement.Transform.right * Mathf.Sign(horizontalMult) * legOffset);
            Vector2 bone1Pos = bone0Pos
                + ((Vector2)playerMovement.Transform.right * horizontalMult * legGap * 0.5f)
                + ((Vector2)playerMovement.Transform.up * kneeHeight);
            Vector2 bone2Pos = bone0Pos
                + ((Vector2)playerMovement.Transform.right * horizontalMult * legGap)
                - ((Vector2)playerMovement.Transform.up * playerMovement.TargetBodyHeight);

            legIK[i].Bones[0].up = bone1Pos - bone0Pos;
            legIK[i].Bones[1].up = bone2Pos - bone1Pos;
            legIK[i].Bones[2].up = playerMovement.Transform.up;

            legIK[i].Bones[0].position = bone0Pos;
            legIK[i].Bones[1].position = bone1Pos;
            legIK[i].Bones[2].position = bone2Pos;

            legIK[i].Bones[0].localPosition = new Vector3(legIK[i].Bones[0].localPosition.x, legIK[i].Bones[0].localPosition.y, 0.0f);
            legIK[i].Bones[1].localPosition = new Vector3(legIK[i].Bones[1].localPosition.x, legIK[i].Bones[1].localPosition.y, 0.0f);
            legIK[i].Bones[2].localPosition = new Vector3(legIK[i].Bones[2].localPosition.x, legIK[i].Bones[2].localPosition.y, 0.0f);

            legIK[i].Bones[0].GetChild(1).position = (legIK[i].Bones[0].position + legIK[i].Bones[1].position) / 2.0f;
            legIK[i].Bones[0].GetChild(1).localScale = new Vector3(legWidth, (bone1Pos - bone0Pos).magnitude, 1.0f);
            legIK[i].Bones[1].GetChild(1).position = (legIK[i].Bones[1].position + legIK[i].Bones[2].position) / 2.0f;
            legIK[i].Bones[1].GetChild(1).localScale = new Vector3(legWidth, (bone2Pos - bone1Pos).magnitude, 1.0f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (playerMovement != null)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(playerMovement.GroundPos, 0.1f);

            if (legTargets != null)
            {
                for (int i = 0; i < legTargets.Length; i++)
                {
                    Gizmos.color = gizmoLegColors[i];
                    Gizmos.DrawSphere(legTargets[i], 0.15f);
                }
            }
        }
    }
}
