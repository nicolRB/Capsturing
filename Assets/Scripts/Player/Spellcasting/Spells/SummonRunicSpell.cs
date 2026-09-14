using UnityEngine;

public class SummonRunicSpell : SpellBase
{
    [Header("Summon Settings")]
    public float cooldownTimeValue = 1.5f;
    public float summonHeight = 1f;
    public RunicStorageManager runicStorageManager;
    public RunicDatabase runicDatabase;
    public GameObject runicWheel;

    [Header("State Tracking")]
    private RunicSaveData selectedRunicFromWheel;
    
    [HideInInspector]
    public Runic activeSummonedRunic = null;
    
    // Flag para saber se a roda de escolha está aberta no momento
    private bool isWheelOpen = false;

    public override void Start()
    {
        base.Start();
        cooldownTime = cooldownTimeValue;
        spellType = SpellType.Targeted;

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
            spellcastingScript.CancelCast();
            return;
        }

        // 1. CHECAGEM: Verifica se existe pelo menos um rúnico válido na party inteira
        bool hasAnyRunicInParty = false;
        foreach (string id in runicStorageManager.partyIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                hasAnyRunicInParty = true;
                break;
            }
        }

        // Se a party estiver totalmente vazia, cancela a magia imediatamente antes de abrir qualquer UI
        if (!hasAnyRunicInParty)
        {
            Debug.Log("Summon spell cancelled: No runics in party.");
            spellcastingScript.CancelCast();
            return;
        }

        Debug.Log("Summon spell: Opening Party Wheel.");
        isWheelOpen = true;
        player.castState = PlayerController.CastState.Channeling;
        runicWheel.SetActive(true);
    }

    // Chamado quando o jogador aperta 'E' novamente com a roda aberta para cancelar
    public override void Cancel()
    {
        Debug.Log("Summon spell cancelled by player.");
        isWheelOpen = false;
        if (runicWheel != null)
            runicWheel.SetActive(false);
        pointTarget?.ClearPoint();
        selectedRunicFromWheel = null;
        player.useAimIndicator = true;
        player.castState = PlayerController.CastState.Idle;
        
        // FECHAR A RODA DE ESCOLHA AQUI SE ELA ESTIVER ABERTA
        // Exemplo: wheelUI.ClosePartyWheel();

        RaiseSpellResolved();
    }

    // Este método seria chamado pela sua UI da roda ao escolher um rúnico
    public void OnRunicSelectedFromWheel(RunicSaveData chosenRunic)
    {
        if (!isWheelOpen) return;

        isWheelOpen = false;
        runicWheel.SetActive(false);

        if (chosenRunic == null)
        {
            spellcastingScript.CancelCast();
            return;
        }

        if (IsRunicSummoned(chosenRunic.runicInstanceId))
        {
            Debug.Log($"Recalling runic {chosenRunic.runicInstanceId}.");
            UnsummonRunic();
            pointTarget?.ClearPoint();
            player.useAimIndicator = true;
            RaiseSpellResolved();
            return;
        }

        // Se escolher um rúnico NÃO INVOCADO -> Vai para o estado de Aiming para posicionar
        selectedRunicFromWheel = chosenRunic;
        player.useAimIndicator = false;
        player.castState = PlayerController.CastState.Aiming;

        RunicSpecies species = runicDatabase != null
            ? runicDatabase.GetSpeciesById(chosenRunic.speciesId)
            : null;
        string runicName = !string.IsNullOrEmpty(chosenRunic.nickname)
            ? chosenRunic.nickname
            : species != null ? species.speciesName : chosenRunic.speciesId;
        Debug.Log($"Runic {runicName} selected. Enter aiming state for placement.");
    }

    public bool IsRunicSummoned(string runicId)
    {
        return activeSummonedRunic != null &&
            !string.IsNullOrEmpty(runicId) &&
            activeSummonedRunic.runicInstanceId == runicId;
    }

    public override void OnChannelComplete(ChannelingGameScript.ChannelingResult result)
    {
        // Não utilizado
    }

    public override void OnSpellCast()
    {
        if (selectedRunicFromWheel == null)
        {
            Debug.LogWarning("No runic selected from wheel to summon.");
            RaiseSpellResolved();
            return;
        }

        if (pointTarget == null || !pointTarget.groundIndicator.activeSelf)
        {
            Debug.LogWarning("SummonRunicSpell: no valid PointTarget position.");
            player.useAimIndicator = true;
            selectedRunicFromWheel = null;
            RaiseSpellResolved();
            return;
        }

        Debug.Log("Summon spell: Executing placement and spawn.");

        // Se houver outro rúnico invocado em campo, limpa ele
        if (activeSummonedRunic != null)
        {
            UnsummonRunic();
        }

        Vector3 spawnPosition = pointTarget.indicatedPosition + Vector3.up * summonHeight;

        if (selectedRunicFromWheel.runicModel == null)
        {
            Debug.LogError($"Runic '{selectedRunicFromWheel.runicInstanceId}' has no model assigned.");
            pointTarget.ClearPoint();
            selectedRunicFromWheel = null;
            RaiseSpellResolved();
            return;
        }

        GameObject spawnedObj = Instantiate(selectedRunicFromWheel.runicModel, spawnPosition, player.transform.rotation);
        pointTarget.ClearPoint();
        activeSummonedRunic = spawnedObj.GetComponent<Runic>();

        if (activeSummonedRunic != null)
        {
            activeSummonedRunic.runicDatabase = runicDatabase;
            activeSummonedRunic.InitializeFromData(selectedRunicFromWheel);
        }

        if (activeSummonedRunic != null) 
            Debug.Log($"Successfully summoned {activeSummonedRunic.name}");

        selectedRunicFromWheel = null;
        player.useAimIndicator = true;
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