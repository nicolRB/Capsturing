using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NovoMapaDeAlvos", menuName = "Captura/Mapa de Alvos")]
public class TargetMapAsset : ScriptableObject
{
    [Header("Configurações Gerais")]
    public MapSettings mapSettings;

    [Header("Eventos (editar aqui)")]
    public MapEvent[] events;

    [Header("Resultado Gerado (não editar manualmente)")]
    [SerializeField]
    private List<TargetData> generatedMap = new List<TargetData>();

    // Acesso somente leitura à lista já expandida, usada em runtime
    public List<TargetData> GeneratedMap => generatedMap;

    // Chamado pelo botão no Inspector (TargetMapAssetEditor)
    public void GenerateMap()
    {
        generatedMap = new List<TargetData>();

        if (events == null)
        {
            Debug.LogWarning($"TargetMapAsset '{name}': nenhum evento definido.", this);
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

        Debug.Log($"TargetMapAsset '{name}': mapa gerado com {generatedMap.Count} alvos.", this);
    }

    private void GenerateLineTargets(MapEvent e)
    {
        var line = e.line;
        var tgt = e.target;

        if (line == null || tgt == null)
        {
            Debug.LogWarning($"TargetMapAsset '{name}': evento do tipo Line sem 'line' ou 'target' configurado.", this);
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