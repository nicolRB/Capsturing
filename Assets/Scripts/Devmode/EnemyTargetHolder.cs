using UnityEngine;
using TMPro;

// Keeps a stable UnityEvent target while the referenced enemy changes.
public class EnemyTargetHolder : MonoBehaviour
{
    [Header("Current Target")]
    [SerializeField] private Runic currentTarget;
    [SerializeField] private TextMeshPro damageValueDisplay;
    [SerializeField] private int damage = 1;

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

    public void DamageCurrentTarget(int damage)
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("EnemyTargetHolder: no target is assigned.");
            return;
        }
        currentTarget.TakeDamage(damage);
    }

    public void DamageCurrentTarget()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("EnemyTargetHolder: no target is assigned.");
            return;
        }
        currentTarget.TakeDamage(damage);
    }

    public void IncreaseDamage(int amount)
    {
        damage += amount;
        damageValueDisplay.text = damage.ToString();
    }
    
    public void ReduceDamage(int amount)
    {
        damage -= amount;
        damageValueDisplay.text = damage.ToString();
    }
}