using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string saveFilePath;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Persistent data path initialization
        saveFilePath = Path.Combine(Application.persistentDataPath, "creature_save.json");
    }

    public void SaveGame(SaveDataContainer dataToSave)
    {
        try
        {
            string json = JsonUtility.ToJson(dataToSave, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"Game Saved to: {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e.Message}");
        }
    }

    public SaveDataContainer LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found. Creating fresh save data.");
            return new SaveDataContainer();
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            SaveDataContainer loadedData = JsonUtility.FromJson<SaveDataContainer>(json);
            Debug.Log("Game Loaded Successfully!");
            return loadedData ?? new SaveDataContainer();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load save file. Returning new save data: {e.Message}");
            return new SaveDataContainer();
        }
    }

    public bool SaveFileExists()
    {
        return File.Exists(saveFilePath);
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file deleted.");
        }
    }
}