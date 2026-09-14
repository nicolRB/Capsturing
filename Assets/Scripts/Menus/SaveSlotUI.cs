using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text fileNameText;
    public Button loadButton;
    public Button overwriteButton;
    public Button deleteButton;
    public Button renameButton;

    public InputManager inputManager;

    private const string SaveFilePrefix = "save_";

    // Variáveis para guardar as funções que o Menu vai passar
    private string mySaveFileName;
    private System.Action<string> onLoad;
    private System.Action<string> onOverwrite;
    private System.Action<string> onDelete;
    private System.Action<string> onRename;

    // O Menu chama isso logo depois de instanciar o prefab
    public void Setup(string fileName, System.Action<string> loadAction, System.Action<string> overwriteAction, 
    System.Action<string> deleteAction, System.Action<string> renameAction)
    {
        mySaveFileName = fileName;
        string displayName = fileName;
        // remove prefixo "save_" se estiver presente para exibição
        if (displayName.StartsWith("save_"))
        {
            displayName = displayName.Substring(5);
        }
        fileNameText.text = displayName;

        onLoad = loadAction;
        onOverwrite = overwriteAction;
        onDelete = deleteAction;
        onRename = renameAction;
        // Limpa os listeners antigos (boa prática) e adiciona os novos
        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(OnLoadClicked);

        overwriteButton.onClick.RemoveAllListeners();
        overwriteButton.onClick.AddListener(OnOverwriteClicked);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(OnDeleteClicked);

        renameButton.onClick.RemoveAllListeners();
        renameButton.onClick.AddListener(OnRenameClicked);

        inputManager = InputManager.Instance; // Pega a instância do InputManager
    }

    // Métodos chamados pelos botões
    private void OnLoadClicked() => onLoad?.Invoke(mySaveFileName);
    private void OnOverwriteClicked() => onOverwrite?.Invoke(mySaveFileName);
    private void OnDeleteClicked() => onDelete?.Invoke(mySaveFileName);
    private void OnRenameClicked()
    {
        inputManager.CallStringInput(
            prompt: "Enter new name for the save file:",
            placeholder: mySaveFileName,
            boxWidth: 630f,
            boxHeight: 300f,
            coordinates: new Vector2(0, 0),
            onResult: (newName) => {
                if (!string.IsNullOrEmpty(newName) && newName != mySaveFileName)
                {
                    // Adiciona o prefixo visualmente se necessário antes de enviar para o menu
                    if (!newName.StartsWith(SaveFilePrefix))
                    {
                        newName = SaveFilePrefix + newName;
                    }

                    Debug.Log($"Requesting rename to {newName}");
                    onRename?.Invoke(newName);
                }
            }
        );
    }
}