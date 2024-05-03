using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TerrainUtils;

public class PlayerLegs : MonoBehaviour
{
    public Vector2 GetFootPos(int footIndex)
    {
        if (footIndex < 0 || footIndex > 3) return Vector2.zero;
        return legIK[footIndex].Bones[legIK[footIndex].BoneCount - 1].position;
    }

    public void SetOverrideFoot(int footIndex, Vector2 pos)
    {
        overrideFoot[footIndex] = true;
        overrideFootPos[footIndex] = pos;
    }

    public void UnsetOverrideFoot(int footIndex)
    {
        overrideFoot[footIndex] = false;
    }

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

    private int walkDir = 0;
    private float walkPct = 0.0f;
    private bool[] legIsIdle = new bool[4];
    private bool[] legIsStepping = new bool[4];
    private Vector2[] footPosA = new Vector2[4];
    private Vector2[] footPosB = new Vector2[4];
    private Vector2[] footTarget = new Vector2[4];
    private Vector2[] footCurrent = new Vector2[4];
    private bool[] overrideFoot = new bool[4];
    private Vector2[] overrideFootPos = new Vector2[4];

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            footTarget[i] = playerMovement.Transform.position;
            footCurrent[i] = playerMovement.Transform.position;
        }
    }

    private void Update()
    {
        // Update walking direction and percent
        float walkAmount = Vector2.Dot(playerMovement.RB.velocity, playerMovement.GroundRightDir.normalized);
        int newWalkDir = (walkAmount < -isWalkingThreshold) ? -1 : (walkAmount > isWalkingThreshold) ? 1 : 0;
        if (newWalkDir != walkDir) { walkDir = newWalkDir; walkPct = 0.0f; }
        walkPct = walkDir == 0 ? 0 : (walkPct + Mathf.Abs(walkAmount * walkSpeed * Time.deltaTime)) % 1;

        for (int i = 0; i < 4; i++)
        {
            bool legPositionSet = false;

            // Foot position is overwritten
            if (overrideFoot[i])
            {
                footTarget[i] = overrideFootPos[i];
                legPositionSet = true;
            }

            // Currently walking so try step each leg
            if (!legPositionSet && walkDir != 0)
            {
                float legPct = GetLegPct(walkPct, i);

                // 0 -> 0.5 | Leg currently at A
                if (legPct <= 0.5f)
                {
                    // Standard Case: Just stepped through onto B
                    if (legIsStepping[i])
                    {
                        footPosA[i] = footPosB[i];
                        footPosB[i] = Vector2.zero;
                        legIsStepping[i] = false;
                    }
                    // Initial Case: Initialize position A
                    if (footPosA[i] == Vector2.zero)
                    {
                        // 0 -> 0.5 === (walkDir -> -walkDir)
                        float stepPct = walkDir * (1.0f - legPct * 4.0f);
                        GetLegEndRaycast(i, stepPct, out _, out Vector2 footPosACheck, out bool footPosATouching);
                        if (footPosATouching) footPosA[i] = footPosACheck;
                    }
                    // Have found foot position
                    if (footPosA[i] != Vector2.zero)
                    {
                        footTarget[i] = footPosA[i];
                        legPositionSet = true;
                    }
                }

                // 0.5 -> 1.0 | Leg stepping from A to B
                else
                {
                    // Initial case: Initialize position A
                    if (footPosA[i] == Vector2.zero)
                    {
                        // 0.5 -> 1.0 === (-walkDir -> -2 * walkDir)
                        float stepPct = -walkDir * (1.0f + 1.0f * ((legPct - 0.5f) / 0.5f));
                        GetLegEndRaycast(i, stepPct, out _, out Vector2 footPosACheck, out bool footPosATouching);
                        if (footPosATouching) footPosA[i] = footPosACheck;
                    }
                    // Standard Case: Find where foot B should be
                    if (footPosB[i] == Vector2.zero)
                    {
                        // 0.5 -> 1.0 === (2 * walkDir -> walkDir)
                        float stepPct = walkDir * (2.0f - ((legPct - 0.5f) / 0.5f));
                        GetLegEndRaycast(i, stepPct, out _, out Vector2 footPosBCheck, out bool footPosBTouching);
                        if (footPosBTouching) footPosB[i] = footPosBCheck;
                    }
                    // Have found foot position
                    if (footPosA[i] != Vector2.zero && footPosB[i] != Vector2.zero)
                    {
                        Vector3 center = (Vector3)((footPosA[i] + footPosB[i]) * 0.5f) - new Vector3(0, 1, 0);
                        float lerpPct = (legPct - 0.5f) * 2.0f;
                        footTarget[i] = center + Vector3.Slerp((Vector3)footPosA[i] - center, (Vector3)footPosB[i] - center, lerpPct);
                        legIsStepping[i] = true;
                        legPositionSet = true;
                    }
                }

                // If didnt find then clear variables
                if (!legPositionSet)
                {
                    footPosA[i] = Vector2.zero;
                    footPosB[i] = Vector2.zero;
                    legIsStepping[i] = false;
                }
            }
            else
            {
                footPosA[i] = Vector2.zero;
                footPosB[i] = Vector2.zero;
                legIsStepping[i] = false;
            }

            // Not set so try idle position
            if (!legPositionSet && playerMovement.IsGrounded)
            {
                if (!legIsIdle[i])
                {
                    GetLegEndRaycast(i, 0, out _, out Vector2 footIdle, out legIsIdle[i]);
                    if (legIsIdle[i]) footTarget[i] = footIdle;
                }
                if (legIsIdle[i]) legPositionSet = true;
            }
            else legIsIdle[i] = false;

            // Nothing worked so free float
            if (!legPositionSet)
            {
                Vector2 dir = footTarget[i] - (Vector2)playerMovement.Transform.position;
                dir = Vector2.ClampMagnitude(dir, legIK[i].TotalLength);
                footTarget[i] = (Vector2)playerMovement.Transform.position + dir;
            }

            // Lerp foot current to target
            footCurrent[i] = Vector2.Lerp(footCurrent[i], footTarget[i], Time.deltaTime * footLerpSpeed);

            // Update IK variables
            legIK[i].TargetPos = footCurrent[i];
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
        float downDistMax = playerMovement.GroundedBodyHeight;
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
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (playerMovement != null)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(playerMovement.GroundPos, 0.1f);

            if (footTarget != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Gizmos.color = gizmoLegColors[i];
                    Gizmos.DrawSphere(footTarget[i], 0.1f);
                }
            }
        }
    }
}
