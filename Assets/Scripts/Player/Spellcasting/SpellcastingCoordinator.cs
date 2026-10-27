using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpellcastingCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private ChannelingGameCoordinator channelingGame;
    [SerializeField] private TargetMapPlayer targetMapPlayer;
    [SerializeField] private PointTargetScript pointTarget;
    [SerializeField] private GameObject castingUI;
    [SerializeField] private GameObject spellList;
    [SerializeField] private SpellSelectUI spellSelectUI;
    [SerializeField] private ChannelingPercentageCounterScript percentageCounter;
    [SerializeField] private PercentageResultUI percentageResult;

    public GameObject CastingUI => castingUI;
    public TargetMapPlayer TargetMapPlayer => targetMapPlayer;
    public ChannelingGameCoordinator ChannelingGame => channelingGame;
    public ChannelingPercentageCounterScript PercentageCounter => percentageCounter;
    public PercentageResultUI PercentageResult => percentageResult;

    [Header("Spells")]
    [SerializeField] private SpellBase[] spells;
    [SerializeField] private int spellIndex = 0;

    public SpellBase[] Spells => spells;
    public int SpellIndex => spellIndex;

    [SerializeField] private List<float> spellCooldowns = new List<float>();

    public List<float> SpellCooldowns => spellCooldowns;

    private SpellBase CurrentSpell =>
        (spells != null && spells.Length > 0)
            ? spells[Mathf.Clamp(spellIndex, 0, spells.Length - 1)]
            : null;

    [SerializeField] private SpellBase.SpellType currentSpellType;

    public SpellBase.SpellType CurrentSpellType => currentSpellType;

    void Awake()
    {
        if (spellList != null)
        {
            spells = spellList.GetComponentsInChildren<SpellBase>();
            spellCooldowns = new List<float>(new float[spells.Length]);
        }
    }

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (channelingGame == null) channelingGame = FindFirstObjectByType<ChannelingGameCoordinator>();

        if (targetMapPlayer == null) targetMapPlayer = FindFirstObjectByType<TargetMapPlayer>();

        if (pointTarget == null) pointTarget = FindFirstObjectByType<PointTargetScript>();

        if (spellSelectUI == null) spellSelectUI = FindFirstObjectByType<SpellSelectUI>();

        if (castingUI != null) castingUI.SetActive(false);
        else Debug.LogWarning("SpellcastingCoordinator: castingUI GameObject is not assigned.");

        if (spellList != null) UpdateSpellList(); 
        else Debug.LogWarning("SpellcastingCoordinator: SpellList GameObject is not assigned.");

        if (percentageCounter == null) percentageCounter = FindFirstObjectByType<ChannelingPercentageCounterScript>();

        if (percentageResult == null) percentageResult = FindFirstObjectByType<PercentageResultUI>();
    }

    void OnEnable()
    {
        // If the game recompiles during channeling, resets the player
        if (player != null && (player.CastingState == CastState.Channeling || player.CastingState == CastState.Casting || player.CastingState == CastState.Aiming))
        {
            Debug.LogWarning("SpellcastingCoordinator: Recompilação detectada durante conjuração. Resetando estado para Idle.");
            CancelCast();
        }
    }

    void Update()
    {
        HandleSpellSelection();
        HandleCastInput();
        UpdateSpellCooldowns();
    }

    void HandleSpellSelection()
    { 
        if (player.CastingState != CastState.Idle || player.MenuManager.IsPaused
        || player.MenuManager.CurrentMenu != null) return;

        // Select spells with the mouse wheel.
        float scrollValue = -Mouse.current.scroll.ReadValue().y;
        
        if (scrollValue > 0f)
        {
            spellIndex = (spellIndex + 1) % spells.Length;
            Debug.Log($"Selected spell: {CurrentSpell?.SpellName}");
        }
        else if (scrollValue < 0f)
        {
            spellIndex = (spellIndex - 1 + spells.Length) % spells.Length;
            Debug.Log($"Selected spell: {CurrentSpell?.SpellName}");
        }

        currentSpellType = CurrentSpell.Type;
    }

    void HandleCastInput()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame && player.MenuManager.IsPaused == false
        && player.MenuManager.CurrentMenu == null)
        {
            if (player.CastingState == CastState.Idle)
            {
                if (spellCooldowns[spellIndex] <= 0)
                {
                        if (CurrentSpell != null)
                            currentSpellType = CurrentSpell.Type;

                    BeginChannel();
                }            
                else
                {
                    Debug.Log("Spell currently in cooldown. Time left: " + spellCooldowns[spellIndex]);
                }
            }
            else if (player.CastingState == CastState.Channeling
                   || player.CastingState == CastState.Aiming) CancelCast();
        }

        if (player.CastingState == CastState.Aiming 
        && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CastSpell();
        }
    }

    void BeginChannel()
    {
        if (player == null || player.CameraCollision == null)
        {
            Debug.LogError("SpellcastingCoordinator: PlayerController or CameraCollision is not assigned.", this);
            return;
        }

        if (channelingGame == null)
        {
            Debug.LogError("SpellcastingCoordinator: ChannelingGameCoordinator is not assigned.", this);
            return;
        }

        Vector3 channelingPosition = channelingGame.transform.localPosition;
        channelingPosition.x = player.CameraCollision.TargetSideOffset > 0f ? 500f : -500f;
        channelingGame.transform.localPosition = channelingPosition;
        SpellBase spell = CurrentSpell;
        if (spell == null)
        {
            Debug.LogWarning("SpellcastingCoordinator: no spell selected.");
            return;
        }
        
        if (spell.SpellMap == null)
        {
            Debug.Log("Spell has no map. Delegating flow entirely to the spell.");
            
            spell.OnSpellResolved += HandleSpellResolved;
            
            player.SetCastingState(CastState.Casting);

            spell.OnCastStart();
            return; 
        }
        
        player.PlayerHUD.SetActive(false);

        if (castingUI != null)
        {
            foreach (Transform child in castingUI.transform)
            {
                if (child.CompareTag("Target"))
                    Destroy(child.gameObject);
            }
            castingUI.SetActive(true);
        }

        targetMapPlayer.LoadMap(spell.SpellMap);
        channelingGame.ResetCast();
        channelingGame.OnChannelingResolved += HandleChannelResolved;
        percentageCounter?.SetValues(
            spell.perfectWeight, spell.goodWeight, spell.missWeight,
            1f, 1f, 1f,
            0f, 0f, 0f,
            1f);

        player.SetCastingState(CastState.Channeling);

        spell.OnCastStart();
    }

    void CastSpell()
    {
        pointTarget?.ConsumeCurrentMousePress();

        if (CurrentSpell.Type != SpellBase.SpellType.PointTargeted)
            pointTarget?.ClearPoint();

        player.SetCastingState(CastState.Casting);
        CurrentSpell.OnSpellResolved += HandleSpellResolved;
        CurrentSpell.OnSpellCast();
    }

    void HandleChannelResolved(ChannelingGameCoordinator.ChannelingResult result)
    {
        channelingGame.OnChannelingResolved -=
            HandleChannelResolved;

        // Ignore delayed events from a previous cast.
        if (player.CastingState !=
            CastState.Channeling)
            return;

        CurrentSpell?.OnChannelComplete(result);

        // The spell may cancel the cast while handling this result.
        if (player.CastingState != CastState.Channeling)
            return;

        if (currentSpellType ==
                SpellBase.SpellType.Projectile ||
            currentSpellType ==
                SpellBase.SpellType.Targeted)
            player.SetCastingState(CastState.Aiming);
        else
        {
            player.SetCastingState(CastState.Casting);

            player.PlayerHUD.SetActive(true);
        }

        percentageCounter?.RecalculatePercentage(result);

        percentageResult?.gameObject.SetActive(true);

        if (percentageCounter != null)
        {
            percentageResult?.ShowResult(
                percentageCounter.Percentage,
                percentageCounter.PercentageText.color
            );
        }

        channelingGame.ResetCast();

        targetMapPlayer.ResetMap();

        castingUI?.SetActive(false);
    }

    void HandleSpellResolved()
    {
        CurrentSpell.OnSpellResolved -= HandleSpellResolved;
        spellCooldowns[spellIndex] = CurrentSpell.CooldownTime;
        player.SetCastingState(CastState.Idle);
        if (castingUI != null) castingUI.SetActive(false);
        if (player.PlayerHUD != null)
            player.PlayerHUD.SetActive(true);
    }

    public void CancelCast()
    {
        channelingGame.OnChannelingResolved -= HandleChannelResolved;
        CurrentSpell?.Cancel();
        spellCooldowns[spellIndex] = CurrentSpell.CooldownTime;
        if (castingUI != null) castingUI.SetActive(false);
        player.SetCastingState(CastState.Idle);
        player.PlayerHUD.SetActive(true);
        Debug.Log("Cast cancelled.");
    }

    public void SetSpellCooldowns(List<float> cooldowns)
    {
        for (int i = 0; i < spellCooldowns.Count; i++)
            spellCooldowns[i] = (cooldowns != null && i < cooldowns.Count) ? cooldowns[i] : 0f;
    }

    public void SetSpellCooldown(int spell, float cooldown)
    {
        if (spell >= 0 && spell < spellCooldowns.Count)
            spellCooldowns[spell] = cooldown;
        else
            Debug.LogError("spell index out of range");
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
        if (spellList != null)
        {
            if (spellSelectUI != null)
            {
                spellSelectUI.Setup(this);
                spellSelectUI.UpdateSpellList();
            }
        }
        else
        {
            Debug.LogWarning("SpellcastingCoordinator: SpellList GameObject is not assigned.");
        }
    }
}