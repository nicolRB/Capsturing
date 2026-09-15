using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text fileNameText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button overwriteButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button renameButton;

    [SerializeField] private InputManager inputManager;

    private const string SaveFilePrefix = "save_";

    // Callbacks supplied by the save menu.
    private string mySaveFileName;
    private System.Action<string> onLoad;
    private System.Action<string> onOverwrite;
    private System.Action<string> onDelete;
    private System.Action<string> onRename;

    // Called by the menu immediately after instantiating the prefab.
    public void Setup(string fileName, System.Action<string> loadAction, System.Action<string> overwriteAction, 
    System.Action<string> deleteAction, System.Action<string> renameAction)
    {
        mySaveFileName = fileName;
        string displayName = fileName;
        // Remove the "save_" prefix from the display name when present.
        if (displayName.StartsWith(SaveFilePrefix))
        {
            displayName = displayName.Substring(5);
        }
        fileNameText.text = displayName;

        onLoad = loadAction;
        onOverwrite = overwriteAction;
        onDelete = deleteAction;
        onRename = renameAction;
        // Replace existing listeners with the current callbacks.
        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(OnLoadClicked);

        overwriteButton.onClick.RemoveAllListeners();
        overwriteButton.onClick.AddListener(OnOverwriteClicked);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(OnDeleteClicked);

        renameButton.onClick.RemoveAllListeners();
        renameButton.onClick.AddListener(OnRenameClicked);

        inputManager = InputManager.Instance;
    }

    // Button callbacks.
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
                    // Add the prefix before passing the name back to the menu.
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