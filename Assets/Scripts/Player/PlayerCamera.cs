using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public interface IFollowable
{
    public Action OnMoveEvent { get; set; }

    public Transform GetFollowTransform();

    public Vector2 GetFollowPosition();

    public Vector2 GetFollowUpwards();
}

public class PlayerCamera : MonoBehaviour
{
    public static Action<float> OnZoomChangeEvent = delegate { };

    public static Action OnMoveEvent = delegate { };

    public enum CameraMode
    { Free, Follow }

    public CameraMode CameraModeState { get; private set; }

    public void SetModeFree()
    {
        CameraModeState = CameraMode.Free;
    }

    public void SetModeFollow(IFollowable follow, bool set = false)
    {
        // Unsubscribe from previous follow
        if (this.follow != null) this.follow.OnMoveEvent -= XUpdateMovementFollow;

        CameraModeState = CameraMode.Follow;
        this.follow = follow;

        // Set position and rotation if set
        if (set)
        {
            Vector2 pos = follow.GetFollowTransform().position;
            cameraTfm.position = Utility.WithZ(pos, cameraTfm.position.z);
            cameraTfm.up = follow.GetFollowUpwards();
        }

        // Listen to OnMove
        // TODO: Check if this lags behind
        this.follow.OnMoveEvent += XUpdateMovementFollow;
    }

    [Header("References")]
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] private Transform cameraTfm;

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
        // Set initial zoom level
        pixelPerfectCamera.refResolutionX = 2 * Mathf.FloorToInt(0.5f * Screen.width / zoomLevel);
        pixelPerfectCamera.refResolutionY = 2 * Mathf.FloorToInt(0.5f * Screen.height / zoomLevel);
        SetModeFree();
    }

    private void Update()
    {
        if (GameManager.IsPaused) return;
        HandleInput();
    }

    private void HandleInput()
    {
        // Handle scrolling and update zoom level
        var scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollWheelInput != 0)
        {
            zoomLevel += Mathf.RoundToInt(scrollWheelInput * 10);
            zoomLevel = Mathf.Clamp(zoomLevel, zoomLevelMin, zoomLevelMax);
            pixelPerfectCamera.refResolutionX = 2 * Mathf.FloorToInt(0.5f * Screen.width / zoomLevel);
            pixelPerfectCamera.refResolutionY = 2 * Mathf.FloorToInt(0.5f * Screen.height / zoomLevel);
            OnZoomChangeEvent(zoomLevel);
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.IsPaused) return;
        if (CameraModeState == CameraMode.Free) FixedUpdateMovementFree();
    }

    private void FixedUpdateMovementFree()
    {
        if (CameraModeState != CameraMode.Free) return;

        // Handle camera movement
        float horz = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");
        movementVelocity += Vector3.right * horz * freeMovementAcc / zoomLevel;
        movementVelocity += Vector3.up * vert * freeMovementAcc / zoomLevel;

        // Constrain, move camera, apply damping
        movementVelocity = Vector2.ClampMagnitude(movementVelocity, freeMovementMaxSpeed / zoomLevel);
        movementVelocity *= freeMovementDamping;
        cameraTfm.position += movementVelocity;

        OnMoveEvent();
    }

    private void XUpdateMovementFollow()
    {
        if (CameraModeState != CameraMode.Follow) return;

        // Follow object position
        Vector2 pos = follow.GetFollowPosition();
        cameraTfm.position = Utility.WithZ(pos, cameraTfm.position.z);

        // Follow object rotation
        cameraTfm.up = Vector2.Lerp(cameraTfm.up, follow.GetFollowUpwards(), Time.deltaTime * followRotationSpeed);

        OnMoveEvent();
    }
}
