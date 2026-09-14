using System;
using System.Collections.Generic;
using UnityEngine;

public class RunicStorageManager : MonoBehaviour
{
    public static RunicStorageManager Instance { get; private set; }
    public static event Action OnStorageChanged;

    [Header("Runtime Box")]
    public List<RunicSaveData> boxStorage = new List<RunicSaveData>();

    [Header("Party")]
    public List<string> partyIds = new List<string>();
    public Runic activeSummonedRunic;

    [Header("References")]
    public PlayerController player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void NotifyStorageChanged()
    {
        OnStorageChanged?.Invoke();
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        EnsurePartySize();
    }

    private int MaxPartySize => player != null ? player.maxPartySize : 3;

    // Garante que partyIds sempre tenha exatamente maxPartySize slots
    private void EnsurePartySize()
    {
        while (partyIds.Count < MaxPartySize) partyIds.Add(null);
        while (partyIds.Count > MaxPartySize) partyIds.RemoveAt(partyIds.Count - 1);
    }

    // ============================================================
    // GETTERS
    // ============================================================

    public RunicSaveData GetBoxRunicBySlot(int boxIndex)
    {
        if (boxIndex < 0 || boxIndex >= boxStorage.Count) return null;
        return boxStorage[boxIndex];
    }

    public bool IsRunicInParty(string runicId)
    {
        return partyIds.Contains(runicId);
    }

    public int GetPartySlotIndex(string runicId)
    {
        return partyIds.IndexOf(runicId);
    }

    public int GetBoxSlotIndex(string runicId)
    {
        return boxStorage.FindIndex(data => data != null && data.runicInstanceId == runicId);
    }

    public RunicSaveData GetRunicById(string runicId)
    {
        if (string.IsNullOrEmpty(runicId))
            return null;

        return boxStorage.Find(data =>
            data != null && data.runicInstanceId == runicId);
    }

    public int GetBoxCount()
    {
        return boxStorage.Count;
    }

    public int GetPartyCount()
    {
        return partyIds.Count;
    }

    public int GetNextAvailablePartySlot()
    {
        return partyIds.FindIndex(id => string.IsNullOrEmpty(id));
    }

    public RunicSaveData GetPartyRunicBySlot(int partyIndex)
    {
        if (partyIndex < 0 || partyIndex >= partyIds.Count)
            return null;

        string runicId = partyIds[partyIndex];
        if (string.IsNullOrEmpty(runicId))
            return null;

        return boxStorage.Find(data =>
                        data != null && data.runicInstanceId == runicId);
    }

    public RunicSaveData GetPartyRunicBySequence(int sequentialPosition)
    {
        if (sequentialPosition <= 0)
            return null;

        int currentPosition = 1;

        for (int i = 0; i < partyIds.Count; i++)
        {
            string runicId = partyIds[i];
            if (!string.IsNullOrEmpty(runicId))
            {
                if (currentPosition == sequentialPosition)
                {
                    return boxStorage.Find(data =>
                        data != null && data.runicInstanceId == runicId);
                }

                currentPosition++;
            }
        }

        return null;
    }

    // ============================================================
    // SAVE / LOAD
    // ============================================================

    public void LoadPartyData(SaveDataContainer data)
    {
        boxStorage = new List<RunicSaveData>(data.boxStorage);
        partyIds = new List<string>(data.partyIds);
        EnsurePartySize();
        NotifyStorageChanged();
    }

    public void PopulateSaveData(SaveDataContainer data)
    {
        data.boxStorage = new List<RunicSaveData>(boxStorage);
        data.partyIds = new List<string>(partyIds);
    }

    // ============================================================
    // CAPTURA
    // ============================================================

    public void AddCapturedRunic(RunicSaveData capturedData)
    {
        boxStorage.Add(capturedData);

        int freeSlot = partyIds.FindIndex(id => string.IsNullOrEmpty(id));
        if (freeSlot != -1) partyIds[freeSlot] = capturedData.runicInstanceId;

        NotifyStorageChanged();
    }

    // ============================================================
    // REMOÇÃO
    // ============================================================

