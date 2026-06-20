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
    public GameObject targetPrefab;

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
        if (castingGameScript.castingMode != 2 || map == null)
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

    // Carrega o mapa a partir de um TargetMapAsset (ScriptableObject)
    public void LoadMap(TargetMapAsset mapAsset)
    {
        if (mapAsset == null)
        {
            Debug.LogError("TargetMapPlayer.LoadMap: nenhum asset atribuído.");
            return;
        }

        if (mapAsset.GeneratedMap == null || mapAsset.GeneratedMap.Count == 0)
        {
            Debug.LogWarning($"TargetMapPlayer.LoadMap: o asset '{mapAsset.name}' não tem mapa gerado. Clique em 'Gerar Mapa' no Inspector do asset.");
            return;
        }

        // Copia a lista para não modificar os dados congelados dentro do asset
        map = new List<TargetData>(mapAsset.GeneratedMap);

        if (mapAsset.mapSettings != null)
        {
            mapHitWindow    = mapAsset.mapSettings.hitWindow;
            mapPerfectWindow = mapAsset.mapSettings.perfectWindow;
        }
        else
        {
            Debug.LogWarning($"TargetMapPlayer.LoadMap: asset '{mapAsset.name}' sem MapSettings, usando valores padrão.");
            mapHitWindow    = 0.2f;
            mapPerfectWindow = 0.08f;
        }

        Debug.Log($"Mapa '{mapAsset.name}' carregado com {map.Count} alvos.");
    }

    public void ResetMap()
    {
        startTime = Time.time;
        currentIndex = 0;
    }
}