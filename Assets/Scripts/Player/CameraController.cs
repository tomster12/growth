
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;


public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] Transform mainPosition;
    
    [Header("Config")]
    [SerializeField] int zoomLevel = 1;
    [SerializeField] int zoomLevelMin = 1;
    [SerializeField] int zoomLevelMax = 8;
    [SerializeField] float movementAcc = 1.5f;
    [SerializeField] float movementDamping = 0.75f;
    [SerializeField] float movementMaxSpeed = 3.0f;

    private Vector3 movementVelocity = Vector3.zero;


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

        // Handle camera movement
        float horz = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");
        movementVelocity += Vector3.right * horz * movementAcc;
        movementVelocity += Vector3.up * vert * movementAcc;

        // Constrain, move camera, apply damping
        movementVelocity = Vector2.ClampMagnitude(movementVelocity, movementMaxSpeed);
        movementVelocity *= movementDamping;
        mainPosition.position += movementVelocity;
    }


    private void UpdateCameraZoom()
    {
        // Set the zoom using zoom level
        pixelPerfectCamera.refResolutionX = 2 * Mathf.FloorToInt(0.5f * Screen.width / zoomLevel);
        pixelPerfectCamera.refResolutionY = 2 * Mathf.FloorToInt(0.5f * Screen.height / zoomLevel);
    }
}
