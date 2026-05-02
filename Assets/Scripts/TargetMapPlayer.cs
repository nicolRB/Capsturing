using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

public class TargetMapPlayer : MonoBehaviour
{
    public GameObject targetPrefab;
    // public RectTransform canvas; // Removed, using transform

    public List<TargetData> map;
    
    private CastingGameScript castingGameScript;

    private float startTime;
    private int currentIndex = 0;

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
                SpawnTarget(map[currentIndex]);
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

    void SpawnTarget(TargetData data)
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

            targetScript.perfectWindow = 0.08f;
            targetScript.hitWindow = 0.2f;
            targetScript.missDuration = 0.2f;
            targetScript.targetIndex = currentIndex;
        }
    }

    // Loads a target map from a JSON file and populates the map list
    public void LoadMap(string mapPath)
    {
        try
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>(mapPath);
            if (jsonAsset == null)
            {
                Debug.LogError($"Map file not found: {mapPath}");
                return;
            }
            string json = jsonAsset.text;
            TargetMapWrapper wrapper = JsonUtility.FromJson<TargetMapWrapper>(json);
            if (wrapper == null || wrapper.targets == null)
            {
                Debug.LogError("Invalid JSON format: missing targets array.");
                return;
            }
            map = new List<TargetData>(wrapper.targets);
            map.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime)); // Ensure sorted by spawnTime
            Debug.Log($"Loaded map with {map.Count} targets.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load map: {e.Message}");
        }
    }

    [System.Serializable]
    public class TargetMapWrapper
    {
        public TargetData[] targets;
    }

    void ResetMap()
    {
        startTime = Time.time;
        currentIndex = 0;
    }
}