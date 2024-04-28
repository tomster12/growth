using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public interface IFollowable
{
    public Transform GetFollowTransform();

    public Vector2 GetFollowPosition();

    public Vector2 GetFollowUpwards();
}

public class PlayerCamera : MonoBehaviour
{
    public enum CameraMode
    { Free, Follow }

    public CameraMode CameraModeState { get; private set; }

    public void SetModeFollow(IFollowable follow, bool set = false)
    {
        CameraModeState = CameraMode.Follow;
        this.follow = follow;

        // Set values
        if (set)
        {
            Vector2 pos = follow.GetFollowTransform().position;
            controlledTransform.position = new Vector3(pos.x, pos.y, controlledTransform.position.z);
            controlledTransform.up = follow.GetFollowUpwards();
        }
    }

    public void SetModeFree()
    {
        CameraModeState = CameraMode.Free;
    }

    [Header("References")]
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] private Transform controlledTransform;

    [Header("Config")]
    [SerializeField] private int zoomLevel = 1;
    [SerializeField] private int zoomLevelMin = 1;
    [SerializeField] private int zoomLevelMax = 8;
    [SerializeField] private float freeMovementAcc = 1.5f;
    [SerializeField] private float freeMovementDamping = 0.75f;
    [SerializeField] private float freeMovementMaxSpeed = 3.0f;
    [SerializeField] private float followRotationSpeed = 3.0f;
    private IFollowable follow;
    private Vector3 movementVelocity = Vector3.zero;

    private void Awake()
    {
        SetModeFree();
    }

    private void Start()
    {
        FixedUpdateInputZoom();
    }

    private void Update()
    {
        if (GameManager.IsPaused) return;
        HandleInput();
    }

    private void HandleInput()
    {
        // Handle scrolling
        var scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollWheelInput != 0)
        {
            zoomLevel += Mathf.RoundToInt(scrollWheelInput * 10);
            zoomLevel = Mathf.Clamp(zoomLevel, zoomLevelMin, zoomLevelMax);
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        if (CameraModeState == CameraMode.Free) FixedUpdateMovementFree();
        else if (CameraModeState == CameraMode.Follow) FixedUpdateMovementFollow();
        FixedUpdateInputZoom();
    }

    private void FixedUpdateMovementFree()
    {
        // Handle camera movement
        float horz = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");
        movementVelocity += Vector3.right * horz * freeMovementAcc / zoomLevel;
        movementVelocity += Vector3.up * vert * freeMovementAcc / zoomLevel;

        // Constrain, move camera, apply damping
        movementVelocity = Vector2.ClampMagnitude(movementVelocity, freeMovementMaxSpeed / zoomLevel);
        movementVelocity *= freeMovementDamping;
        controlledTransform.position += movementVelocity;
    }

    private void FixedUpdateMovementFollow()
    {
        // Follow object position
        Vector2 pos = follow.GetFollowPosition();
        controlledTransform.position = new Vector3(pos.x, pos.y, controlledTransform.position.z);

        // Follow object rotation
        controlledTransform.up = Vector2.Lerp(controlledTransform.up, follow.GetFollowUpwards(), Time.deltaTime * followRotationSpeed);
    }

    private void FixedUpdateInputZoom()
    {
        // Set the zoom using zoom level
        pixelPerfectCamera.refResolutionX = 2 * Mathf.FloorToInt(0.5f * Screen.width / zoomLevel);
        pixelPerfectCamera.refResolutionY = 2 * Mathf.FloorToInt(0.5f * Screen.height / zoomLevel);
    }
}
