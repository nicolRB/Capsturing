using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NovoMapaDeAlvos", menuName = "Magia/Mapa de Alvos")]
public class TargetMapAsset : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] 
    private MapSettings mapSettings;
    public MapSettings MapSettings => mapSettings;

    [Header("Events (edit here)")]
    [SerializeField]
    private MapEvent[] events;

    [Header("Generated Result (do not edit manually)")]
    [SerializeField]
    private List<TargetData> generatedMap = new List<TargetData>();

    // Read-only access to the expanded runtime map.
    public List<TargetData> GeneratedMap => generatedMap;

    // Called by the TargetMapAssetEditor Inspector button.
    public void GenerateMap()
    {
        generatedMap = new List<TargetData>();

        if (events == null)
        {
            Debug.LogWarning($"TargetMapAsset '{name}': no events defined.", this);
            return;
        }

        foreach (var e in events)
        {
            if (e.type == EventType.Target)
            {
                generatedMap.Add(new TargetData(
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

        generatedMap.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

        Debug.Log($"TargetMapAsset '{name}': generated {generatedMap.Count} targets.", this);
    }

    private void GenerateLineTargets(MapEvent e)
    {
        var line = e.line;
        var tgt = e.target;

        if (line == null || tgt == null)
        {
            Debug.LogWarning($"TargetMapAsset '{name}': Line event has no configured line or target settings.", this);
            return;
        }

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

            generatedMap.Add(new TargetData(
                spawnTime,
                pos,
                tgt.size,
                tgt.lifetime,
                tgt.activationTime,
                tgt.fadeInDuration
            ));
        }
    }
}