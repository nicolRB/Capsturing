using UnityEngine;

public class SummonRunicSpell : SpellBase
{
    [Header("Summon Settings")]
    [SerializeField] private float cooldownTimeValue = 1.5f;
    [SerializeField] private float summonHeight = 1f;
    [SerializeField] private RunicStorageManager runicStorageManager;
    [SerializeField] private RunicDatabase runicDatabase;
    [SerializeField] private GameObject runicWheel;

    [Header("State Tracking")]
    private RunicSaveData selectedRunicFromWheel;
    private Runic activeSummonedRunic = null;
    
    // Tracks whether the selection wheel is currently open.
    private bool isWheelOpen = false;

    public override void Start()
    {
        base.Start();

        if (runicStorageManager == null)
            runicStorageManager = FindFirstObjectByType<RunicStorageManager>();

        if (runicDatabase == null)
        {
            RunicDatabase[] databases = Resources.FindObjectsOfTypeAll<RunicDatabase>();
            if (databases.Length > 0)
                runicDatabase = databases[0];
        }

        if (runicWheel == null)
            runicWheel = GameObject.Find("RunicWheel");

        if (runicWheel != null)
            runicWheel.SetActive(false);
    }

    public override void OnCastStart()
    {
        if (runicStorageManager == null)
        {
            Debug.LogWarning("SummonRunicSpell: RunicStorageManager not found!");
            spellcastingCoordinator.CancelCast();
            return;
        }

        // Check whether at least one valid runic is in the party.
        bool hasAnyRunicInParty = false;
        foreach (string id in runicStorageManager.PartyIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                hasAnyRunicInParty = true;
                break;
            }
        }

        // Cancel before opening the UI when the party is empty.
        if (!hasAnyRunicInParty)
        {
            Debug.Log("Summon spell cancelled: No runics in party.");
            spellcastingCoordinator.CancelCast();
            return;
        }

        Debug.Log("Summon spell: Opening Party Wheel.");
        isWheelOpen = true;
        player.SetCastingState(PlayerController.CastState.Channeling);
        runicWheel.SetActive(true);
    }

    // Called when the player presses E again while the wheel is open.
    public override void Cancel()
    {
        Debug.Log("Summon spell cancelled by player.");
        isWheelOpen = false;
        if (runicWheel != null)
            runicWheel.SetActive(false);
        pointTarget?.ClearPoint();
        selectedRunicFromWheel = null;
        player.ToggleAimIndicator(true);
        player.SetCastingState(PlayerController.CastState.Idle);
        
        RaiseSpellResolved();
    }

    // Called by the wheel UI when the player chooses a runic.
    public void OnRunicSelectedFromWheel(RunicSaveData chosenRunic)
    {
        if (!isWheelOpen) return;

        isWheelOpen = false;
        runicWheel.SetActive(false);

        if (chosenRunic == null)
        {
            spellcastingCoordinator.CancelCast();
            return;
        }

        if (IsRunicSummoned(chosenRunic.runicInstanceId))
        {
            Debug.Log($"Recalling runic {chosenRunic.runicInstanceId}.");
            UnsummonRunic();
            pointTarget?.ClearPoint();
            player.ToggleAimIndicator(true);
            RaiseSpellResolved();
            return;
        }

        // An unsummoned runic enters the aiming state for placement.
        selectedRunicFromWheel = chosenRunic;
        player.ToggleAimIndicator(false);
        player.SetCastingState(PlayerController.CastState.Aiming);

        RunicSpecies species = runicDatabase != null
            ? runicDatabase.GetSpeciesById(chosenRunic.speciesId)
            : null;
        string runicName = !string.IsNullOrEmpty(chosenRunic.nickname)
            ? chosenRunic.nickname
            : species != null ? species.SpeciesName : chosenRunic.speciesId;
        Debug.Log($"Runic {runicName} selected. Enter aiming state for placement.");
    }

    public bool IsRunicSummoned(string runicId)
    {
        return activeSummonedRunic != null &&
            !string.IsNullOrEmpty(runicId) &&
            activeSummonedRunic.RunicInstanceId == runicId;
    }

    public override void OnChannelComplete(ChannelingGameScript.ChannelingResult result)
    {
        // This spell does not use a channeling result.
    }

    public override void OnSpellCast()
    {
        if (selectedRunicFromWheel == null)
        {
            Debug.LogWarning("No runic selected from wheel to summon.");
            RaiseSpellResolved();
            return;
        }

        if (pointTarget == null || !pointTarget.GroundIndicator.activeSelf)
        {
            Debug.LogWarning("SummonRunicSpell: no valid PointTarget position.");
            player.ToggleAimIndicator(true);
            selectedRunicFromWheel = null;
            RaiseSpellResolved();
            return;
        }

        Debug.Log("Summon spell: Executing placement and spawn.");

        // Remove any other runic currently summoned in the scene.
        if (activeSummonedRunic != null)
        {
            UnsummonRunic();
        }

        Vector3 spawnPosition = pointTarget.IndicatedPosition + Vector3.up * summonHeight;

        GameObject runicModel = runicStorageManager.GetRunicModel(selectedRunicFromWheel);
        if (runicModel == null)
        {
            Debug.LogError($"Runic '{selectedRunicFromWheel.runicInstanceId}' has no model assigned.");
            pointTarget.ClearPoint();
            selectedRunicFromWheel = null;
            RaiseSpellResolved();
            return;
        }

        GameObject spawnedObj = Instantiate(runicModel, spawnPosition, player.transform.rotation);
        pointTarget.ClearPoint();
        activeSummonedRunic = spawnedObj.GetComponent<Runic>();

        if (activeSummonedRunic != null)
        {
            activeSummonedRunic.SetRunicDatabase(runicDatabase);
            activeSummonedRunic.InitializeFromData(selectedRunicFromWheel);
        }

        if (activeSummonedRunic != null) 
            Debug.Log($"Successfully summoned {activeSummonedRunic.name}");

        selectedRunicFromWheel = null;
        player.ToggleAimIndicator(true);
        RaiseSpellResolved();
    }

    private void UnsummonRunic()
    {
        if (activeSummonedRunic != null)
        {
            Debug.Log($"Unsummoning {activeSummonedRunic.name}");
            Destroy(activeSummonedRunic.gameObject);
            activeSummonedRunic = null;
        }
    }
}