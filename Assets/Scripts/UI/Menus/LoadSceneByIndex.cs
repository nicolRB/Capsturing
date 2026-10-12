using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneByIndex : MonoBehaviour
{
    // Destination scene, read by the Loader in the LoadingScene.
    public static int TargetSceneIndex { get; private set; }

    [SerializeField] private int loadingSceneIndex = 1; // LoadingScene build index

    public void LoadScene(int sceneIndex)
    {
        TargetSceneIndex = sceneIndex;
        SceneManager.LoadScene(loadingSceneIndex);
    }
}