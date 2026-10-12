using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Image healthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        healthBar = GetComponentInChildren<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (healthBar != null && player != null)
            healthBar.fillAmount = player.CurrentHP / player.MaxHP;
        else    
            Debug.LogError("Healthbar: player or healthbar missing");
    }
}
