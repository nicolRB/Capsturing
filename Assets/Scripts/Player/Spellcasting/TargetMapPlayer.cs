using UnityEngine;
using System.Collections.Generic;

public enum EventType
{
    Target,
    Line
}

[System.Serializable]
public class MapSettings
{
    [Header("Timing")]
    [Tooltip("Maximum delay after the ideal hit time that still counts as a hit.")]
    public float hitWindow = 0.2f;
    [Tooltip("Maximum delay from the ideal hit time for a perfect result.")]
    public float perfectWindow = 0.08f;
    [Tooltip("Display name used to identify the map.")]
    public string name = "Novo Mapa";
}

[System.Serializable]
public class TargetData
{
    public float spawnTime;
    public Vector2 position;
    public float size;
    public float lifetime;
    public float activationTime;
    public float fadeInDuration;
    public TargetData(float spawnTime, Vector2 position, float size, float lifetime, 
    float activationTime, float fadeInDuration)
    {
        this.spawnTime = spawnTime;
        this.position = position;
        this.size = size;
        this.lifetime = lifetime;
        this.activationTime = activationTime;
        this.fadeInDuration = fadeInDuration;
    }
}

[System.Serializable]
public class MapEvent
{
    public EventType type;
    public float spawnTime;

    // Target event settings.
    public Vector2 position;
    public float size;
    public float lifetime;
    public float activationTime;
    public float fadeInDuration;

    // Line event settings.
    public LineSettings line = new LineSettings();
    public TargetSettings target = new TargetSettings();
}

[System.Serializable]
public class LineSettings
{
    public Vector2 startPos;
    public Vector2 endPos;
    public int amount;
    public float arc;
    public float duration;
}

[System.Serializable]
public class TargetSettings
{
    public float size;
    public float lifetime;
    public float activationTime;
    public float fadeInDuration;
}

public class TargetMapPlayer : MonoBehaviour
{
    [SerializeField] private GameObject targetPrefab;
    private List<TargetData> map = new List<TargetData>();

    public IReadOnlyList<TargetData> Map => map;
    public int TotalTargets => map.Count;
    
    private ChannelingGameScript castingGameScript;

    private float startTime;
    private int currentIndex = 0;
    private float mapHitWindow;
    private float mapPerfectWindow;

    void Start()
    {
        startTime = Time.time;
        castingGameScript = Object.FindFirstObjectByType<ChannelingGameScript>();
        if (castingGameScript == null)
            Debug.LogError("TargetMapPlayer: ChannelingGameScript not found in the scene.", this);

        if (targetPrefab == null)
            Debug.LogError("TargetMapPlayer: target prefab is not assigned.", this);
    }

    void Update()
    {
        if (castingGameScript == null ||
            castingGameScript.Player == null ||
            castingGameScript.Player.CastingState != PlayerController.CastState.Channeling)
        {
            return;
        }

        if (map.Count > currentIndex)
        {
            float elapsed = Time.time - startTime;

            while (currentIndex < map.Count && elapsed >= map[currentIndex].spawnTime)
            {
                SpawnTarget(map[currentIndex], currentIndex);
                currentIndex++;
            }
        }
    }

    void SpawnTarget(TargetData data, int index)
    {
        if (targetPrefab == null)
            return;

        GameObject obj = Instantiate(targetPrefab, transform);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("TargetMapPlayer: target prefab requires a RectTransform.", obj);
            Destroy(obj);
            return;
        }

        rt.anchoredPosition = data.position;

        TargetScript targetScript = obj.GetComponent<TargetScript>();

        if (targetScript != null)
            targetScript.Setup(data.size, data.lifetime, mapPerfectWindow, mapHitWindow, index, castingGameScript);
        
    }

    // Load a map from a TargetMapAsset.
    public void LoadMap(TargetMapAsset mapAsset)
    {
        if (mapAsset == null)
        {
            Debug.LogError("TargetMapPlayer.LoadMap: no asset assigned.");
            return;
        }

        if (mapAsset.GeneratedMap == null || mapAsset.GeneratedMap.Count == 0)
        {
            Debug.LogWarning($"TargetMapPlayer.LoadMap: asset '{mapAsset.name}' has no generated map. Generate it in the asset Inspector.");
            return;
        }

        // Copy the list so the asset data remains unchanged.
        map = new List<TargetData>(mapAsset.GeneratedMap);

        if (mapAsset.MapSettings != null)
        {
            mapHitWindow    = mapAsset.MapSettings.hitWindow;
            mapPerfectWindow = mapAsset.MapSettings.perfectWindow;
        }
        else
        {
            Debug.LogWarning($"TargetMapPlayer.LoadMap: asset '{mapAsset.name}' has no MapSettings; using defaults.");
            mapHitWindow    = 0.2f;
            mapPerfectWindow = 0.08f;
        }

        Debug.Log($"Map '{mapAsset.name}' loaded with {map.Count} targets.");
    }

    public void ResetMap()
    {
        startTime = Time.time;
        currentIndex = 0;
    }
}