using UnityEngine;

public class HPBar : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color tameColor;
    [SerializeField] private Color hostileColor;
    [SerializeField] private Color threatColor;
    [SerializeField] private Color neutralColor;

    [Header("References")]
    [SerializeField] private Runic ownerRunic;
    [SerializeField] private SpriteRenderer frameBar;
    [SerializeField] private SpriteRenderer hpBar;

    private bool tame;
    private Vector3 hpBarInitialLocalPos;

    void Start()
    {
        if (ownerRunic == null)
            ownerRunic = GetComponentInParent<Runic>();

        if (hpBar == null)
        {
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.transform != transform)
                {
                    hpBar = renderer;
                    break;
                }
            }
        }

        frameBar = GetComponent<SpriteRenderer>();
        hpBarInitialLocalPos = hpBar.transform.localPosition;

        UpdateBarColor();
    }

    void Update()
    {
        if (ownerRunic != null)
        {
            float healthPercent = (float)ownerRunic.CurrentHP / ownerRunic.MaxHP;
            healthPercent = Mathf.Clamp01(healthPercent);

            float maxHPLength = ownerRunic.MaxHP / 10f;
            float currentHPMaxLength = maxHPLength - 0.1f;
            maxHPLength = Mathf.Clamp(maxHPLength, 0, 20);
            currentHPMaxLength = Mathf.Clamp(currentHPMaxLength, 0, 19.9f);

            Vector2 frameSize = frameBar.size;
            frameSize.x = maxHPLength;
            frameBar.size = frameSize;

            float currentHPLength = currentHPMaxLength * healthPercent;
            Vector2 currentBarSize = hpBar.size;
            currentBarSize.x = currentHPLength;
            hpBar.size = currentBarSize;

            float positionDifference = (currentHPMaxLength - currentHPLength) / 2;
            hpBar.transform.localPosition = new Vector3 (
                hpBarInitialLocalPos.x - positionDifference, 
                hpBarInitialLocalPos.y, 
                hpBarInitialLocalPos.z
            );
        }
    }

    public void UpdateBarColor()
    {
        if (ownerRunic.TameState == RunicState.Tamed)
        {
            hpBar.color = tameColor;
            return;
        }

        if (ownerRunic.Combat != null && ownerRunic.Combat.AttackingPlayer)
        {
            hpBar.color = hostileColor;
        }
        else if (ownerRunic.ActState == BehaviorState.Threatening 
              || ownerRunic.ActState == BehaviorState.Fighting
              || ownerRunic.ActState == BehaviorState.Hunting)
        {
            hpBar.color = threatColor;
        }
        else
        {
            hpBar.color = neutralColor;
        }
    }
}
