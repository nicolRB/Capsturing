using System;
using UnityEngine;

public abstract class SpellBase : MonoBehaviour
{
    [Header("Spell Identity")]
    public string spellName;
    public string spellId;
    public string description;
    public float cooldownTime = 1f;
    public enum SpellType { Projectile, Targeted, Self, Ally }
    public SpellType spellType;
    public TargetMapAsset spellMap;
    public Sprite spellIcon;

    [Header("Channeling Scoring")]
    [Tooltip("Pesos usados para pontuar a canalização deste feitiço.")]
    public float perfectWeight = 1f;
    public float goodWeight = 0.75f;
    public float missWeight = 0f;

    [Header("References")]
    public SpellcastingScript spellcastingScript;
    public PlayerController player;
    public PointTargetScript pointTarget;
    public ChannelingPercentageCounterScript percentageCounter;

    public event Action OnSpellResolved;
    public ChannelingGameScript.ChannelingResult channelResult;

    protected void RaiseSpellResolved() => OnSpellResolved?.Invoke();

    public virtual void Start()
    {
        if (spellcastingScript == null) spellcastingScript = FindFirstObjectByType<SpellcastingScript>();
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

    // Chamado quando o jogador inicia o lançamento do feitiço (pressiona o botão E)
    public abstract void OnCastStart();

    // Chamado quando o jogador termina o minigame de canalização e entra no estado de lançamento do feitiço (alguns feitiços afetam um alvo
    // específico e alguns são disparados em uma direção)
    public abstract void OnChannelComplete(ChannelingGameScript.ChannelingResult result);

    // Chamado quando o jogador confirma o alvo do feitiço (pressiona o botão esquerdo do mouse) e o feitiço é lançado
    public abstract void OnSpellCast();

    // Chamado quando o feitiço é cancelado, seja por falha ou por ação do jogador (pressionar o botão E ou Esc)
    public abstract void Cancel();
}