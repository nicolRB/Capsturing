using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Image healthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (healthBar == null)
        {
            foreach (Transform child in transform)
            {
                healthBar = child.GetComponentInChildren<Image>();
                if (healthBar != null)
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (healthBar != null && player != null)
            healthBar.fillAmount = (float)player.CurrentHP / (float)player.MaxHP;
        else    
            Debug.LogError("Healthbar: player or healthbar missing / not referenced");
    }
}