    // Desmarca um slot da party (o rúnico continua na box)
    public void RemoveFromPartyBySlot(int partySlot)
    {
        if (partySlot < 0 || partySlot >= partyIds.Count) return;
        partyIds[partySlot] = null;
        NotifyStorageChanged();
    }

    // Remove o rúnico da box inteiramente (e de qualquer slot de party que o referencie)
    public void RemoveFromBoxByID(string runicId)
    {
        var runicData = boxStorage.Find(data => data.runicInstanceId == runicId);
        if (runicData == null) return;

        boxStorage.Remove(runicData);

        for (int i = 0; i < partyIds.Count; i++)
        {
            if (partyIds[i] == runicId) 
            {
                partyIds[i] = null;
                NotifyStorageChanged();
            }
        }
    }

    public void RemoveFromBoxBySlot(int boxIndex)
    {
        if (boxIndex < 0 || boxIndex >= boxStorage.Count) return;
        RemoveFromBoxByID(boxStorage[boxIndex].runicInstanceId);
        NotifyStorageChanged();
    }

    // ============================================================
    // MOVIMENTAÇÃO
    // ============================================================

    // Seleciona o rúnico do boxIndex para ocupar o slot partyIndex.
    // O rúnico continua fisicamente na box — isto é apenas seleção.
    public void MoveToPartyBySlot(int boxIndex, int partyIndex)
    {
        if (boxIndex < 0 || boxIndex >= boxStorage.Count) return;
        if (partyIndex < 0 || partyIndex >= partyIds.Count) return;

        partyIds[partyIndex] = boxStorage[boxIndex].runicInstanceId;
        NotifyStorageChanged();
    }

    // Reordena a posição visual dentro da box.
    // Não precisa corrigir a party: ela referencia por ID, não por índice.
    public void MovePositionInBoxBySlot(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= boxStorage.Count) return;

        // Garante que a lista tenha tamanho suficiente para alcançar o newIndex
        while (boxStorage.Count <= newIndex)
        {
            boxStorage.Add(null);
        }

        // Troca os itens de lugar com segurança
        RunicSaveData temp = boxStorage[oldIndex];
        boxStorage[oldIndex] = boxStorage[newIndex];
        boxStorage[newIndex] = temp;

        // Limpa espaços nulos excedentes na cauda da lista
        CleanupBoxStorageTail();

        NotifyStorageChanged();
    }

    private void CleanupBoxStorageTail()
    {
        for (int i = boxStorage.Count - 1; i >= 0; i--)
        {
            if (boxStorage[i] == null)
                boxStorage.RemoveAt(i);
            else
                break;
        }
    }

    // Troca/reordena posições dentro da party (apenas os IDs, slots fixos)
    public void MovePositionInParty(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= partyIds.Count) return;
        if (newIndex < 0 || newIndex >= partyIds.Count) return;
        if (oldIndex == newIndex) return;

        (partyIds[newIndex], partyIds[oldIndex]) = (partyIds[oldIndex], partyIds[newIndex]);
        NotifyStorageChanged();
    }

    // ============================================================
    // LIMPEZA
    // ============================================================

    public void ClearAllRunics()
    {
        boxStorage.Clear();
        for (int i = 0; i < partyIds.Count; i++) partyIds[i] = null;
        NotifyStorageChanged();
    }

    public void ClearParty()
    {
        for (int i = 0; i < partyIds.Count; i++) partyIds[i] = null;
        NotifyStorageChanged();
    }

    // ============================================================
    // DEBUG / INJEÇÃO
    // ============================================================

    public RunicSaveData CreateRunicSaveData(RunicSpecies species, int level = 1, int experience = 0, string nickname = null)
    {
        RunicSaveData newRunicData = new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = species.speciesId,
            level = level,
            experience = experience,
            nickname = string.IsNullOrEmpty(nickname) ? species.speciesName : nickname,
            runicModel = species.speciesModels.Count > 0 ? species.speciesModels[0] : null
        };

        return newRunicData;
    }

    public void DebugInjectRunic(RunicSpecies species, int level = 1)
    {
        if (species == null) return;
        AddCapturedRunic(CreateRunicSaveData(species, level));
    }

    // ============================================================
    // 
    // ============================================================

    public void CreateRunicFromSaveData(RunicSaveData saveData)
    {
        
    }
}