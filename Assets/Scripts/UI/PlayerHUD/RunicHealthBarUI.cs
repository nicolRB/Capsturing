using UnityEngine;
using UnityEngine.UI;

public class RunicHealthBarUI : MonoBehaviour
{
    [SerializeField] private Runic runic;
    [SerializeField] private RectTransform frame;
    [SerializeField] private RectTransform hpBarMask;
    [SerializeField] private RectTransform hpBar;
    [SerializeField] private Image hpBarImage;

    void Awake()
    {
        frame = GetComponent<RectTransform>();

        if (hpBarMask == null)
        {
            foreach (RectTransform rect in GetComponentsInChildren<RectTransform>())
            {
                if (rect.transform != transform)
                {
                    hpBarMask = rect;
                    break;
                }
            }
        }

        if (hpBar == null && hpBarMask != null)
        {
            foreach (RectTransform rect in GetComponentsInChildren<RectTransform>())
            {
                if (rect.transform != transform && rect.transform != hpBarMask.transform)
                {
                    hpBar = rect;
                    break;
                }
            }
        }

        if (hpBarImage == null && hpBar != null)
            hpBarImage = hpBar.GetComponent<Image>();
    }

    void Update()
    {
        if (runic != null)
        {
            float healthPercent = (float)runic.CurrentHP / runic.MaxHP;
            healthPercent = Mathf.Clamp01(healthPercent);

            float maxHPLength = runic.MaxHP * 10;
            float currentHPMaxLength = maxHPLength - 10;

            maxHPLength = Mathf.Max(maxHPLength, 100);
            currentHPMaxLength = Mathf.Max(currentHPMaxLength, 90);

            frame.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                maxHPLength
            );

            hpBarMask.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                currentHPMaxLength
            );

            hpBar.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                currentHPMaxLength
            );

            hpBarImage.fillAmount = (float)runic.CurrentHP / runic.MaxHP;
        }
    }

    public void SetRunic(Runic runic)
    {
        this.runic = runic;
    }
}
