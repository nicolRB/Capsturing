using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public static SceneChanger Instance { get; private set; }

    // Destination scene, read by the Loader in the LoadingScene.
    public static int TargetSceneIndex { get; private set; }

    [SerializeField] private int loadingSceneIndex = 1;

    private enum PlacementMode { None, Entry, Coordinate }
    private PlacementMode pendingMode = PlacementMode.None;
    private string pendingEntryId;
    private Vector3 pendingPosition;
    private Quaternion pendingRotation;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"SceneChanger duplicado em '{name}' (cena '{gameObject.scene.name}'). " +
                $"Mantendo '{Instance.name}' (cena '{Instance.gameObject.scene.name}').", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Plain load: no placement (menu, new game, etc.).
    public void LoadSceneByIndex(int sceneIndex)
    {
        pendingMode = PlacementMode.None;
        BeginLoad(sceneIndex);
    }

    public void LoadSceneByName(string sceneName)
    {
        int sceneIndex = FindBuildIndex(sceneName);
        if (sceneIndex < 0)
        {
            Debug.LogError($"Scene '{sceneName}' was not found in Build Settings.", this);
            return;
        }
        LoadSceneByIndex(sceneIndex);
    }

    public void LoadSceneAtEntry(int sceneIndex, string entryId)
    {
        pendingMode = PlacementMode.Entry;
        pendingEntryId = entryId;
        BeginLoad(sceneIndex);   // não LoadSceneByIndex
    }

    public void LoadSceneAtCoordinate(int sceneIndex, Vector3 position, Quaternion rotation)
    {
        pendingMode = PlacementMode.Coordinate;
        pendingPosition = position;
        pendingRotation = rotation;
        BeginLoad(sceneIndex);   // não LoadSceneByIndex
    }

     // ---------------- Internals ----------------

    private void BeginLoad(int sceneIndex)
    {
        TargetSceneIndex = sceneIndex;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetFrozen(true);

        SceneManager.LoadScene(loadingSceneIndex);
    }

    private int FindBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return i;
        }
        return -1;
    }

    // Fires for the LoadingScene too, so it only reacts to the destination scene.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != TargetSceneIndex) return;
        StartCoroutine(FinishLoadNextFrame());
    }

    private IEnumerator PlacePlayerNextFrame()
    {
        // Lets the Start() calls of the new scene run first.
        yield return null;

        PlacementMode mode = pendingMode;
        pendingMode = PlacementMode.None;

        Vector3 position = pendingPosition;
        Quaternion rotation = pendingRotation;

        if (mode == PlacementMode.Entry)
        {
            EntryPoint found = null;
            foreach (var entry in FindObjectsByType<EntryPoint>(FindObjectsSortMode.None))
            {
                if (entry.Id == pendingEntryId) { found = entry; break; }
            }

            if (found == null)
            {
                Debug.LogWarning($"EntryPoint '{pendingEntryId}' não encontrado na cena '{SceneManager.GetActiveScene().name}'.");
                yield break;
            }

            position = found.Position;
            rotation = found.Rotation;
        }

        if (PlayerController.Instance == null)
        {
            Debug.LogWarning("PlayerController não encontrado para posicionar.");
            yield break;
        }

        PlayerController.Instance.TeleportTo(position, rotation);
    }

    private IEnumerator FinishLoadNextFrame()
    {
        // Lets the Start() methods of the new scene run first.
        yield return null;

        var player = PlayerController.Instance;
        if (player == null)
        {
            pendingMode = PlacementMode.None;
            yield break;
        }

        if (TryGetPlacement(out Vector3 position, out Quaternion rotation))
            player.TeleportTo(position, rotation);

        pendingMode = PlacementMode.None;
        player.SetFrozen(false);   // Always unfreezes
    }

    private bool TryGetPlacement(out Vector3 position, out Quaternion rotation)
    {
        position = pendingPosition;
        rotation = pendingRotation;

        switch (pendingMode)
        {
            case PlacementMode.Coordinate:
                return true;

            case PlacementMode.Entry:
                foreach (var entry in FindObjectsByType<EntryPoint>(FindObjectsSortMode.None))
                {
                    if (entry.Id != pendingEntryId) continue;
                    position = entry.Position;
                    rotation = entry.Rotation;
                    return true;
                }
                Debug.LogWarning($"EntryPoint '{pendingEntryId}' não encontrado na cena '{SceneManager.GetActiveScene().name}'.");
                return false;

            default:
                return false;
        }
    }
}