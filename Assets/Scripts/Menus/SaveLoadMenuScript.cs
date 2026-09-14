using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveLoadMenuScript : MonoBehaviour 
{
    [Header("References")]
    public MenuManager menuManager; 
    public SaveManager saveManager; 
    public GameObject saveButtonPrefab; // O botão simples de "New Save"
    public GameObject saveFilePrefab;   // O prefab de SaveSlotUI
    public Transform savesList;

    void Start()
    {
        ViewAllSaves();
    }

    public void ViewAllSaves()
    {
        // Limpa a lista atual
        foreach (Transform child in savesList)
        {
            Destroy(child.gameObject);
        }

        // 1. Cria o botão de Novo Jogo
        CreateNewSaveButton("New Save");

        // 2. Gera os painéis complexos para cada save existente
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
        // Instancia o prefab
        GameObject slotObj = Instantiate(saveFilePrefab, savesList);
        
        // Pega o script que acabamos de criar
        SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();

        if (slotUI != null)
        {
            // Passa os dados e as funções (Expressões Lambda) para o slot configurar seus botões
            slotUI.Setup(
                fileName: fileName,
                loadAction: (name) => LoadSave(name),
                overwriteAction: (name) => OverwriteSave(name),
                deleteAction: (name) => DeleteSave(name),
                renameAction: (name) => RenameSave(fileName, name) // Passa o nome antigo e o novo
            );
        }
    }

    // --  --

    public SaveDataContainer GetCurrentGameData()
    {
        SaveDataContainer data = new SaveDataContainer();
        menuManager.player.PopulateSaveData(data);
        RunicStorageManager.Instance.PopulateSaveData(data);
        return data;
    }

    // --- Ações ---

    public void NewSave()
    {
        SaveDataContainer newGameData = new SaveDataContainer();
        string newSaveFileName = saveManager.CreateNextSaveFileName(); 
        saveManager.SaveGame(newSaveFileName, newGameData); 
        ViewAllSaves(); 
    }

    public void LoadSave(string saveFileName)
    {
        SaveDataContainer loadedData = saveManager.LoadGame(saveFileName); 
        menuManager.CloseAllMenus(); 
        menuManager.player.LoadPlayerData(loadedData);
    }

    public void OverwriteSave(string saveFileName)
    {
        SaveDataContainer newGameData = GetCurrentGameData();
        saveManager.SaveGame(saveFileName, newGameData);
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
        ViewAllSaves(); // Atualiza a lista visualmente
    }
}