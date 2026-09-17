using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string LegacySaveFileName = "creature_save.json";
    private const string SaveFilePrefix = "save_";
    private string saveDirectory;
    private string activeSaveFileName = "save_1";

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
            activeSaveFileName = fileName;
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
            activeSaveFileName = fileName;
            Debug.Log("Game Loaded Successfully!");
            return loadedData ?? new SaveDataContainer();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load save file. Returning new save data: {e.Message}");
            return new SaveDataContainer();
        }
    }

    public void SaveGame(SaveDataContainer dataToSave)
    {
        SaveGame(activeSaveFileName, dataToSave);
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
        while (SaveFileExists($"{SaveFilePrefix}SaveFile_{slotNumber}")) slotNumber++;
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
            if (activeSaveFileName == normalizedFileName) activeSaveFileName = "save_1";
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
            // Se o original tinha o prefixo, garante que o novo também tenha para manter o padrão
            // (Remova este bloco se preferir que nomes customizados não precisem começar com save_)
            newFileName = SaveFilePrefix + newFileName;
        }

        string oldPath = GetSaveFilePath(oldFileName);
        
        // Evita sobrescrita: se o arquivo com o novo nome já existir, adiciona um sufixo numérico
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
                    activeSaveFileName = newFileName;
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
}