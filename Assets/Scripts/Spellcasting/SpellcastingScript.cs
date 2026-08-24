using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class SpellcastingScript : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public ChannelingGameScript channelingGame;
    public TargetMapPlayer targetMapPlayer;
    public PointTargetScript pointTarget;
    public GameObject castingUI;
    public GameObject spellList;
    public SpellSelectUI spellSelectUI;
    public ChannelingPercentageCounterScript percentageCounter;
    public PercentageResultScript percentageResult;

    [Header("Spells")]
    public SpellBase[] spells;
    public int spellIndex = 0;

    public List<float> spellCooldowns = new List<float>();

    private SpellBase CurrentSpell =>
        (spells != null && spells.Length > 0)
            ? spells[Mathf.Clamp(spellIndex, 0, spells.Length - 1)]
            : null;

    public SpellBase.SpellType currentSpellType;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (channelingGame == null) channelingGame = FindFirstObjectByType<ChannelingGameScript>();

        if (targetMapPlayer == null) targetMapPlayer = FindFirstObjectByType<TargetMapPlayer>();

        if (pointTarget == null) pointTarget = FindFirstObjectByType<PointTargetScript>();

        if (spellSelectUI == null) spellSelectUI = FindFirstObjectByType<SpellSelectUI>();

        if (castingUI != null) castingUI.SetActive(false);
        else Debug.LogWarning("SpellcastingScript: castingUI GameObject is not assigned.");

        if (spellList != null) UpdateSpellList(); 
        else Debug.LogWarning("SpellcastingScript: SpellList GameObject is not assigned.");

        if (percentageCounter == null) percentageCounter = FindFirstObjectByType<ChannelingPercentageCounterScript>();

        if (percentageResult == null) percentageResult = FindFirstObjectByType<PercentageResultScript>();
    }

    void Update()
    {
        HandleSpellSelection();
        HandleCastInput();
        UpdateSpellCooldowns();
    }

    void HandleSpellSelection()
    { 
        if (player.castState != PlayerController.CastState.Idle) return;

        // método de seleção de feitiço por scroll do mouse
        float scrollValue = Mouse.current.scroll.ReadValue().y;
        
        if (scrollValue > 0f)
        {
            spellIndex = (spellIndex + 1) % spells.Length;
            Debug.Log($"Selected spell: {CurrentSpell?.spellName}");
        }
        else if (scrollValue < 0f)
        {
            spellIndex = (spellIndex - 1 + spells.Length) % spells.Length;
            Debug.Log($"Selected spell: {CurrentSpell?.spellName}");
        }

        currentSpellType = CurrentSpell.spellType;
    }

    void HandleCastInput()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame && player.pauseManager.isPaused == false)
        {
            if (player.castState == PlayerController.CastState.Idle)
            {
                if (spellCooldowns[spellIndex] <= 0)
                {
                    BeginChannel();
                    player.playerHUD.SetActive(false);
                }            
                else
                {
                    Debug.Log("Spell currently in cooldown. Time left: " + spellCooldowns[spellIndex]);
                }
            }
            else if (player.castState == PlayerController.CastState.Channeling
                   || player.castState == PlayerController.CastState.Aiming)
            {
                CancelCast();
                spellCooldowns[spellIndex] = CurrentSpell.cooldownTime;
            }
        }

        if (player.castState == PlayerController.CastState.Aiming 
        && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CastSpell();
        }
    }

    void BeginChannel()
    {
        SpellBase spell = CurrentSpell;
        if (spell == null || spell.spellMap == null)
        {
            Debug.LogWarning("SpellcastingScript: no spell selected or spell has no map assigned.");
            return;
        }

        if (castingUI != null)
        {
            foreach (Transform child in castingUI.transform)
            {
                if (child.CompareTag("Target"))
                    Destroy(child.gameObject);
            }
            castingUI.SetActive(true);
        }

        targetMapPlayer.LoadMap(spell.spellMap);
        channelingGame.ResetCast();
        channelingGame.OnChannelingResolved += HandleChannelResolved;
        percentageCounter?.SetValues(
            spell.perfectWeight, spell.goodWeight, spell.missWeight,
            1f, 1f, 1f,
            0f, 0f, 0f,
            1f);

        player.castState = PlayerController.CastState.Channeling;

        spell.OnCastStart();
    }

    void CastSpell()
    {
        player.castState = PlayerController.CastState.Casting;
        CurrentSpell.OnSpellResolved += HandleSpellResolved;
        CurrentSpell.OnSpellCast();
    }

    void HandleChannelResolved(ChannelingGameScript.ChannelingResult result)
    {
        channelingGame.OnChannelingResolved -=
            HandleChannelResolved;

        // Impede processamento de eventos atrasados.
        if (player.castState !=
            PlayerController.CastState.Channeling)
            return;

        if (currentSpellType ==
                SpellBase.SpellType.Projectile ||
            currentSpellType ==
                SpellBase.SpellType.Targeted)
        {
            player.castState =
                PlayerController.CastState.Aiming;
        }
        else
        {
            player.castState =
                PlayerController.CastState.Casting;

            player.playerHUD.SetActive(true);
        }

        CurrentSpell?.OnChannelComplete(result);

        percentageCounter?.RecalculatePercentage(result);

        percentageResult?.gameObject.SetActive(true);

        if (percentageCounter != null)
        {
            percentageResult?.ShowResult(
                percentageCounter.percentage,
                percentageCounter.percentageText.color
            );
        }

        channelingGame.ResetCast();

        targetMapPlayer.ResetMap();

        castingUI?.SetActive(false);
    }

    void HandleSpellResolved()
    {
        CurrentSpell.OnSpellResolved -= HandleSpellResolved;
        spellCooldowns[spellIndex] = CurrentSpell.cooldownTime;
        player.castState = PlayerController.CastState.Idle;
        if (castingUI != null) castingUI.SetActive(false);
    }

    void CancelCast()
    {
        channelingGame.OnChannelingResolved -= HandleChannelResolved;
        CurrentSpell?.Cancel();
        if (castingUI != null) castingUI.SetActive(false);
        player.castState = PlayerController.CastState.Idle;
        player.playerHUD.SetActive(true);
        Debug.Log("Cast cancelled.");
    }

    void UpdateSpellCooldowns()
    {
        for (int i = 0; i < spellCooldowns.Count; i++)
        {
            if (spellCooldowns[i] > 0f)
            {
                spellCooldowns[i] -= Time.deltaTime;
                if (spellCooldowns[i] < 0f)
                {
                    spellCooldowns[i] = 0f;
                }
            }
        }
    }

    public void ResetCooldowns()
    {
        for (int i = 0; i < spellCooldowns.Count; i++)
        {
            spellCooldowns[i] = 0;
        }
    }

    void UpdateSpellList()
    {
        // Reads SpellBase components from the SpellList GameObject and updates the spells array
        if (spellList != null)
        {
            SpellBase[] spellComponents = spellList.GetComponentsInChildren<SpellBase>();
            spells = spellComponents;
            spellCooldowns = new List<float>(new float[spells.Length]);
            if (spellSelectUI != null)
            {
                spellSelectUI.spellcastingScript = this;
                spellSelectUI.UpdateSpellList();
            }
        }
        else
        {
            Debug.LogWarning("SpellcastingScript: SpellList GameObject is not assigned.");
        }
    }
}