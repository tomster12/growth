
using UnityEngine;
using UnityEditor;


public class IKFabrik_Seperated : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boneRoot;
    [SerializeField] private Transform boneEnd;

    [Header("Config")]
    [SerializeField] private int iterations = 10;
    [SerializeField] private float delta = 0.01f;
    [SerializeField][Range(0, 1)] private float snapBackStrength = 1.0f;

    public Vector3 targetPos;
    public Quaternion targetRot;
    public Vector3 polePos;
    public Quaternion poleRot;

    private int boneCount;
    private Transform[] bones;
    private float[] boneLengths;
    public float totalLength { get; private set; }

    private Vector3[] bonesPos_RS;
    private Vector3[] initBoneDir_RS;
    private Quaternion[] initBoneRot_RS;
    private Vector3 initTargetPos_RS;
    private Quaternion initTargetRot_RS;


    private void Awake() => InitIK();

    [ContextMenu("Init IK")]
    public void InitIK()
    {
        // Calculate bone count
        boneCount = 0;
        Transform current = boneEnd;
        while (current != boneRoot)
        {
            boneCount++;
            current = current.parent;
            if (current == null) throw new UnityException("Could not find start.");
        }

        // Declare bone variables
        bones = new Transform[boneCount];
        boneLengths = new float[boneCount - 1];
        totalLength = 0.0f;

        bonesPos_RS = new Vector3[boneCount];
        initBoneDir_RS = new Vector3[boneCount];
        initBoneRot_RS = new Quaternion[boneCount];
        initTargetPos_RS = TransformPosition_WorldToRS(targetPos);
        initTargetRot_RS = TransformRotation_WorldToRS(targetRot);

        // For each bone we should have
        current = boneEnd;
        for (int i = boneCount - 1; i >= 0; i--)
        {
            // Extract bone and loop upwards
            bones[i] = current;
            current = current.parent;

            // Update bone RS variables
            bonesPos_RS[i] = TransformPosition_WorldToRS(bones[i].position);

            // Update initial bone variables and lengths
            initBoneRot_RS[i] = TransformRotation_WorldToRS(bones[i].rotation);
            if (bones[i] == boneEnd)
            {
                initBoneDir_RS[i] = initTargetPos_RS - bonesPos_RS[i];
            }
            else
            {
                initBoneDir_RS[i] = bonesPos_RS[i + 1] - bonesPos_RS[i];
                boneLengths[i] = initBoneDir_RS[i].magnitude;
                totalLength += boneLengths[i];
            }
        }
    }


    public void LateUpdate() => ResolveIK();

    public void ResolveIK()
    {
        // Get positions and rotations
        for (int i = 0; i < bones.Length; i++) bonesPos_RS[i] = TransformPosition_WorldToRS(bones[i].position);
        Vector3 targetPos_RS = TransformPosition_WorldToRS(targetPos);
        Quaternion targetRot_RS = TransformRotation_WorldToRS(targetRot);

        // If cannot directly reach, straighten towards target
        Vector3 targetDir_RS = targetPos_RS - bonesPos_RS[0];
        if (targetDir_RS.sqrMagnitude >= (totalLength * totalLength))
        {
            targetDir_RS = targetDir_RS.normalized;
            for (int i = 0; i < boneCount - 1; i++)
            {
                bonesPos_RS[i + 1] = bonesPos_RS[i] + targetDir_RS * boneLengths[i];
            }
        }

        // Otherwise, apply inverse kinematics
        else
        {
            // Apply snap back
            for (int i = 0; i < boneCount - 1; i++)
            {
                bonesPos_RS[i + 1] = Vector3.Lerp(bonesPos_RS[i + 1], bonesPos_RS[i] + initBoneDir_RS[i], snapBackStrength);
            }

            // Iteratively solve
            for (int itr = 0; itr < iterations; itr++)
            {
                // Backwards kinematics: going backwards, drag each bone to next
                for (int i = boneCount - 1; i > 0; i--)
                {
                    if (i == boneCount - 1)
                    {
                        bonesPos_RS[i] = targetPos_RS;
                    }
                    else
                    {
                        Vector3 backDir = (bonesPos_RS[i] - bonesPos_RS[i + 1]).normalized;
                        bonesPos_RS[i] = bonesPos_RS[i + 1] + backDir * boneLengths[i];
                    }
                }

                // Forwards kinematics: going forward, pull each bone to previous
                for (int i = 1; i < boneCount; i++)
                {
                    Vector3 fwDir = (bonesPos_RS[i] - bonesPos_RS[i - 1]).normalized;
                    bonesPos_RS[i] = bonesPos_RS[i - 1] + fwDir * boneLengths[i - 1];
                }

                // Reached target so break
                if ((bonesPos_RS[boneCount - 1] - targetPos_RS).sqrMagnitude < (delta * delta)) break;
            }
        }

        // Rotate intermediate bones towards pole
        if (polePos != null)
        {
            Vector3 pole_RS = TransformPosition_WorldToRS(polePos);
            for (int i = 1; i < bonesPos_RS.Length - 1; i++)
            {
                Plane plane_RS = new Plane(bonesPos_RS[i + 1] - bonesPos_RS[i - 1], bonesPos_RS[i - 1]);
                Vector3 projectedPole_RS = plane_RS.ClosestPointOnPlane(pole_RS);
                Vector3 projectedBone_RS = plane_RS.ClosestPointOnPlane(bonesPos_RS[i]);
                float angle_RS = Vector3.SignedAngle(projectedBone_RS - bonesPos_RS[i - 1], projectedPole_RS - bonesPos_RS[i - 1], plane_RS.normal);
                bonesPos_RS[i] = Quaternion.AngleAxis(angle_RS, plane_RS.normal) * (bonesPos_RS[i] - bonesPos_RS[i - 1]) + bonesPos_RS[i - 1];
            }
        }

        // Set positions and rotations
        for (int i = 0; i < boneCount; i++)
        {
            if (bones[i] == boneEnd)
            {
                bones[i].rotation = TransformRotation_RSToWorld(Quaternion.Inverse(targetRot_RS) * initTargetRot_RS * Quaternion.Inverse(initBoneRot_RS[i]));
            }
            else
            {
                bones[i].rotation = TransformRotation_RSToWorld(Quaternion.FromToRotation(initBoneDir_RS[i], bonesPos_RS[i + 1] - bonesPos_RS[i]) * Quaternion.Inverse(initBoneRot_RS[i]));
            }
            bones[i].position = TransformPosition_RSToWorld(bonesPos_RS[i]);
        }
    }


    public void OnDrawGizmos()
    {
        // Loop upwards through transforms
        Transform current = boneEnd;
        for (int i = 0; i < boneCount && current != null && current.parent != null; i++)
        {
            // Get vectors and distances
            Vector3 dir = current.parent.position - current.position;
            float scale = dir.magnitude * 0.1f;

            // Setup matrix then draw wireframe to parent
            Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, dir), new Vector3(scale, dir.magnitude, scale));
            Handles.color = Color.green;
            Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);

            // Move upwards to parent
            current = current.parent;
        }
    }


    private Vector3 TransformPosition_WorldToRS(Vector3 worldPos) => Quaternion.Inverse(boneRoot.rotation) * (worldPos - boneRoot.position);
    private Quaternion TransformRotation_WorldToRS(Quaternion worldRot) => Quaternion.Inverse(worldRot) * boneRoot.rotation;

    private Vector3 TransformPosition_RSToWorld(Vector3 position) => boneRoot.rotation * position + boneRoot.position;
    private Quaternion TransformRotation_RSToWorld(Quaternion rotation) => boneRoot.rotation * rotation;
}
