using UnityEngine;

// Mantém a referência ao inimigo "atual" sempre atualizada.
// A ligação do UnityEvent no Inspector aponta pra ESTE script,
// que nunca é destruído — só o campo currentTarget muda.
public class EnemyTargetHolder : MonoBehaviour
{
    public EnemyScript currentTarget;

    public void SetTarget(EnemyScript newTarget)
    {
        currentTarget = newTarget;
    }

    // Chamado pelo onInteract do Interactable via Inspector
    public void KillCurrentTarget()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("EnemyTargetHolder: nenhum alvo definido.");
            return;
        }

        Destroy(currentTarget.gameObject);
        currentTarget = null;
    }

    public void DamageCurrentTarget(float damage)
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("EnemyTargetHolder: nenhum alvo definido.");
            return;
        }

        currentTarget.TakeDamage(damage);
    }
}