
using UnityEngine;


public class PlayerController : MonoBehaviour, IFollowable
{
    [Header("References")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private WorldManager world;
    [SerializeField] private GravityObject gravityObj;
    [SerializeField] private Rigidbody2D rb;

    [Header("Config")]
    [SerializeField] private float rotationSpeed = 4.0f;
    [SerializeField] private float groundedHeight = 1.3f;
    [SerializeField] private float feetHeight = 1.0f;
    [SerializeField] private float feetStrength = 1.0f;
    [SerializeField] private float airDrag = 1.0f;
    [SerializeField] private float airAngularDrag = 1.0f;
    [SerializeField] private float airMovementSpeed = 0.5f;
    [SerializeField] private float groundDrag = 8.0f;
    [SerializeField] private float groundAngularDrag = 10.0f;
    [SerializeField] private float groundMovementSpeed = 2.0f;
    [SerializeField] private float jumpForce = 15.0f;
    [SerializeField] private float jumpTimerMax = 0.5f;

    private Vector3 groundPosition, groundDir, upDir;
    private Vector3 rightDir => new Vector3(upDir.y, -upDir.x, 0.0f);
    private Vector3 targetPosition;

    private Vector3 inputDir;
    private bool inputJump;
    private bool isGrounded;
    private float jumpTimer = 0.0f;


    private void Start()
    {
        cameraController.SetModeFollow(this);
    }


    private void Update()
    {
        // Take in input
        inputDir = Vector3.zero;
        inputDir += Input.GetAxisRaw("Horizontal") * rightDir;
        
        // Vertical movement while not grounded
        if (!isGrounded) inputDir += Input.GetAxisRaw("Horizontal") * rightDir;

        // Update input jump
        inputJump = Input.GetKey(KeyCode.Space);

        // Update jump timer
        jumpTimer = Mathf.Max(jumpTimer - Time.deltaTime, 0.0f);
    }

    private void FixedUpdate()
    {
        // Calculate ground positions
        groundPosition = world.GetClosestSurfacePoint(rb.transform.position);
        groundDir = groundPosition - rb.transform.position;
        isGrounded = groundDir.magnitude < groundedHeight;
        upDir = groundDir.normalized * -1;

        //  Rotate upwards
        float angleDiff = (Vector2.SignedAngle(rb.transform.up, upDir) - 0) % 360 + 0;
        rb.AddTorque(angleDiff * rotationSpeed * Mathf.Deg2Rad);
        
        // - While grounded
        if (isGrounded)
        {
            // Update variables
            rb.drag = groundDrag;
            rb.angularDrag = groundAngularDrag;
            gravityObj.isEnabled = false;

            // Force upwards with legs
            targetPosition = groundPosition + (upDir * feetHeight);
            rb.AddForce((targetPosition - rb.transform.position) * feetStrength, ForceMode2D.Impulse);
            
            // Force with input
            rb.AddForce(inputDir.normalized * groundMovementSpeed, ForceMode2D.Impulse);

            if (inputJump && jumpTimer == 0.0f)
            {
                rb.AddForce(upDir * jumpForce, ForceMode2D.Impulse);
                jumpTimer = jumpTimerMax;
            }
        }

        // - While in the air
        else
        {
            rb.drag = airDrag;
            rb.angularDrag = airAngularDrag;
            gravityObj.isEnabled = true;

            // Force with input
            rb.AddForce(inputDir.normalized * airMovementSpeed, ForceMode2D.Impulse);
            jumpTimer = 0.0f;
        }
    }


    public Transform GetFollowTransform() => transform;

    public Vector2 GetFollowUpwards() => upDir;


    private void OnDrawGizmos()
    {
        // Draw upwards
        if (upDir != Vector3.zero)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawLine(transform.position, transform.position + upDir);
        }

        // Draw to ground
        if (groundPosition != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, groundPosition);
        }

        // Draw to target
        if (targetPosition != Vector3.zero && isGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
