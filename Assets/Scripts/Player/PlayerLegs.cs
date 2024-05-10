using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TerrainUtils;

public class PlayerLegs : MonoBehaviour
{
    public Vector2 GetFootPos(int legIndex)
    {
        if (legIndex < 0 || legIndex > 3) return Vector2.zero;
        return legIK[legIndex].Bones[legIK[legIndex].BoneCount - 1].position;
    }

    public void SetOverrideLeg(int legIndex, Vector2 pos) => footOverridePos[legIndex] = pos;

    public void UnsetOverrideLeg(int legIndex) => footOverridePos[legIndex] = Vector2.zero;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Color[] gizmoLegColors;
    [SerializeField] private float[] walkOffsets;
    [SerializeField] private IKFabrik[] legIK;

    [Header("Prefabs")]
    [SerializeField] private GameObject stepParticlePfb;

    [Header("Config")]
    [SerializeField] private float legWidth = 0.15f;
    [SerializeField] private float legStartOffset = 0.3f;
    [SerializeField] private float legEndOffset = 0.35f;
    [SerializeField] private float kneeHeight = 0.2f;
    [SerializeField] private float stepSize = 1.5f;
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float footLerpSpeed = 2.0f;
    [SerializeField] private float isWalkingThreshold = 0.1f;
    [SerializeField] private bool drawGizmos = false;
    [SerializeField] private int gizmoLeg = -1;

    private float[] legLengths = new float[4];
    private int walkingDirection = 0;
    private float walkingTime = 0.0f;
    private FootState[] footState = new FootState[4];
    private Vector2[] footStepA = new Vector2[4];
    private Vector2[] footStepB = new Vector2[4];
    private Transform[] footStepBTfm = new Transform[4];
    private Vector2[] footTargetPos = new Vector2[4];
    private Vector2[] footCurrentPos = new Vector2[4];
    private Vector2[] footOverridePos = new Vector2[4];

    private enum FootState
    { None, Air, Override, Idle, WalkingA, WalkingB };

    private void Start()
    {
        // Initialize IK and foot positions
        SetLegLengths();
        for (int i = 0; i < 4; i++)
        {
            footTargetPos[i] = GetFootPos(i);
            footCurrentPos[i] = footTargetPos[i];
        }
    }

    private void Update()
    {
        // Update walking direction and time
        if (playerMovement.IsGrounded)
        {
            float walkAmount = Vector2.Dot(playerMovement.RB.velocity, playerMovement.GroundRightDir.normalized);
            int newWalkDirection = (walkAmount < -isWalkingThreshold) ? -1 : (walkAmount > isWalkingThreshold) ? 1 : 0;
            walkingTime = newWalkDirection == 0 ? 0.0f : (walkingTime + Mathf.Abs(walkAmount * walkSpeed * Time.deltaTime)) % 1;

            // Changed direction so reset variables
            if (newWalkDirection != walkingDirection)
            {
                walkingDirection = newWalkDirection;
                walkingTime = 0.0f;

                if (walkingDirection != 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        footStepA[i] = Vector2.zero;
                        footStepB[i] = Vector2.zero;
                        if (footState[i] == FootState.Idle) footState[i] = FootState.None;
                    }
                }
            }
        }

        // Otherwise reset variables
        else
        {
            walkingDirection = 0;
            walkingTime = 0.0f;
        }

