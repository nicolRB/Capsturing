using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // Destination scene, read by the Loader in the LoadingScene.
    public static int TargetSceneIndex { get; private set; }

    [SerializeField] private int loadingSceneIndex = 1; // LoadingScene build index

    public void LoadSceneByIndex(int sceneIndex)
    {
        TargetSceneIndex = sceneIndex;
        SceneManager.LoadScene(loadingSceneIndex);
    }

    public void LoadSceneByName(string sceneName)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCountInBuildSettings; sceneIndex++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            string buildSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (buildSceneName == sceneName)
            {
                LoadSceneByIndex(sceneIndex);
                return;
            }
        }

        Debug.LogError($"Scene '{sceneName}' was not found in Build Settings.", this);
    }
}