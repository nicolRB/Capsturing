using UnityEngine;
using TMPro;

public class HPCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Runic creature;
    [SerializeField] private TextMeshPro hpText;

    // Mirror the tracked creature's current and maximum health in the UI.
    void Update()
    {
        if (hpText != null && creature != null)
        {
            hpText.text = $"{creature.CurrentHP}/{creature.MaxHP}";
        }
    }
}
