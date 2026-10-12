using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor;

public class SaveLoadMenu : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private MenuManager menuManager; 
    [SerializeField] private SaveManager saveManager; 
    [SerializeField] private RunicStorageManager runicStorageManager;
    [SerializeField] private SettingsManager settingsManager;
    [SerializeField] private GameObject saveButtonPrefab;
    [SerializeField] private GameObject saveFilePrefab;
    [SerializeField] private Transform savesList;

    void Start()
    {
        if (menuManager == null) menuManager = FindFirstObjectByType<MenuManager>();
        
        if (saveManager == null) saveManager = FindFirstObjectByType<SaveManager>();
        
        if (runicStorageManager == null) runicStorageManager = FindFirstObjectByType<RunicStorageManager>();

        if (settingsManager == null) settingsManager = FindFirstObjectByType<SettingsManager>();

        ViewAllSaves();
    }

    public void ViewAllSaves()
    {
        // Clear the current list.
        foreach (Transform child in savesList)
        {
            Destroy(child.gameObject);
        }

        // 1. Create the new-save button.
        CreateNewSaveButton("New Save");

        // 2. Create a slot for each existing save.
        foreach (string saveFileName in saveManager.GetSaveFiles())
        {
            CreateSaveFileSlot(saveFileName);
        }
    }

    private void CreateNewSaveButton(string label)
    {
        GameObject newButton = Instantiate(saveButtonPrefab, savesList);
        TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = label;

        Button btn = newButton.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(NewSave);
    }

    private void CreateSaveFileSlot(string fileName)
    {
        // Instantiate the prefab.
        GameObject slotObj = Instantiate(saveFilePrefab, savesList);
        
        // Get the component from the new instance.
        SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();

        if (slotUI != null)
        {
            // Pass data and callbacks so the slot can configure its buttons.
            slotUI.Setup(
                fileName: fileName,
                loadAction: (name) => LoadSave(name),
                overwriteAction: (name) => OverwriteSave(name),
                deleteAction: (name) => DeleteSave(name),
                renameAction: (name) => RenameSave(fileName, name)
            );
        }
    }

    // --- Actions ---

    public void NewSave()
    {
        SaveDataContainer newGameData = new SaveDataContainer();
        string newSaveFileName = saveManager.CreateNextSaveFileName(); 
        saveManager.SaveGame(newSaveFileName, newGameData); 
        settingsManager.SaveActiveSaveFile(newSaveFileName);
        ViewAllSaves(); 
    }

    public void LoadSave(string saveFileName)
    {
        SaveDataContainer loadedData = saveManager.LoadGame(saveFileName); 
        settingsManager.SaveActiveSaveFile(saveFileName);
        menuManager.CloseAllMenus(); 
        menuManager.Player.LoadPlayerData(loadedData);
        runicStorageManager.LoadPartyData(loadedData);
    }

    public void OverwriteSave(string saveFileName)
    {
        SaveDataContainer newGameData = menuManager.GetCurrentGameData();
        saveManager.SaveGame(saveFileName, newGameData);
        settingsManager.SaveActiveSaveFile(saveFileName);
        ViewAllSaves();
    }

    public void DeleteSave(string saveFileName)
    {
        saveManager.DeleteSaveFile(saveFileName);
        ViewAllSaves();
    }

    public void RenameSave(string oldFileName, string newFileName)
    {
        saveManager.RenameSaveFile(oldFileName, newFileName);
        ViewAllSaves();
    }
}