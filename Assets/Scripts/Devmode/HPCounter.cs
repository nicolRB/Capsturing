using UnityEngine;
using TMPro;

public class HPCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Runic creature;
    [SerializeField] private TextMeshPro hpText;
    [SerializeField] private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
        if (creature == null)
            creature = GetComponentInParent<Runic>();
    }

    // Mirror the tracked creature's current and maximum health in the UI.
    void Update()
    {
        if (hpText != null && creature != null)
        {
            hpText.text = $"{creature.CurrentHP}/{creature.MaxHP}";
        }
    }

    void LateUpdate()
    {
        if (playerCamera != null)
        {
            transform.LookAt(
                transform.position + playerCamera.transform.forward,
                playerCamera.transform.up);
        }
    }
}
