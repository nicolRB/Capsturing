using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();

            // Reset velocity so player doesn't keep falling
            if (rb != null)
                rb.linearVelocity = Vector3.zero;

            // Teleport player
            other.transform.position = respawnPoint.position;
        }

        if (other.CompareTag("Wild") || other.CompareTag("Tamed"))
        {
            Runic runic = other.GetComponent<Runic>();

            Debug.Log(runic.gameObject + " has fallen out of the world.");

            runic.Die();
        }
    }
}