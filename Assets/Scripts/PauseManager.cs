using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu;
    public PlayerController player;

    public bool isPaused = false;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (pauseMenu == null) pauseMenu = GameObject.Find("PauseMenu");
        Resume(); // ensure correct initial state
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
        {
            if (isPaused) Resume(); 
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;

        player.playerHUD.SetActive(false);

        pauseMenu.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        pauseMenu.SetActive(false);

        if (player.castState != PlayerController.CastState.Channeling) player.playerHUD.SetActive(true);

        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false; // stops Play mode
        #else
            Application.Quit(); // closes the game build
        #endif
    }
}