        // Update each foot
        for (int i = 0; i < 4; i++)
        {
            FootState newFootState = FootState.None;

            // Foot: Override
            if (footOverridePos[i] != Vector2.zero)
            {
                footTargetPos[i] = footOverridePos[i];
                newFootState = FootState.Override;
            }

            // Check either walking or idle
            if (newFootState == FootState.None && playerMovement.IsGrounded)
            {
                // State: Walking
                if (newFootState == FootState.None && walkingDirection != 0)
                {
                    float legPct = GetLegPct(walkingTime, i);

                    // 0 -> 0.5 | Leg currently at A
                    if (legPct <= 0.5f)
                    {
                        // Standard Case: Just stepped through onto B
                        if (footState[i] == FootState.WalkingB)
                        {
                            footStepA[i] = footStepB[i];
                            footStepB[i] = Vector2.zero;
                            newFootState = FootState.WalkingA;

                            GenerateStepParticles(footStepBTfm[i], footStepA[i]);
                        }

                        // Initial Case: Try initialize position A
                        if (footStepA[i] == Vector2.zero)
                        {
                            // 0 -> 0.5 === (walkDir -> -walkDir)
                            float stepPct = walkingDirection * (1.0f - legPct * 4.0f);
                            GetLegEndRaycast(i, stepPct, out _, out Vector2 calcFootStepB, out bool footStepATouching);
                            if (footStepATouching) footStepA[i] = calcFootStepB;
                        }

                        // Foot pos A is grounded so set
                        if (footStepA[i] != Vector2.zero)
                        {
                            footTargetPos[i] = footStepA[i];
                            newFootState = FootState.WalkingA;
                        }
                    }

                    // 0.5 -> 1.0 | Leg stepping from A to B
                    else
                    {
                        // Initial case: Initialize position A
                        if (footStepA[i] == Vector2.zero)
                        {
                            // 0.5 -> 1.0 === (-walkDir -> -2 * walkDir)
                            float stepPct = -walkingDirection * (1.0f + 1.0f * ((legPct - 0.5f) / 0.5f));
                            GetLegEndRaycast(i, stepPct, out _, out Vector2 footStepACheck, out bool footStepATouching);
                            if (footStepATouching) footStepA[i] = footStepACheck;
                        }

                        // Standard Case: Find where foot B should be
                        if (footStepB[i] == Vector2.zero)
                        {
                            // 0.5 -> 1.0 === (2 * walkDir -> walkDir)
                            float stepPct = walkingDirection * (2.0f - ((legPct - 0.5f) / 0.5f));
                            GetLegEndRaycast(i, stepPct, out Transform calcFootStepBTfm, out Vector2 calcFootStepB, out bool footStepBTouching);
                            if (footStepBTouching)
                            {
                                footStepB[i] = calcFootStepB;
                                footStepBTfm[i] = calcFootStepBTfm;
                            }
                        }

                        // Have found foot position
                        if (footStepA[i] != Vector2.zero && footStepB[i] != Vector2.zero)
                        {
                            Vector3 center = (Vector3)((footStepA[i] + footStepB[i]) * 0.5f) - new Vector3(0, 1, 0);
                            float lerpPct = (legPct - 0.5f) * 2.0f;
                            footTargetPos[i] = center + Vector3.Slerp((Vector3)footStepA[i] - center, (Vector3)footStepB[i] - center, lerpPct);
                            newFootState = FootState.WalkingB;
                        }
                    }
                }

                // State: Idle
                if (newFootState == FootState.None && walkingDirection == 0)
                {
                    if (footState[i] != FootState.Idle)
                    {
                        GetLegEndRaycast(i, 0, out _, out Vector2 footIdle, out bool footIdleTouching);
                        if (footIdleTouching)
                        {
                            footTargetPos[i] = footIdle;
                            newFootState = FootState.Idle;
                        }
                    }
                    else newFootState = FootState.Idle;
                }
            }

            // State: Air
            if (newFootState == FootState.None)
            {
                Vector2 dir = footTargetPos[i] - (Vector2)playerMovement.Transform.position;
                dir = Vector2.ClampMagnitude(dir, legIK[i].TotalLength);
                footTargetPos[i] = (Vector2)playerMovement.Transform.position + dir;
                newFootState = FootState.Air;
            }

            // ------------------------------

            // Clear walking variables if no longer walking
            if ((footState[i] == FootState.WalkingA || footState[i] == FootState.WalkingB)
                && newFootState != FootState.WalkingA && newFootState != FootState.WalkingB)
            {
                footStepA[i] = Vector2.zero;
                footStepB[i] = Vector2.zero;
            }

            // Lerp foot current to target
            footCurrentPos[i] = Vector2.Lerp(footCurrentPos[i], footTargetPos[i], Time.deltaTime * footLerpSpeed);
            footState[i] = newFootState;

            // Update IK variables
            legIK[i].TargetPos = footCurrentPos[i];
            legIK[i].TargetRot = Quaternion.identity;
            legIK[i].PolePos = GetLegPole(i);
            legIK[i].PoleRot = Quaternion.identity;
        }
    }

    private float GetLegPct(float walkPct, int footIndex)
    {
        return (walkPct + walkOffsets[footIndex]) % 1.0f;
    }

    private void GenerateStepParticles(Transform transform, Vector2 pos)
    {
        // Only produce steps on world
        // TODO: Change colour based on material
        if (transform != playerMovement.ClosestWorld.WorldGenerator.WorldTransform) return;
        GameObject particleGO = Instantiate(stepParticlePfb);
        particleGO.transform.position = pos;
    }

    private void GetLegEndRaycast(int footIndex, float stepPct, out Transform transform, out Vector2 pos, out bool isTouching)
    {
        float horizontalMult = (footIndex <= 1) ? (-2 + footIndex) : (-1 + footIndex);
        int terrainMask = 1 << LayerMask.NameToLayer("Terrain");

        // Raycast sideways
        Vector2 sideFrom = playerMovement.Transform.position;
        Vector2 sideDir = playerMovement.GroundRightDir.normalized;
        float sideDistMax = horizontalMult * legEndOffset + stepPct * stepSize * 0.5f;
        RaycastHit2D sideHit = Physics2D.Raycast(sideFrom, sideDir, sideDistMax, terrainMask);
        float sideDist = (sideHit.collider != null) ? sideHit.distance : sideDistMax;

        // Raycast downwards
        Vector2 downFrom = sideFrom + sideDir * sideDist;
        Vector2 downDir = -playerMovement.GroundUpDir.normalized;
        float downDistMax = playerMovement.GroundedBodyHeight + stepSize * 0.5f;
        RaycastHit2D downHit = Physics2D.Raycast(downFrom, downDir, downDistMax, terrainMask);

        // Update out variables
        isTouching = downHit.collider != null;
        if (isTouching)
        {
            transform = downHit.collider.transform;
            pos = downHit.point;
        }
        else
        {
            transform = null;
            pos = downFrom + downDir * downDistMax;
        }
    }

    private Vector2 GetLegPole(int footIndex)
    {
        float horizontalMult = (footIndex <= 1) ? (-2 + footIndex) : (-1 + footIndex);
        return (Vector2)playerMovement.Transform.position
            + (playerMovement.GroundRightDir.normalized * horizontalMult * 2 * legEndOffset)
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
                + ((Vector2)playerMovement.Transform.right * Mathf.Sign(horizontalMult) * legStartOffset);
            Vector2 bone1Pos = bone0Pos
                + ((Vector2)playerMovement.Transform.right * horizontalMult * legEndOffset * 0.5f)
                + ((Vector2)playerMovement.Transform.up * kneeHeight);
            Vector2 bone2Pos = bone0Pos
                + ((Vector2)playerMovement.Transform.right * horizontalMult * legEndOffset)
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

            legLengths[i] = (bone2Pos - bone1Pos).magnitude + (bone1Pos - bone0Pos).magnitude;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (playerMovement != null)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(playerMovement.GroundPos, 0.1f);

            if (footTargetPos != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (gizmoLeg != -1 && i != gizmoLeg) continue;

                    Gizmos.color = gizmoLegColors[i];
                    Gizmos.DrawSphere(footTargetPos[i], 0.1f);

                    if (gizmoLeg != -1)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(footStepA[i], 0.1f);
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(footStepB[i], 0.1f);
                    }
                }
            }
        }
    }
}
