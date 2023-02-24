
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;


public interface IFollowable
{
    public Transform GetFollowTransform();
    public Vector2 GetFollowUpwards();
}


public class CameraController : MonoBehaviour
{
    public enum CameraMode { FREE, FOLLOW };

    [Header("References")]
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] private Transform mainPosition;
    
    [Header("Config")]
    [SerializeField] private int zoomLevel = 1;
    [SerializeField] private int zoomLevelMin = 1;
    [SerializeField] private int zoomLevelMax = 8;
    [SerializeField] private float freeMovementAcc = 1.5f;
    [SerializeField] private float freeMovementDamping = 0.75f;
    [SerializeField] private float freeMovementMaxSpeed = 3.0f;
    [SerializeField] private float followRotationSpeed = 3.0f;

    public CameraMode mode { get; private set; }
    private IFollowable follow;
    private Vector3 movementVelocity = Vector3.zero;


    private void Awake()
    {
        SetModeFree();
    }

    private void Start()
    {
        UpdateCameraZoom();
    }


    private void Update()
    {
        // Handle scrolling
        var scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollWheelInput != 0)
        {
            zoomLevel += Mathf.RoundToInt(scrollWheelInput * 10);
            zoomLevel = Mathf.Clamp(zoomLevel, zoomLevelMin, zoomLevelMax);
            UpdateCameraZoom();
        }

        // Handle main movement
        if (mode == CameraMode.FREE) UpdateMovementFree();
        else if (mode == CameraMode.FOLLOW) UpdateMovementFollow();
    }

    private void UpdateMovementFree()
    {
        // Handle camera movement
        float horz = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");
        movementVelocity += Vector3.right * horz * freeMovementAcc / zoomLevel;
        movementVelocity += Vector3.up * vert * freeMovementAcc / zoomLevel;

        // Constrain, move camera, apply damping
        movementVelocity = Vector2.ClampMagnitude(movementVelocity, freeMovementMaxSpeed / zoomLevel);
        movementVelocity *= freeMovementDamping;
        mainPosition.position += movementVelocity;
    }

    private void UpdateMovementFollow()
    {
        // Follow object position
        Vector2 pos = follow.GetFollowTransform().position;
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        // Follow object rotation
        transform.up = Vector2.Lerp(transform.up, follow.GetFollowUpwards(), Time.deltaTime * followRotationSpeed);
    }


    private void UpdateCameraZoom()
    {
        // Set the zoom using zoom level
        pixelPerfectCamera.refResolutionX = 2 * Mathf.FloorToInt(0.5f * Screen.width / zoomLevel);
        pixelPerfectCamera.refResolutionY = 2 * Mathf.FloorToInt(0.5f * Screen.height / zoomLevel);
    }


    public void SetModeFollow(IFollowable follow)
    {
        mode = CameraMode.FOLLOW;
        this.follow = follow;
    }

    public void SetModeFree()
    {
        mode = CameraMode.FREE;
    }
}
