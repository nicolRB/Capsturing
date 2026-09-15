using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.VisualScripting;


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
    private GameObject currentMenu;
    private readonly List<GameObject> previousMenus = new List<GameObject>();

    public GameObject CurrentMenu => currentMenu;

    [Header("Other References")]
    [SerializeField] private PlayerController player;

    public PlayerController Player => player;

    [Header("Menu State")]
    [Tooltip("Enables pause and gameplay menus while the player is in the game.")]
    [SerializeField] private bool onGame = true;
    private bool isPaused = false;

    public bool IsPaused => isPaused;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (pauseMenu == null) pauseMenu = GameObject.Find("PauseMenu");
        Resume();
        if (saveLoadMenu != null) saveLoadMenu.SetActive(false);
        if (runicStorageMenu != null) runicStorageMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(false);
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

    public void Pause()
    {
        isPaused = true;

        currentMenu = pauseMenu;

        player.PlayerHUD.SetActive(false);

        pauseMenu.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        currentMenu = null;

        pauseMenu.SetActive(false);

        if (player.CastingState != PlayerController.CastState.Channeling) player.PlayerHUD.SetActive(true);

        Time.timeScale = 1f;
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

    public void SaveGame()
    {
        
    }
}