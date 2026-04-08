using UnityEngine;
using UnityEngine.InputSystem;

public class CastingGameScript : MonoBehaviour
{
    public int currentTargetIndex = 0;
    public int hits;
    public int perfects;
    public int misses;

    // Update is called once per frame
    void Update()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ResetCast();
        }
    }

    
    public void RegisterHit(bool isPerfect)
    {
        hits++;

        if (isPerfect)
            perfects++;

        Debug.Log($"Hit registered | Hits: {hits} | Perfects: {perfects}");

        currentTargetIndex++;
    }

    public void RegisterMiss()
    {
        misses++;
        Debug.Log($"Miss registered | Misses: {misses}");
        currentTargetIndex++;
    }

    public void ResetCast()
    {
        hits = 0;
        perfects = 0;
        misses = 0;
        currentTargetIndex = 0;

        Debug.Log("New cast started");
    }
}
