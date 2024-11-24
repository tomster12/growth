using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using static UnityEngine.GraphicsBuffer;

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
    public Transform CameraTfm => cameraTfm;

    public void SetModeFree()
    {
        CameraModeState = CameraMode.Free;
    }

    public void SetModeFollow(IFollowable follow, bool set = false)
    {
        // Unsubscribe from previous follow
        if (this.follow != null) this.follow.OnMoveEvent -= OnTargetMove;

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
        this.follow.OnMoveEvent += OnTargetMove;
    }

    [Header("References")]
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera outsideUICamera;
    [SerializeField] private RenderTexture outsideUITexture;
    [SerializeField] private Transform cameraTfm;

    [Header("Config")]
    [SerializeField] private int zoomLevel = 1;
    [SerializeField] private int zoomLevelMin = 1;
    [SerializeField] private int zoomLevelMax = 8;
    [SerializeField] private float freeMovementAcc = 1.5f;
    [SerializeField] private float freeMovementDamping = 0.75f;
    [SerializeField] private float freeMovementMaxSpeed = 3.0f;
    [SerializeField] private float followRotationSpeed = 3.0f;
    [SerializeField] private float followRotationThreshold = 0.4f;

    private IFollowable follow;
    private Vector2 movementVelocity = Vector2.zero;

    private void Awake()
    {
        UpdateCamerasToZoom();
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
            UpdateCamerasToZoom();
            OnZoomChangeEvent(zoomLevel);
        }
    }

    [ContextMenu("Update Cameras To Zoom")]
    private void UpdateCamerasToZoom()
    {
        // Get screen resolution
        string[] res = UnityStats.screenRes.Split('x');
        float screenWidth = int.Parse(res[0]);
        float screenHeight = int.Parse(res[1]);

        // Round PP camera resolution to nearest even number
        pixelPerfectCamera.refResolutionX = 2 * Mathf.FloorToInt(0.5f * screenWidth / zoomLevel);
        pixelPerfectCamera.refResolutionY = 2 * Mathf.FloorToInt(0.5f * screenHeight / zoomLevel);

        // Update outside UI camera size to match PP cameras
        outsideUICamera.orthographicSize = (float)Mathf.FloorToInt(0.5f * screenHeight / zoomLevel) / 12.0f;
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
        movementVelocity += Vector2.right * horz * freeMovementAcc / zoomLevel;
        movementVelocity += Vector2.up * vert * freeMovementAcc / zoomLevel;

        // Constrain, move camera, apply damping
        movementVelocity = Vector2.ClampMagnitude(movementVelocity, freeMovementMaxSpeed / zoomLevel);
        movementVelocity *= freeMovementDamping;
        cameraTfm.position += (Vector3)movementVelocity;

        OnMoveEvent();
    }

    private void OnTargetMove()
    {
        if (CameraModeState != CameraMode.Follow) return;

        // Follow object position
        Vector2 pos = follow.GetFollowPosition();
        cameraTfm.position = Utility.WithZ(pos, cameraTfm.position.z);

        // Follow object rotation if difference is bigger than threshold
        float angle = Vector2.SignedAngle(cameraTfm.up, follow.GetFollowUpwards());
        if (Mathf.Abs(angle) > followRotationThreshold)
        {
            cameraTfm.up = Vector2.Lerp(cameraTfm.up, follow.GetFollowUpwards(), Time.deltaTime * followRotationSpeed);
        }

        OnMoveEvent();
    }
}
