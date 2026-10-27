using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private int firstGameplaySceneIndex = 2;
    private SaveDataContainer pendingLoad;

    private const string ActiveSaveKey = "ActiveSaveFile";
    private string activeSaveFileName = "";   // vazio = nenhum
    public string ActiveSaveFileName => activeSaveFileName;

    private const string LegacySaveFileName = "creature_save.json";
    private const string SaveFilePrefix = "save_";
    private string saveDirectory;

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
        saveDirectory = Application.persistentDataPath;
        Directory.CreateDirectory(saveDirectory);
        activeSaveFileName = PlayerPrefs.GetString(ActiveSaveKey, "");
    }

    public void SaveGame(string fileName, SaveDataContainer dataToSave)
    {
        if (dataToSave == null)
        {
            Debug.LogError("SaveManager: cannot save null data.");
            return;
        }

        try
        {
            fileName = NormalizeFileName(fileName);
            string json = JsonUtility.ToJson(dataToSave, true);
            File.WriteAllText(GetSaveFilePath(fileName), json);
            SetActiveSave(fileName);
            Debug.Log($"Game Saved to: {GetSaveFilePath(fileName)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e.Message}");
        }
    }

    public SaveDataContainer LoadGame(string fileName)
    {
        fileName = NormalizeFileName(fileName);
        string saveFilePath = GetSaveFilePath(fileName);
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found. Creating fresh save data.");
            return new SaveDataContainer();
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            SaveDataContainer loadedData = JsonUtility.FromJson<SaveDataContainer>(json);
            SetActiveSave(fileName);
            Debug.Log("Game Loaded Successfully!");
            return loadedData ?? new SaveDataContainer();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load save file. Returning new save data: {e.Message}");
            return new SaveDataContainer();
        }
    }

    public void LoadGameAndChangeScene(string fileName)
    {
        if (!SaveFileExists(fileName))
        {
            Debug.LogWarning($"Save '{fileName}' não encontrado.");
            return;
        }

        SaveDataContainer data = LoadGame(fileName); // também define o save ativo

        // Saves novos ou antigos têm sceneIndex 0 (menu): caem na primeira cena de jogo.
        int target = data.sceneIndex;
        if (target < firstGameplaySceneIndex || target >= SceneManager.sceneCountInBuildSettings)
            target = firstGameplaySceneIndex;

        pendingLoad = data;

        if (SceneChanger.Instance != null)
            SceneChanger.Instance.LoadSceneByIndex(target);
        else
            SceneManager.LoadScene(target);
    }

    // Chamado pela cena de jogo depois que ela carrega.
    public bool TryConsumePendingLoad(out SaveDataContainer data)
    {
        data = pendingLoad;
        pendingLoad = null;
        return data != null;
    }

    public void SaveGame(SaveDataContainer dataToSave)
    {
        string target = string.IsNullOrEmpty(activeSaveFileName)
            ? CreateNextSaveFileName()
            : activeSaveFileName;
        SaveGame(target, dataToSave);
    }

    public SaveDataContainer LoadGame()
    {
        return LoadGame(activeSaveFileName);
    }

    public bool SaveFileExists(string fileName)
    {
        return File.Exists(GetSaveFilePath(NormalizeFileName(fileName)));
    }

    public bool SaveFileExists()
    {
        return SaveFileExists(activeSaveFileName);
    }

    public string[] GetSaveFiles()
    {
        try
        {
            List<string> saveFiles = new List<string>();
            string[] paths = Directory.GetFiles(saveDirectory, "*.json");

            foreach (string path in paths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName == LegacySaveFileName.Replace(".json", "") || fileName.StartsWith(SaveFilePrefix))
                {
                    saveFiles.Add(fileName);
                }
            }

            saveFiles.Sort();
            return saveFiles.ToArray();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list save files: {e.Message}");
            return Array.Empty<string>();
        }
    }

    public string CreateNextSaveFileName()
    {
        int slotNumber = 1;
        while (SaveFileExists($"{SaveFilePrefix}{slotNumber}")) slotNumber++;
        return $"{SaveFilePrefix}{slotNumber}";
    }

    public void DeleteSaveFile(string fileName)
    {
        try
        {
            string normalizedFileName = NormalizeFileName(fileName);
            string saveFilePath = GetSaveFilePath(normalizedFileName);
            if (!File.Exists(saveFilePath))
                return;

            File.Delete(saveFilePath);
            if (activeSaveFileName == normalizedFileName) SetActiveSave("");
            Debug.Log("Save file deleted.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }

    public void DeleteSaveFile()
    {
        DeleteSaveFile(activeSaveFileName);
    }

    private string GetSaveFilePath(string fileName)
    {
        return Path.Combine(saveDirectory, $"{fileName}.json");
    }

    private string NormalizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return activeSaveFileName;

        string normalizedName = Path.GetFileNameWithoutExtension(fileName);
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            normalizedName = normalizedName.Replace(invalidCharacter.ToString(), string.Empty);
        }

        return string.IsNullOrWhiteSpace(normalizedName) ? activeSaveFileName : normalizedName;
    }

    public void RenameSaveFile(string oldFileName, string newFileName)
    {
        try
        {
            oldFileName = NormalizeFileName(oldFileName);
            newFileName = NormalizeFileName(newFileName);

        if (oldFileName == newFileName)
            return;

        if (!newFileName.StartsWith(SaveFilePrefix) && oldFileName.StartsWith(SaveFilePrefix))
        {
            // If the original had the prefix, guarantees the new one has it to keep the pattern
            newFileName = SaveFilePrefix + newFileName;
        }

        string oldPath = GetSaveFilePath(oldFileName);
        
        // Avoids overwrute: if the file with the new name already exists, adds a numbered suffix
        if (File.Exists(GetSaveFilePath(newFileName)))
        {
            string baseName = newFileName;
            int suffix = 1;
            while (File.Exists(GetSaveFilePath($"{baseName}_{suffix}")))
            {
                suffix++;
            }
            newFileName = $"{baseName}_{suffix}";
        }

        string newPath = GetSaveFilePath(newFileName);

            if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);

                if (activeSaveFileName == oldFileName)
                {
                    SetActiveSave(newFileName);
                }

                Debug.Log($"Save file renamed from {oldFileName} to {newFileName}");
            }
            else
            {
                Debug.LogWarning($"Could not rename. Old save file not found: {oldPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to rename save file: {e.Message}");
        }
    }

    private void SetActiveSave(string fileName)
    {
        activeSaveFileName = fileName;
        PlayerPrefs.SetString(ActiveSaveKey, fileName);
        PlayerPrefs.Save();
    }
}