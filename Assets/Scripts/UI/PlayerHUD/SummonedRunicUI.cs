using UnityEngine;
using UnityEngine.UI;

public class SummonedRunicUI : MonoBehaviour
{
    [SerializeField] private Image runicSprite;
    [SerializeField] private RunicHealthBarUI healthBar;
    [SerializeField] private Sprite defaultRunicSprite;
    [SerializeField] private RunicCommands runicCommands;

    void Awake()
    {
        if (healthBar == null)
            healthBar = GetComponentInChildren<RunicHealthBarUI>();

        if (runicCommands == null)
            runicCommands = GetComponentInChildren<RunicCommands>();

        if (runicSprite == null)
            runicSprite = transform.Find("RunicIcon/SpriteMask/RunicSprite").GetComponent<Image>();

        if (runicCommands == null)
            runicCommands = FindFirstObjectByType<RunicCommands>();
    }

    public void SetRunic(Runic runic)
    {
        healthBar.SetRunic(runic);
        runicCommands.SetSummonedRunic(runic);

        if (runic == null)
        {
            if (runicSprite != null) runicSprite.sprite = defaultRunicSprite;
            return;
        }

        if (runic.RunicIcon != null)
            runicSprite.sprite = runic.RunicIcon;
        else 
            runicSprite.sprite = defaultRunicSprite;
    }
}
