using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionScript : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Camera playerCamera;

    [Header("Interaction Detection")]
    public float maxDistance = 7.5f;
    public float sphereRadius = 1f;
    public LayerMask interactableLayer; // layer onde ficam os interagíveis (ex: "Interactable" e "InteractableHighlight")

    [Header("Interaction Input")]
    public Key interactKey = Key.F;

    private Ray ray;
    public GameObject interactableTarget;
    private Interactable interactableComponent;
    private Highlight highlightTarget;

    void Start()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        Point();

        if (Keyboard.current[interactKey].wasPressedThisFrame
            && player.castState == PlayerController.CastState.Idle
            && interactableComponent != null)
        {
            interactableComponent.Interact();
        }
    }

    void Point()
    {
        ray = new Ray(playerCamera.transform.position, Quaternion.Euler(playerCamera.transform.eulerAngles.x,
            playerCamera.transform.eulerAngles.y, 0) * Vector3.forward);

        bool canTargetInteractable = player.castState == PlayerController.CastState.Idle;

        if (canTargetInteractable
            && Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, interactableLayer))
        {
            Highlight novoHighlight = hit.collider.gameObject.GetComponentInParent<Highlight>();
            Interactable novoInteractable = hit.collider.gameObject.GetComponentInParent<Interactable>();

            if (novoHighlight != null && novoInteractable != null)
            {
                if (highlightTarget != null && highlightTarget != novoHighlight)
                    highlightTarget.pointed = false;

                interactableTarget = hit.collider.gameObject;
                highlightTarget = novoHighlight;
                interactableComponent = novoInteractable;
                highlightTarget.pointed = true;
                return;
            }
        }

        ClearTarget();
    }

    void ClearTarget()
    {
        if (highlightTarget != null)
        {
            highlightTarget.pointed = false;
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
