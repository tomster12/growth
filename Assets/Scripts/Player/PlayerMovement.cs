using System;
using UnityEngine;

public partial class PlayerMovement : MonoBehaviour, IFollowable
{
    public World ClosestWorld { get; private set; }
    public WorldSurfaceEdge ClosestEdge { get; private set; }
    public Vector2 GroundPos { get; private set; }
    public Vector2 GroundUpDir { get; private set; }
    public bool IsGrounded { get; private set; }
    public float SetMovementSlowdown { get; set; }
    public float SetVerticalLean { get; set; }
    public Action OnMoveEvent { get; set; } = delegate { };
    public float TargetBodyHeight => baseFeetHeight + inputVerticalLean * verticalLeanHeight;
    public float GroundedBodyHeight => TargetBodyHeight + groundedThreshold;
    public Vector2 GroundRightDir => new Vector2(GroundUpDir.y, -GroundUpDir.x);
    public Rigidbody2D RB => characterRB;
    public Transform Transform => characterRB.transform;

    public Vector2 GetJumpDir()
    {
        Vector2 jumpDir = GroundUpDir;

        // Add horizontal component to jump if moving fast enough
        float rightComponent = Vector2.Dot(characterRB.velocity, GroundRightDir);
        if (Mathf.Abs(rightComponent) > horizontalJumpThreshold) jumpDir += GroundRightDir * Mathf.Sign(rightComponent);

        return jumpDir.normalized;
    }

    [Header("References")]
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private WorldGenerator world;
    [SerializeField] private GravityObject characterGravity;
    [SerializeField] private Rigidbody2D characterRB;

    [Header("Config")]
    [SerializeField] private float rotationSpeed = 4.0f;
    [SerializeField] private float groundedThreshold = 0.3f;
    [SerializeField] private float legRaiseStrength = 0.2f;
    [SerializeField] private float legLowerStrength = 0.2f;
    [SerializeField] private float legForceThreshold = 0.1f;
    [SerializeField] private float verticalLeanHeight = 0.2f;
    [SerializeField] private float baseFeetHeight = 1.0f;
    [Space(10)]
    [SerializeField] private float airDrag = 1.0f;
    [SerializeField] private float airAngularDrag = 1.0f;
    [SerializeField] private float airMovementSpeed = 0.5f;
    [Space(10)]
    [SerializeField] private float groundDrag = 8.0f;
    [SerializeField] private float groundAngularDrag = 10.0f;
    [SerializeField] private float groundMovementSpeed = 2.0f;
    [Space(10)]
    [SerializeField] private float jumpForce = 15.0f;
    [SerializeField] private float jumpTimerMax = 0.5f;
    [SerializeField] private float horizontalJumpThreshold = 0.1f;
    [Space(10)]
    [SerializeField] private bool drawGizmos = false;

    private Vector2 inputDir;
    private Vector2 targetPosition;
    private bool inputJump;
    private float inputVerticalLean;
    private float jumpTimer = 0.0f;

    private void Start()
    {
        ClosestWorld = World.GetClosestWorldByRB(Transform.position, out Vector2 closestGroundPosition);
        ClosestEdge = ClosestWorld?.GetClosestEdge(Transform.position);
        playerCamera.SetModeFollow(this, true);
    }

    private void Update()
    {
        if (GameManager.IsPaused) return;
        HandleInput();
    }

    private void HandleInput()
    {
        // Take in input for movement
        inputDir = Vector3.zero;
        inputDir += Input.GetAxisRaw("Horizontal") * GroundRightDir;

        // Take in input for jump
        inputJump = Input.GetKey(KeyCode.Space);

        // Take in input for leaning
        inputVerticalLean = 0.0f;
        if (Input.GetAxisRaw("Vertical") != 0) inputVerticalLean = (int)Mathf.Sign(Input.GetAxisRaw("Vertical"));
        if (SetVerticalLean != 0.0f) inputVerticalLean = -SetVerticalLean;
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        FixedUpdateProperties();
        FixedUpdateMovement();
    }

