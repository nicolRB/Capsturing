using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenu;

    private bool isPaused = false;

    void Start()
    {
        Resume(); // ensure correct initial state
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;

        pauseMenu.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;

        pauseMenu.SetActive(false);

        Time.timeScale = 1f;

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
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