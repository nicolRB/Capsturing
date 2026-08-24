using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChannelingGameScript : MonoBehaviour
{
    [Header("Casting Stats")]
    public int currentTargetIndex = 0;
    public int hits;
    public int perfects;
    public int misses;
    public int totalTargets;

    [Header("References")]
    public PlayerController player;
    public ComboCounter comboCounter;
    public TargetMapPlayer targetMapPlayer;

    private bool resolved = false;

    public struct ChannelingResult
    {
        public int perfects;
        public int goods;
        public int misses;
        public int total;
    }

    public System.Action<ChannelingResult> OnChannelingResolved;

    public void RegisterHit(bool isPerfect)
    {
        hits++;

        if (isPerfect)
            perfects++;

        currentTargetIndex++;

        comboCounter.IncrementCombo();

        CheckIfResolved();
    }

    public void RegisterMiss()
    {
        misses++;

        comboCounter.ResetCombo();

        currentTargetIndex++;

        CheckIfResolved();
    }

    public void ResetCast()
    {
        hits = 0;
        perfects = 0;
        misses = 0;
        currentTargetIndex = 0;
        resolved = false;

        comboCounter.ResetCombo();
        targetMapPlayer.ResetMap();

        totalTargets = targetMapPlayer.map != null
            ? targetMapPlayer.map.Count
            : 0;

        foreach (Transform child in transform)
        {
            if (child.CompareTag("Target"))
                Destroy(child.gameObject);
        }
    }

    private void CheckIfResolved()
    {
        if (resolved) return;
        if (totalTargets <= 0) return;
        if (currentTargetIndex < totalTargets) return;

        resolved = true;

        ChannelingResult result = new ChannelingResult
        {
            perfects = perfects,
            goods = hits - perfects,
            misses = misses,
            total = totalTargets
        };

        OnChannelingResolved?.Invoke(result);
    }

    public static float ComputeScore(
        int perfects,
        int goods,
        int misses,
        int total,
        float perfectWeight,
        float goodWeight,
        float missWeight,
        float perfectMultiplier = 1f,
        float goodMultiplier = 1f,
        float missMultiplier = 1f,
        float perfectModifier = 0f,
        float goodModifier = 0f,
        float missModifier = 0f,
        float globalMultiplier = 1f)
    {
        if (total <= 0)
            return 0f;

        float maxPossible =
            total *
            Mathf.Max(
                perfectWeight,
                goodWeight,
                missWeight,
                0.0001f
            );

        float perfectPerHit =
            (perfectWeight / maxPossible) * perfectMultiplier
            + perfectModifier;

        float goodPerHit =
            (goodWeight / maxPossible) * goodMultiplier
            + goodModifier;

        float missPerHit =
            (missWeight / maxPossible) * missMultiplier
            + missModifier;

        float score =
            perfects * perfectPerHit +
            goods * goodPerHit +
            misses * missPerHit;

        score *= globalMultiplier;

        return Mathf.Clamp01(score);
    }
}