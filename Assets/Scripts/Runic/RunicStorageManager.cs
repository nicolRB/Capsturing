using System;
using System.Collections.Generic;
using UnityEngine;

public class RunicStorageManager : MonoBehaviour
{
    public static RunicStorageManager Instance { get; private set; }
    public static event Action OnStorageChanged;

    [Header("Runtime Box")]
    [SerializeField] private List<RunicSaveData> boxStorage = new List<RunicSaveData>();

    public List<RunicSaveData> BoxStorage => boxStorage;

    [Header("Party")]
    [SerializeField] private List<string> partyIds = new List<string>();

    public List<string> PartyIds => partyIds;

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private RunicDatabase runicDatabase;

    public PlayerController Player => player;

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
        if (runicDatabase == null) runicDatabase = FindFirstObjectByType<RunicDatabase>();
        EnsurePartySize();
    }

    public Sprite GetRunicIcon(RunicSaveData runicData)
    {
        RunicSpecies species = GetSpeciesForSaveData(runicData);
        return species != null ? species.SpeciesIcon : null;
    }

    public GameObject GetRunicModel(RunicSaveData runicData)
    {
        RunicSpecies species = GetSpeciesForSaveData(runicData);
        if (species == null || species.SpeciesModels == null || species.SpeciesModels.Count == 0)
            return null;

        int modelIndex = Mathf.Clamp(runicData.modelIndex, 0, species.SpeciesModels.Count - 1);
        return species.SpeciesModels[modelIndex];
    }

    private RunicSpecies GetSpeciesForSaveData(RunicSaveData runicData)
    {
        if (runicData == null || runicDatabase == null || string.IsNullOrEmpty(runicData.speciesId))
            return null;

        return runicDatabase.GetSpeciesById(runicData.speciesId);
    }

    private int MaxPartySize => player != null ? player.MaxPartySize : 3;

    // Keep the party list aligned with the player's maximum party size.
    private void EnsurePartySize()
    {
        while (partyIds.Count < MaxPartySize) partyIds.Add(null);
        while (partyIds.Count > MaxPartySize) partyIds.RemoveAt(partyIds.Count - 1);
    }

    // ============================================================
    // LOOKUPS
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
        boxStorage = data.boxStorage != null
            ? new List<RunicSaveData>(data.boxStorage)
            : new List<RunicSaveData>();

        partyIds = data.partyIds != null
            ? new List<string>(data.partyIds)
            : new List<string>();
        EnsurePartySize();
        NotifyStorageChanged();
    }

    public void PopulateSaveData(SaveDataContainer data)
    {
        data.boxStorage = new List<RunicSaveData>(boxStorage);
        data.partyIds = new List<string>(partyIds);
    }

    // ============================================================
    // CAPTURE
    // ============================================================

    public void AddCapturedRunic(RunicSaveData capturedData)
    {
        boxStorage.Add(capturedData);

        int freeSlot = partyIds.FindIndex(id => string.IsNullOrEmpty(id));
        if (freeSlot != -1) partyIds[freeSlot] = capturedData.runicInstanceId;

        NotifyStorageChanged();
    }

    // ============================================================
    // REMOVAL
    // ============================================================

    // Clear a party slot without removing the runic from the box.
    public void RemoveFromPartyBySlot(int partySlot)
    {
        if (partySlot < 0 || partySlot >= partyIds.Count) return;
        partyIds[partySlot] = null;
        NotifyStorageChanged();
    }

    // Remove the runic from the box and from every party slot that references it.
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
    // MOVEMENT
    // ============================================================

    // Assign the runic at boxIndex to partyIndex.
    // The runic remains physically stored in the box.
    public void MoveToPartyBySlot(int boxIndex, int partyIndex)
    {
        if (boxIndex < 0 || boxIndex >= boxStorage.Count) return;
        if (partyIndex < 0 || partyIndex >= partyIds.Count) return;

        partyIds[partyIndex] = boxStorage[boxIndex].runicInstanceId;
        NotifyStorageChanged();
    }

    // Reorder the box display without changing party references, which use IDs.
    public void MovePositionInBoxBySlot(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= boxStorage.Count) return;

        // Extend the list when the target index is beyond its current size.
        while (boxStorage.Count <= newIndex)
        {
            boxStorage.Add(null);
        }

        // Swap the entries.
        RunicSaveData temp = boxStorage[oldIndex];
        boxStorage[oldIndex] = boxStorage[newIndex];
        boxStorage[newIndex] = temp;

        // Remove trailing null entries created by the move.
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

    // Swap fixed party slots by exchanging their IDs.
    public void MovePositionInParty(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= partyIds.Count) return;
        if (newIndex < 0 || newIndex >= partyIds.Count) return;
        if (oldIndex == newIndex) return;

        (partyIds[newIndex], partyIds[oldIndex]) = (partyIds[oldIndex], partyIds[newIndex]);
        NotifyStorageChanged();
    }

    // ============================================================
    // CLEARING
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
    // DEBUG / INJECTION
    // ============================================================

    public RunicSaveData CreateRunicSaveData(RunicSpecies species, int level = 1, int experience = 0, string nickname = null)
    {
        RunicSaveData newRunicData = new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = species.SpeciesId,
            level = level,
            experience = experience,
            nickname = string.IsNullOrEmpty(nickname) ? species.SpeciesName : nickname,
            modelIndex = 0
        };

        return newRunicData;
    }

    public void DebugInjectRunic(RunicSpecies species, int level = 1)
    {
        if (species == null) return;
        AddCapturedRunic(CreateRunicSaveData(species, level));
    }

    // ============================================================
    // SAVE DATA CONVERSION
    // ============================================================

    public void CreateRunicFromSaveData(RunicSaveData saveData)
    {
        
    }
}