using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public enum EventType
{
    Target,
    Line
}

[System.Serializable]
public class MapSettings
{
    public float hitWindow;
    public float perfectWindow;
    public string name;
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

    // target
    public Vector2 position;
    public float size;
    public float lifetime;
    public float activationTime;
    public float fadeInDuration;

    // line
    public LineSettings line;
    public TargetSettings target;
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

[System.Serializable]
public class MapWrapper
{
    public MapEvent[] events;
    public MapSettings[] MapSettings;
}

public class TargetMapPlayer : MonoBehaviour
{
    public GameObject targetPrefab;
    // public RectTransform canvas; // Removed, using transform

    public List<TargetData> map;
    
    private CastingGameScript castingGameScript;

    private float startTime;
    private int currentIndex = 0;
    private float mapHitWindow;
    private float mapPerfectWindow;

    void Start()
    {
        startTime = Time.time;
        castingGameScript = Object.FindFirstObjectByType<CastingGameScript>();
        if (castingGameScript == null)
        {
            Debug.LogError("CastingGameScript not found in the scene!");
        }
    }

    void Update()
    {
        if (castingGameScript.castingMode != 2 || map == null) // Only run if TargetMap mode is active and map is loaded
            return;
        else if (castingGameScript.castingMode == 2 && 
        castingGameScript.player.casting == true)
        {
            float elapsed = Time.time - startTime;

            while (currentIndex < map.Count && elapsed >= map[currentIndex].spawnTime)
            {
                SpawnTarget(map[currentIndex], currentIndex);
                currentIndex++;
            }
        }
        
        if (Keyboard.current.eKey.wasPressedThisFrame || 
        Keyboard.current.digit2Key.wasPressedThisFrame || 
        Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            startTime = Time.time;
            currentIndex = 0;
            castingGameScript.ResetCast();
        }
    }

    void SpawnTarget(TargetData data, int index)
    {
        GameObject obj = Instantiate(targetPrefab, transform);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.position;

        TargetScript targetScript = obj.GetComponent<TargetScript>();

        if (targetScript != null)
        {
            targetScript.size = data.size;
            targetScript.lifetime = data.lifetime;
            targetScript.activationTime = data.activationTime;
            targetScript.fadeInDuration = data.fadeInDuration;

            targetScript.perfectWindow = mapPerfectWindow > 0 ? mapPerfectWindow : 0.01f;
            targetScript.hitWindow = mapHitWindow > 0 ? mapHitWindow : 0.2f;
            targetScript.missDuration = 0.2f;

            targetScript.targetIndex = index;
        }
    }

    // Loads a target map from a JSON file and populates the map list
    public void LoadMap(string mapPath)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(mapPath);

        if (jsonAsset == null)
        {
            Debug.LogError($"Map file not found: {mapPath}");
            return;
        }

        MapWrapper wrapper = JsonUtility.FromJson<MapWrapper>(jsonAsset.text);

        map = new List<TargetData>();

        foreach (var e in wrapper.events)
        {
            if (e.type == EventType.Target)
            {
                map.Add(new TargetData(
                    e.spawnTime,
                    e.position,
                    e.size,
                    e.lifetime,
                    e.activationTime,
                    e.fadeInDuration
                ));
            }
            else if (e.type == EventType.Line)
            {
                GenerateLineTargets(e);
            }
        }

        if (wrapper.MapSettings != null && wrapper.MapSettings.Length > 0)
        {
            mapHitWindow = wrapper.MapSettings[0].hitWindow;
            mapPerfectWindow = wrapper.MapSettings[0].perfectWindow;
        }
        else
        {
            Debug.LogWarning("MapSettings not found, using default values.");
            mapHitWindow = 0.2f;
            mapPerfectWindow = 0.08f;
        }        

        map.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

        Debug.Log($"Loaded map with {map.Count} targets (after expansion).");
    }

    void GenerateLineTargets(MapEvent e)
    {
        var line = e.line;
        var tgt = e.target;

        int count = line.amount;
        if (count <= 0) return;

        Vector2 dir = line.endPos - line.startPos;
        Vector2 perp = new Vector2(-dir.y, dir.x).normalized;

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;

            Vector2 pos = Vector2.Lerp(line.startPos, line.endPos, t);

            float arcOffset = Mathf.Sin(t * Mathf.PI) * line.arc;
            pos += perp * arcOffset;

            float spawnTime = count > 1
                ? e.spawnTime + t * line.duration
                : e.spawnTime;

            map.Add(new TargetData(
                spawnTime,
                pos,
                tgt.size,
                tgt.lifetime,
                tgt.activationTime,
                tgt.fadeInDuration
            ));
        }
    }

    public void ResetMap()
    {
        startTime = Time.time;
        currentIndex = 0;
    }
}