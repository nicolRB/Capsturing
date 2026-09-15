using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Detection")]
    [Tooltip("Maximum distance at which an interactable can be detected.")]
    [SerializeField] private float maxDistance = 7.5f;
    [Tooltip("Radius of the detection sphere cast.")]
    [SerializeField] private float sphereRadius = 1f;
    [Tooltip("Layers containing interactable objects and their highlights.")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("Interaction Input")]
    [SerializeField] private Key interactKey = Key.F;

    private Ray ray;
    private GameObject interactableTarget;
    private Interactable interactableComponent;
    private Highlight highlightTarget;

    public GameObject InteractableTarget => interactableTarget;

    void Start()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        Point();

        if (Keyboard.current[interactKey].wasPressedThisFrame
            && player.CastingState == PlayerController.CastState.Idle
            && interactableComponent != null)
        {
            interactableComponent.Interact();
        }
    }

    void Point()
    {
        ray = new Ray(playerCamera.transform.position, Quaternion.Euler(playerCamera.transform.eulerAngles.x,
            playerCamera.transform.eulerAngles.y, 0) * Vector3.forward);

        bool canTargetInteractable = player.CastingState == PlayerController.CastState.Idle;

        if (canTargetInteractable
            && Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, interactableLayer))
        {
            Highlight novoHighlight = hit.collider.gameObject.GetComponentInParent<Highlight>();
            Interactable novoInteractable = hit.collider.gameObject.GetComponentInParent<Interactable>();

            if (novoHighlight != null && novoInteractable != null)
            {
                if (highlightTarget != null && highlightTarget != novoHighlight)
                    highlightTarget.Toggle(false);

                interactableTarget = hit.collider.gameObject;
                highlightTarget = novoHighlight;
                interactableComponent = novoInteractable;
                highlightTarget.Toggle(true);
                return;
            }
        }

        ClearTarget();
    }

    void ClearTarget()
    {
        if (highlightTarget != null)
        {
            highlightTarget.Toggle(false);
            highlightTarget = null;
        }
        interactableTarget = null;
        interactableComponent = null;
    }

    void OnDrawGizmos()
    {
        if (playerCamera == null || player == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(ray.origin + ray.direction * maxDistance, sphereRadius);
        Gizmos.DrawWireSphere(ray.origin, sphereRadius);
    }
}
