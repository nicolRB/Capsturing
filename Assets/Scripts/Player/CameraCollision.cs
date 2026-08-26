using UnityEngine;
using UnityEngine.InputSystem;

public class CameraCollision : MonoBehaviour
{
    public Transform pivot;

    [Header("Distance")]
    public float distance = 4f;
    public float minDistance = 0.5f;
    public float smoothSpeed = 10f;

    [Header("Collision")]
    public LayerMask collisionMask;
    public float sphereRadius = 0.3f;

    [Header("Offset (Shoulder)")]
    public float sideOffset = 0.5f;
    public float heightOffset = 0.3f;

    [Header("Camera Target")]
    public float lookSideOffset = 0.3f;
    public float lookHeightOffset = 0.5f;

    [Header("Shoulder Smooth")]
    public float shoulderSmoothSpeed = 8f;

    private float currentDistance;

    // Target values for smooth interpolation
    private float targetSideOffset;
    private float targetLookSideOffset;

    void Start()
    {
        // Initialize camera distance
        currentDistance = distance;

        // Initialize shoulder targets
        targetSideOffset = sideOffset;
        targetLookSideOffset = lookSideOffset;
    }

    void Update()
    {
        // Toggle shoulder side when Q is pressed
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            targetSideOffset = -targetSideOffset;
            targetLookSideOffset = -targetLookSideOffset;
        }
    }

    void LateUpdate()
    {
        Vector3 origin = pivot.position;

        // Direction from pivot to camera (backwards)
        Vector3 backDir = -pivot.forward;

        float targetDistance = distance;
        RaycastHit hit;

        // Detect obstacles using sphere cast
        if (Physics.SphereCast(
            origin,
            sphereRadius,
            backDir,
            out hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            targetDistance = Mathf.Clamp(hit.distance, minDistance, distance);
        }

        // Smoothly interpolate camera distance
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * smoothSpeed);

        // Smoothly interpolate shoulder offset (left/right)
        sideOffset = Mathf.Lerp(sideOffset, targetSideOffset, Time.deltaTime * shoulderSmoothSpeed);
        lookSideOffset = Mathf.Lerp(lookSideOffset, targetLookSideOffset, Time.deltaTime * shoulderSmoothSpeed);

        // Base camera position (behind the player)
        Vector3 basePosition = origin + backDir * currentDistance;

        // Apply shoulder offset (horizontal + vertical)
        Vector3 finalPosition =
            basePosition +
            pivot.right * sideOffset +
            pivot.up * heightOffset;

        transform.position = finalPosition;
    }
}