using UnityEditor;
using UnityEngine;

public class IKFabrik : MonoBehaviour
{
    public Vector2 TargetPos { get; set; }
    public Quaternion TargetRot { get; set; }
    public Vector2 PolePos { get; set; }
    public Quaternion PoleRot { get; set; }
    public int BoneCount { get; private set; }
    public Transform[] Bones { get; private set; }
    public float[] BoneLengths { get; private set; }
    public float TotalLength { get; private set; }

    [ContextMenu("Init IK")]
    public void InitIK()
    {
        InitBones();
        InitInternal();
    }

    public void InitBones()
    {
        // Get bone count through parents from end
        BoneCount = 0;
        Transform current = boneEnd;
        while (current != boneRoot)
        {
            BoneCount++;
            current = current.parent;
            if (current == null) throw new UnityException("Could not find start.");
        }

        // For each bone we should have as above
        Bones = new Transform[BoneCount];
        current = boneEnd;
        for (int i = BoneCount - 1; i >= 0; i--)
        {
            // Extract bone and loop upwards
            Bones[i] = current;
            current = current.parent;
        }
    }

    public void InitInternal()
    {
        // Declare bone variables
        BoneLengths = new float[BoneCount - 1];
        TotalLength = 0.0f;

        bonesPos_RS = new Vector2[BoneCount];
        initBoneDir_RS = new Vector2[BoneCount];
        initBoneRot_RS = new Quaternion[BoneCount];
        initTargetPos_RS = TransformPosition_WorldToRS(TargetPos);
        initTargetRot_RS = TransformRotation_WorldToRS(TargetRot);

        // For each bone we should have
        for (int i = BoneCount - 1; i >= 0; i--)
        {
            // Update bone RS variables
            bonesPos_RS[i] = TransformPosition_WorldToRS(Bones[i].position);

            // Update initial bone variables and lengths
            initBoneRot_RS[i] = TransformRotation_WorldToRS(Bones[i].rotation);
            if (Bones[i] == boneEnd)
            {
                initBoneDir_RS[i] = initTargetPos_RS - bonesPos_RS[i];
            }
            else
            {
                initBoneDir_RS[i] = bonesPos_RS[i + 1] - bonesPos_RS[i];
                BoneLengths[i] = initBoneDir_RS[i].magnitude;
                TotalLength += BoneLengths[i];
            }
        }
    }

