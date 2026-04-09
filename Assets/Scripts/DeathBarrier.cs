using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public Transform respawnPoint;

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
    }
}