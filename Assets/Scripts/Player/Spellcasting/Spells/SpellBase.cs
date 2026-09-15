using System;
using UnityEngine;

public abstract class SpellBase : MonoBehaviour
{
    [Header("Spell Identity")]
    [SerializeField] private string spellName;
    [SerializeField] private string spellId;
    [SerializeField] private string description;
    [SerializeField] private float cooldownTime = 1f;
    public enum SpellType { Projectile, Targeted, PointTargeted, Self, Ally }
    [SerializeField] private SpellType spellType;
    [SerializeField] private TargetMapAsset spellMap;
    [SerializeField] private Sprite spellIcon;

    public string SpellName => spellName;
    public string SpellId => spellId;
    public string Description => description;
    public float CooldownTime => cooldownTime;
    public SpellType Type => spellType;
    public TargetMapAsset SpellMap => spellMap;
    public Sprite SpellIcon => spellIcon;

    [Header("Channeling Scoring")]
    [Tooltip("Weights used to score this spell's channeling sequence.")]
    public float perfectWeight = 1f;
    public float goodWeight = 0.75f;
    public float missWeight = 0f;

    [Header("References")]
    public SpellcastingCoordinator spellcastingCoordinator;
    public PlayerController player;
    public PointTargetScript pointTarget;
    public ChannelingPercentageCounterScript percentageCounter;

    public event Action OnSpellResolved;
    public ChannelingGameScript.ChannelingResult channelResult;

    protected void RaiseSpellResolved() => OnSpellResolved?.Invoke();

    public virtual void Start()
    {
        if (spellcastingCoordinator == null) spellcastingCoordinator = FindFirstObjectByType<SpellcastingCoordinator>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (pointTarget == null) pointTarget = FindFirstObjectByType<PointTargetScript>();
        if (percentageCounter == null) percentageCounter = FindFirstObjectByType<ChannelingPercentageCounterScript>();
    }

    protected float ComputeChannelScore(ChannelingGameScript.ChannelingResult result)
    {
        return ChannelingGameScript.ComputeScore(
            result.perfects, result.goods, result.misses, result.total,
            perfectWeight, goodWeight, missWeight);
    }

    // Called when the player starts casting the spell by pressing E.
    public abstract void OnCastStart();

    // Called when the channeling minigame ends and the spell enters its casting state.
    public abstract void OnChannelComplete(ChannelingGameScript.ChannelingResult result);

    // Called when the player confirms the spell target with the left mouse button.
    public abstract void OnSpellCast();

    // Called when the spell is cancelled by failure or by pressing E or Escape.
    public abstract void Cancel();
}