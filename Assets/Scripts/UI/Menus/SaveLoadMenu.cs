using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveLoadMenu : MonoBehaviour 
{
    [Header("References")]
    [SerializeField] private MenuManager menuManager; 
    [SerializeField] private GameObject saveButtonPrefab;
    [SerializeField] private GameObject saveFilePrefab;
    [SerializeField] private Transform savesList;

    void OnEnable()
    {
        if (menuManager == null) menuManager = FindFirstObjectByType<MenuManager>();

        ViewAllSaves();
    }

    public void ViewAllSaves()
    {
        if (SaveManager.Instance == null) return;

        // Clear the current list.
        foreach (Transform child in savesList)
        {
            Destroy(child.gameObject);
        }

        // 1. Create the new-save button.
        CreateNewSaveButton("New Save");

        // 2. Create a slot for each existing save.
        foreach (string saveFileName in SaveManager.Instance.GetSaveFiles())
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
        SaveDataContainer newGameData = menuManager.GetCurrentGameData();
        string newSaveFileName = SaveManager.Instance.CreateNextSaveFileName(); 
        SaveManager.Instance.SaveGame(newSaveFileName, newGameData); 
        ViewAllSaves(); 
    }

    public void LoadSave(string saveFileName)
    {
        SaveManager.Instance.LoadGameAndChangeScene(saveFileName);
    }

    public void OverwriteSave(string saveFileName)
    {
        SaveDataContainer newGameData = menuManager.GetCurrentGameData();
        SaveManager.Instance.SaveGame(saveFileName, newGameData);
        ViewAllSaves();
    }

    public void DeleteSave(string saveFileName)
    {
        SaveManager.Instance.DeleteSaveFile(saveFileName);
        ViewAllSaves();
    }

    public void RenameSave(string oldFileName, string newFileName)
    {
        SaveManager.Instance.RenameSaveFile(oldFileName, newFileName);
        ViewAllSaves();
    }
}