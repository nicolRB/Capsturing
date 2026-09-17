using UnityEngine;

// Keeps a stable UnityEvent target while the referenced enemy changes.
public class EnemyTargetHolder : MonoBehaviour
{
    [Header("Current Target")]
    [SerializeField] private Runic currentTarget;

    public void SetTarget(Runic newTarget)
    {
        currentTarget = newTarget;
    }

    // Called by the Interactable onInteract UnityEvent.
    public void KillCurrentTarget()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("EnemyTargetHolder: no target is assigned.");
            return;
        }

        Destroy(currentTarget.gameObject);
        currentTarget = null;
    }

    public void DamageCurrentTarget(float damage)
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("EnemyTargetHolder: no target is assigned.");
            return;
        }

        currentTarget.TakeDamage(damage);
    }
}