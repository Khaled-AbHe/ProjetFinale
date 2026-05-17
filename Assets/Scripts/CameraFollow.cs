using UnityEngine;

/// <summary>
/// Smooth 2D camera that follows a target (usually the player).
/// Attach this to your Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;               // Drag your player GameObject here

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;         // Higher = snappier; lower = floatier
    public Vector2 offset = new Vector2(0f, 1f); // Offset from player position

    [Header("Camera Bounds (optional)")]
    public bool useBounds = false;
    public float minX, maxX, minY, maxY;  // World-space level edges (not camera center)

    private Vector3 desiredPosition;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate() // LateUpdate runs after all Updates – ideal for cameras
    {
        if (target == null) return;

        // Calculate where the camera wants to be
        desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z            // Keep the camera's Z fixed
        );

        // Clamp to level bounds if enabled
        if (useBounds)
        {
            // How much world space the camera shows from its center to each edge.
            // These change automatically with different screen sizes and aspect ratios.
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth  = cam.orthographicSize * cam.aspect;

            // Inset the clamp by the camera's half-size so the screen EDGE
            // lines up with minX/maxX/minY/maxY, not the camera center.
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX + camHalfWidth,  maxX - camHalfWidth);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY + camHalfHeight, maxY - camHalfHeight);
        }

        // Smoothly move toward the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}