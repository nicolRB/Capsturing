using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuManager : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject saveLoadMenu;
    [SerializeField] private GameObject runicStorageMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject runicInspectionMenu;
    private GameObject currentMenu;
    private readonly List<GameObject> previousMenus = new List<GameObject>();

    public GameObject CurrentMenu => currentMenu;

    [Header("Other References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject popupTextPrefab; // "Game Saved" popup for example
    [SerializeField] private Canvas canvas;

    public PlayerController Player => player;

    [Header("Menu State")]
    [Tooltip("Enables pause and gameplay menus while the player is in the game.")]
    [SerializeField] private bool onGame = true;
    private bool isPaused = false;

    public bool IsPaused => isPaused;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (canvas == null) canvas = gameObject.GetComponent<Canvas>();

        if (pauseMenu == null) pauseMenu = GameObject.Find("PauseMenu");

        Resume();

        if (saveLoadMenu != null) saveLoadMenu.SetActive(false);

        if (runicStorageMenu != null) runicStorageMenu.SetActive(false);

        if (runicInspectionMenu != null) runicInspectionMenu.SetActive(false);

        if (settingsMenu != null) settingsMenu.SetActive(false);

        if (SaveManager.Instance != null && SaveManager.Instance.TryConsumePendingLoad(out SaveDataContainer pending)
        && RunicStorageManager.Instance != null)
        {
            player.LoadPlayerData(pending);
            RunicStorageManager.Instance.LoadPartyData(pending);
        }
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasReleasedThisFrame && onGame) Escape();
        if (Keyboard.current.tabKey.wasPressedThisFrame && onGame && !isPaused 
        && currentMenu != runicStorageMenu) Open(runicStorageMenu);
        else if (Keyboard.current.tabKey.wasPressedThisFrame && currentMenu == runicStorageMenu) Escape();
    }

    public void Open(GameObject menu)
    {
        if (currentMenu != null)
        {
            previousMenus.Add(currentMenu);
            currentMenu.SetActive(false);
        }
        currentMenu = menu;
        currentMenu.SetActive(true);

        if (player != null && player.PlayerHUD != null)
        {
            player.PlayerHUD.SetActive(false);
        }
    }

    public void Escape()
    {
        if (!isPaused && currentMenu == null) Pause(); 
        else if (currentMenu == pauseMenu) Resume();
        else if (previousMenus.Count > 0)
        {
            currentMenu.SetActive(false);
            int lastIndex = previousMenus.Count - 1;
            currentMenu = previousMenus[lastIndex];
            previousMenus.RemoveAt(lastIndex);
            currentMenu.SetActive(true);
        }
        else
        {
            currentMenu.SetActive(false);
            currentMenu = null;

            if (!isPaused && player != null && player.PlayerHUD != null)
            {
                player.PlayerHUD.SetActive(true);
            }
        }
    }

    public void OpenSaveLoadMenu() => Open(saveLoadMenu);
    public void OpenSettingsMenu() => Open(settingsMenu);
    public void OpenRunicStorageMenu() => Open(runicStorageMenu);
    public void OpenRunicInspectionMenu(RunicSaveData data)
    {
        Open(runicInspectionMenu);
        RunicInspectionMenu inspectionMenu = runicInspectionMenu.GetComponent<RunicInspectionMenu>();
        inspectionMenu.SetupPage(data);
    }

    public void Pause()
    {
        isPaused = true;

        currentMenu = pauseMenu;

        if (player != null && player.PlayerHUD != null)
            player.PlayerHUD.SetActive(false);

        pauseMenu.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        currentMenu = null;

        if (pauseMenu != null) pauseMenu.SetActive(false);

        if (player != null && player.PlayerHUD != null && player.CastingState != CastState.Channeling)
            player.PlayerHUD.SetActive(true);

        Time.timeScale = 1f;
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void CloseAllMenus()
    {
        if (currentMenu != null)
        {
            currentMenu.SetActive(false);
            currentMenu = null;
        }
        previousMenus.Clear();
        Resume(); // Ensure the game is unpaused when closing all menus
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false; // stops Play mode
        #else
            Application.Quit(); // closes the game build
        #endif
    }

    public SaveDataContainer GetCurrentGameData()
    {
        SaveDataContainer data = new SaveDataContainer();
        player.PopulateSaveData(data);
        RunicStorageManager.Instance.PopulateSaveData(data);
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;
        return data;
    }

    public void SaveGame()
    {
        SaveManager.Instance.SaveGame(GetCurrentGameData());
        PopupText(new Vector2(-800, -400), "Game Saved", 50, new Color(0.12f, 0.12f, 0.12f));
    }

    public void MainMenu()
    {
        SceneChanger.Instance.LoadSceneByIndex(0);
    }

    public void PopupText(Vector2 coordinates, string text, float fontSize, Color color)
    {
        if (popupTextPrefab == null)
        {
            Debug.LogError("popupTextPrefab not assigned.");
            return;    
        }

        GameObject obj = Instantiate(popupTextPrefab, canvas.transform);
        if (obj == null)
        {
            Debug.LogError("MenuManager: Failed to instantiate textPrefab.", this);
            return;
        }

        PopupText popup = obj.GetComponent<PopupText>();
        if (popup == null)
        {
            Debug.LogError("MenuManager: textPrefab does not have a PopupText component.", this);
            Destroy(obj);
            return;
        }
        
        popup.Setup(coordinates, text, fontSize, color, 0.6f, 50);
        popup.Play();
    }
}