using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    public void QuitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false; // stops Play mode
        #else
            Application.Quit(); // closes the game build
        #endif
    }

    public void LoadScene(int sceneIndex)
    {
        if (SceneChanger.Instance != null)
        {
            SceneChanger.Instance.LoadSceneByIndex(sceneIndex);
        }
        else
        {
            Debug.LogError("MainMenu: Scene Changer Instance not found.");
        }
    }
}