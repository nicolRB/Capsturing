using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Loader : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Settings")]
    [SerializeField] private float minimumLoadingTime = 1.5f;

    void Start()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // Starts loading asynchronously.
        AsyncOperation operation = SceneManager.LoadSceneAsync(LoadSceneByIndex.TargetSceneIndex);
        
        // Prevents the scene from automatically activating as soon as loading is complete 
        // (optional, good for allowing time to show 100%).
        operation.allowSceneActivation = false;
        
        float elapsed = 0f;

        while (!operation.isDone)
        {
            // Progress goes from 0 to 0.9 when allowSceneActivation is false.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            if (progressText != null)
                progressText.text = (progress * 100f).ToString("F0") + "%";

            // After reaching 90% (loaded into memory), allows the scene to be activated.
            if (operation.progress >= 0.9f && elapsed >= minimumLoadingTime)
                operation.allowSceneActivation = true;

            yield return null;
        }
    }
}