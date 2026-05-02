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
    public TargetMapPlayer targetMapPlayer;
    // Json target map file 
    private string targetMap = "Maps/MapaTeste"; 

    public int castingMode = 1; // 1 = TargetSpawner(random), 2 = TargetMap(predefined)

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame || 
        Keyboard.current.digit2Key.wasPressedThisFrame || 
        Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ResetCast();
        }
    
        // Pressing 1 activates TargetSpawner for testing (temporary)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            castingMode = 1;
            ResetCast();
            Debug.Log("Casting mode set to TargetSpawner (random)");
        }

        // Pressing 2 activates TargetMap for testing (temporary)
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            castingMode = 2;
            ResetCast();
            Debug.Log("Casting mode set to TargetMap (predefined)");
            targetMapPlayer.LoadMap(targetMap); // Reload the map to reset spawn times
        }

        if (player.casting == false)
        {
            castingMode = 0; // No casting
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