    public void UpdateIK()
    {
        // Get positions and rotations
        for (int i = 0; i < Bones.Length; i++) bonesPos_RS[i] = TransformPosition_WorldToRS(Bones[i].position);
        Vector2 targetPos_RS = TransformPosition_WorldToRS(TargetPos);
        Quaternion targetRot_RS = TransformRotation_WorldToRS(TargetRot);

        // If cannot directly reach, straighten towards target
        Vector2 targetDir_RS = targetPos_RS - bonesPos_RS[0];
        if (targetDir_RS.sqrMagnitude >= (TotalLength * TotalLength))
        {
            targetDir_RS = targetDir_RS.normalized;
            for (int i = 0; i < BoneCount - 1; i++)
            {
                bonesPos_RS[i + 1] = bonesPos_RS[i] + targetDir_RS * BoneLengths[i];
            }
        }

        // Otherwise, apply inverse kinematics
        else
        {
            // Apply snap back
            for (int i = 0; i < BoneCount - 1; i++)
            {
                bonesPos_RS[i + 1] = Vector2.Lerp(bonesPos_RS[i + 1], bonesPos_RS[i] + initBoneDir_RS[i], snapBackStrength);
            }

            // Iteratively solve
            for (int itr = 0; itr < iterations; itr++)
            {
                // Backwards kinematics: going backwards, drag each bone to next
                for (int i = BoneCount - 1; i > 0; i--)
                {
                    if (i == BoneCount - 1)
                    {
                        bonesPos_RS[i] = targetPos_RS;
                    }
                    else
                    {
                        Vector2 backDir = (bonesPos_RS[i] - bonesPos_RS[i + 1]).normalized;
                        bonesPos_RS[i] = bonesPos_RS[i + 1] + backDir * BoneLengths[i];
                    }
                }

                // Forwards kinematics: going forward, pull each bone to previous
                for (int i = 1; i < BoneCount; i++)
                {
                    Vector2 fwDir = (bonesPos_RS[i] - bonesPos_RS[i - 1]).normalized;
                    bonesPos_RS[i] = bonesPos_RS[i - 1] + fwDir * BoneLengths[i - 1];
                }

                // Reached target so break
                if ((bonesPos_RS[BoneCount - 1] - targetPos_RS).sqrMagnitude < (delta * delta)) break;
            }
        }

        // Rotate intermediate bones towards pole
        if (PolePos != null)
        {
            Vector2 pole_RS = TransformPosition_WorldToRS(PolePos);
            for (int i = 1; i < bonesPos_RS.Length - 1; i++)
            {
                Plane plane_RS = new Plane(bonesPos_RS[i + 1] - bonesPos_RS[i - 1], bonesPos_RS[i - 1]);
                Vector2 projectedPole_RS = plane_RS.ClosestPointOnPlane(pole_RS);
                Vector2 projectedBone_RS = plane_RS.ClosestPointOnPlane(bonesPos_RS[i]);
                float angle_RS = Vector2.SignedAngle(projectedBone_RS - bonesPos_RS[i - 1], projectedPole_RS - bonesPos_RS[i - 1]);
                bonesPos_RS[i] = (Vector2)(Quaternion.AngleAxis(angle_RS, plane_RS.normal) * (bonesPos_RS[i] - bonesPos_RS[i - 1])) + bonesPos_RS[i - 1];
            }
        }

        // Set positions and rotations
        for (int i = 0; i < BoneCount; i++)
        {
            if (Bones[i] == boneEnd)
            {
                Bones[i].rotation = TransformRotation_RSToWorld(Quaternion.Inverse(targetRot_RS) * initTargetRot_RS * Quaternion.Inverse(initBoneRot_RS[i]));
            }
            else
            {
                Bones[i].rotation = TransformRotation_RSToWorld(Quaternion.FromToRotation(initBoneDir_RS[i], bonesPos_RS[i + 1] - bonesPos_RS[i]) * Quaternion.Inverse(initBoneRot_RS[i]));
            }
            Bones[i].position = TransformPosition_RSToWorld(bonesPos_RS[i]);
        }
    }

    [Header("References")]
    [SerializeField] private Transform boneRoot;
    [SerializeField] private Transform boneEnd;

    [Header("Config")]
    [SerializeField] private int iterations = 10;
    [SerializeField] private float delta = 0.01f;
    [SerializeField][Range(0, 1)] private float snapBackStrength = 1.0f;
    private Vector2[] bonesPos_RS;
    private Vector2[] initBoneDir_RS;
    private Quaternion[] initBoneRot_RS;
    private Vector2 initTargetPos_RS;
    private Quaternion initTargetRot_RS;

    private void Start() => InitIK();

    private Vector2 TransformPosition_WorldToRS(Vector2 worldPos) => Quaternion.Inverse(boneRoot.rotation) * (worldPos - (Vector2)boneRoot.position);

    private Quaternion TransformRotation_WorldToRS(Quaternion worldRot) => Quaternion.Inverse(worldRot) * boneRoot.rotation;

    private Vector3 TransformPosition_RSToWorld(Vector2 position) => boneRoot.rotation * position + boneRoot.position;

    private Quaternion TransformRotation_RSToWorld(Quaternion rotation) => boneRoot.rotation * rotation;

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Loop upwards through transforms
        Transform current = boneEnd;
        for (int i = 0; i < BoneCount && current != null && current.parent != null; i++)
        {
            // Get vectors and distances
            Vector2 dir = current.parent.position - current.position;
            float scale = dir.magnitude * 0.1f;

            // Setup matrix then draw wireframe to parent
            Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector2.up, dir), new Vector3(scale, dir.magnitude, scale));
            Handles.color = Color.green;
            Handles.DrawWireCube(Vector2.up * 0.5f, Vector2.one);

            // Move upwards to parent
            current = current.parent;
        }
#endif
    }
}
