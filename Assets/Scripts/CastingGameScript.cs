using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CastingGameScript : MonoBehaviour
{
    public int currentTargetIndex = 0;
    public int hits;
    public int perfects;
    public int misses;
    public PlayerController player;

    public ComboCounter comboCounter;

    // Update is called once per frame
    void Update()
    {
        if (player.casting == true) 
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ResetCast();
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    
    public void RegisterHit(bool isPerfect)
    {
        hits++;

        if (isPerfect)
            perfects++;

        Debug.Log($"Hit registered | Hits: {hits} | Perfects: {perfects}");

        currentTargetIndex++;
        comboCounter.IncrementCombo();
    }

    public void RegisterMiss()
    {
        misses++;
        comboCounter.ResetCombo();
        Debug.Log($"Miss registered | Misses: {misses}");
        currentTargetIndex++;
    }

    public void ResetCast()
    {
        hits = 0;
        perfects = 0;
        misses = 0;
        currentTargetIndex = 0;
        comboCounter.ResetCombo();

        Debug.Log("New cast started");

        foreach (Transform child in transform)
        {
            if (child.CompareTag("Target"))
            {
                Destroy(child.gameObject);
            }
        }
    }
}
