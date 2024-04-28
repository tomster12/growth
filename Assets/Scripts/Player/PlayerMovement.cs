using UnityEngine;

public class PlayerMovement : MonoBehaviour, IFollowable
{
    public float FeetHeight => baseFeetHeight + inputVerticalLean * verticalLeanHeight;
    public float GroundedHeight => FeetHeight + groundedSpacing;
    public Vector2 RightDir => new Vector2(UpDir.y, -UpDir.x);
    public Rigidbody2D RB => characterRB;
    public Transform Transform => characterRB.transform;
    public World ClosestWorld { get; private set; }
    public Vector2 GroundPosition { get; private set; }
    public Vector2 GroundDir { get; private set; }
    public Vector2 UpDir { get; private set; }
    public bool IsGrounded { get; private set; }
    public float MovementSlowdown { get; set; }
    public float OverrideVerticalLean { get; set; }

    public Transform GetFollowTransform() => characterRB.transform;

    public Vector2 GetFollowPosition() => characterRB.position;

    public Vector2 GetFollowUpwards() => UpDir;

    public Vector2 GetJumpDir()
    {
        Vector2 jumpDir = UpDir;

        // Add horizontal component to jump if moving fast enough
        float rightComponent = Vector2.Dot(characterRB.velocity, RightDir);
        if (rightComponent > horizontalJumpThreshold) jumpDir += RightDir * Mathf.Sign(rightComponent);

        return jumpDir.normalized;
    }

    [Header("References")]
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private WorldGenerator world;
    [SerializeField] private GravityObject characterGravity;
    [SerializeField] private Rigidbody2D characterRB;

    [Header("Config")]
    [SerializeField] private float rotationSpeed = 4.0f;
    [SerializeField] private float groundedSpacing = 0.3f;
    [SerializeField] private float feetRaiseStrength = 1.0f;
    [SerializeField] private float feetLowerStrength = 0.1f;
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
        ClosestWorld = World.GetClosestWorld(characterRB.transform.position, out Vector2 closestGroundPosition);
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
        inputDir += Input.GetAxisRaw("Horizontal") * RightDir;

        // Take in input for jump
        inputJump = Input.GetKey(KeyCode.Space);

        // Take in input for leaning
        inputVerticalLean = 0.0f;
        if (Input.GetAxisRaw("Vertical") != 0) inputVerticalLean = (int)Mathf.Sign(Input.GetAxisRaw("Vertical"));
        if (OverrideVerticalLean != 0.0f) inputVerticalLean = -OverrideVerticalLean;
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        FixedUpdateDynamics();
        FixedUpdateMovement();
    }

    private void FixedUpdateDynamics()
    {
        // Calculate closest world and ground position
        ClosestWorld = World.GetClosestWorld(characterRB.transform.position, out Vector2 closestGroundPosition);
        GroundPosition = closestGroundPosition;

        // Calculate ground variables
        GroundDir = GroundPosition - (Vector2)characterRB.transform.position;
        IsGrounded = GroundDir.magnitude < GroundedHeight;
        UpDir = GroundDir.normalized * -1;
    }

    private void FixedUpdateMovement()
    {
        //  Rotate upwards
        Vector2 rotateTo = IsGrounded ? UpDir : RB.velocity.normalized;
        float angleDiff = Vector2.SignedAngle(characterRB.transform.up, rotateTo) % 360;
        characterRB.AddTorque(angleDiff * rotationSpeed * Mathf.Deg2Rad);

        // ------ Grounded ------
        if (IsGrounded)
        {
            // Set grounded physical properties
            characterRB.drag = groundDrag;
            characterRB.angularDrag = groundAngularDrag;
            characterGravity.IsEnabled = false;

            // Apply force for height with legs
            targetPosition = GroundPosition + (UpDir * FeetHeight);
            Vector2 dir = targetPosition - (Vector2)characterRB.transform.position;
            float upComponent = Vector2.Dot(UpDir, dir);
            characterRB.AddForce(dir * (upComponent > 0 ? feetRaiseStrength : feetLowerStrength), ForceMode2D.Impulse);

            // Apply force for movement with input
            float speed = groundMovementSpeed * (1.0f - MovementSlowdown);
            characterRB.AddForce(inputDir.normalized * speed, ForceMode2D.Impulse);

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
            characterGravity.IsEnabled = true;

            // Reset jump timer to max
            jumpTimer = jumpTimerMax;

            // Force with input
            characterRB.AddForce(inputDir.normalized * airMovementSpeed, ForceMode2D.Impulse);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Draw upwards
        //if (upDir != Vector2.zero)
        //{
        //    if (jumpTimer == 0.0f) Gizmos.color = Color.green;
        //    else Gizmos.color = Color.white;
        //    Gizmos.DrawLine(characterRB.transform.position, (Vector2)characterRB.transform.position + upDir);
        //}

        //// Draw to ground
        //if (groundPosition != Vector2.zero)
        //{
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawLine(characterRB.transform.position, groundPosition);
        //}

        // Draw to uncontrolled
        if (GroundPosition != Vector2.zero)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(GroundPosition + UpDir * GroundedHeight, 0.025f);
        }

        // Draw to target
        if (targetPosition != Vector2.zero && IsGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(characterRB.transform.position, targetPosition);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(targetPosition, 0.025f);
        }
    }
}
