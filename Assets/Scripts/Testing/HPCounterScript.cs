using UnityEngine;
using TMPro;

public class HPCounterScript : MonoBehaviour
{
    public Runic creature;
    public TextMeshPro hpText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (hpText != null && creature != null)
        {
            hpText.text = $"{creature.currentHP}/{creature.maxHP}";
        }
    }
}
