using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TargetMapAsset))]
public class TargetMapAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha os campos padrão (mapSettings, events, generatedMap)
        DrawDefaultInspector();

        GUILayout.Space(10);

        TargetMapAsset asset = (TargetMapAsset)target;

        if (GUILayout.Button("Gerar Mapa", GUILayout.Height(30)))
        {
            asset.GenerateMap();

            // Marca o asset como modificado para o Unity salvar a alteração no disco
            EditorUtility.SetDirty(asset);
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            $"Alvos gerados: {asset.GeneratedMap.Count}",
            MessageType.Info
        );
    }
}