    private void FixedUpdateProperties()
    {
        // Calculate closest world and ground position
        ClosestWorld = World.GetClosestWorldByRB(Transform.position, out Vector2 closestGroundPosition);
        ClosestEdge = ClosestWorld?.GetClosestEdge(Transform.position);
        GroundPos = closestGroundPosition;

        // Calculate ground variables
        Vector3 dir = GroundPos - (Vector2)Transform.position;
        IsGrounded = dir.magnitude < GroundedBodyHeight;
        GroundUpDir = -dir.normalized;
    }

    private void FixedUpdateMovement()
    {
        //  Rotate upwards
        Vector2 rotateTo = IsGrounded ? GroundUpDir : RB.velocity.normalized;
        float angleDiff = Vector2.SignedAngle(Transform.up, rotateTo) % 360;
        characterRB.AddTorque(angleDiff * rotationSpeed * Mathf.Deg2Rad);

        // ------ Grounded ------
        if (IsGrounded)
        {
            // Set grounded physical properties
            characterRB.drag = groundDrag;
            characterRB.angularDrag = groundAngularDrag;
            characterGravity.IsKinematic = false;

            // Apply force for height with legs
            targetPosition = GroundPos + (GroundUpDir * TargetBodyHeight);
            Vector2 dir = targetPosition - (Vector2)Transform.position;
            float upAmount = Vector2.Dot(GroundUpDir, dir);
            if (Mathf.Abs(upAmount) > legForceThreshold)
            {
                float legForce = upAmount > 0 ? legRaiseStrength : legLowerStrength;
                characterRB.AddForce(dir.normalized * legForce, ForceMode2D.Impulse);
            }

            // Apply force for movement with input
            float movementForce = groundMovementSpeed * (1.0f - SetMovementSlowdown);
            characterRB.AddForce(inputDir.normalized * movementForce, ForceMode2D.Impulse);

            // Apply jump force if needed
            if (inputJump && jumpTimer == 0.0f)
            {
                jumpTimer = jumpTimerMax;
                Vector2 jumpDir = GetJumpDir();
                characterRB.AddForce(jumpDir * jumpForce, ForceMode2D.Impulse);
            }

            // Update jump timer
            jumpTimer = Mathf.Max(jumpTimer - Time.deltaTime, 0.0f);
        }

        // ------ Air ------
        else
        {
            // Set air physical properties
            characterRB.drag = airDrag;
            characterRB.angularDrag = airAngularDrag;
            characterGravity.IsKinematic = true;

            // Reset jump timer to max
            jumpTimer = jumpTimerMax;

            // Force with input
            characterRB.AddForce(inputDir.normalized * airMovementSpeed, ForceMode2D.Impulse);
        }

        // Call OnMove
        OnMoveEvent();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Draw upwards
        //if (upDir != Vector2.zero)
        //{
        //    if (jumpTimer == 0.0f) Gizmos.color = Color.green;
        //    else Gizmos.color = Color.white;
        //    Gizmos.DrawLine(Transform.position, (Vector2)Transform.position + upDir);
        //}

        //// Draw to ground
        //if (groundPosition != Vector2.zero)
        //{
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawLine(Transform.position, groundPosition);
        //}

        // Draw to uncontrolled
        if (GroundPos != Vector2.zero)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(GroundPos + GroundUpDir * GroundedBodyHeight, 0.025f);
        }

        // Draw to target
        if (targetPosition != Vector2.zero && IsGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Transform.position, targetPosition);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(targetPosition, 0.025f);
        }
    }
}

public partial class PlayerMovement // IFollowable
{
    public Transform GetFollowTransform() => Transform;

    public Vector2 GetFollowPosition() => Transform.position;

    public Vector2 GetFollowUpwards() => -characterGravity.GravityDir;
}
