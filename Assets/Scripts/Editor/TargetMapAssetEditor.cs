using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TargetMapAsset))]
public class TargetMapAssetEditor : Editor
{
    // Referências às SerializedProperties dos campos do asset
    private SerializedProperty mapSettings;
    private SerializedProperty events;
    private SerializedProperty generatedMap;

    private void OnEnable()
    {
        // Busca as propriedades pelo nome do campo no ScriptableObject
        mapSettings  = serializedObject.FindProperty("mapSettings");
        events       = serializedObject.FindProperty("events");
        generatedMap = serializedObject.FindProperty("generatedMap");
    }

    public override void OnInspectorGUI()
    {
        // Sincroniza o objeto serializado com os dados reais
        serializedObject.Update();

        // --- Map Settings ---
        EditorGUILayout.PropertyField(mapSettings, true);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Eventos (editar aqui)", EditorStyles.boldLabel);

        // --- Events array (manual, com lógica condicional por tipo) ---
        events.arraySize = EditorGUILayout.IntField("Quantidade", events.arraySize);

        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty evt = events.GetArrayElementAtIndex(i);

            SerializedProperty type          = evt.FindPropertyRelative("type");
            SerializedProperty spawnTime     = evt.FindPropertyRelative("spawnTime");

            // Cabeçalho do evento com botão de remover
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            evt.isExpanded = EditorGUILayout.Foldout(evt.isExpanded, $"Evento {i}  [{(EventType)type.enumValueIndex}]", true);
            if (GUILayout.Button("−", GUILayout.Width(24)))
            {
                events.DeleteArrayElementAtIndex(i);
                break; // evita iterar sobre array modificado
            }
            EditorGUILayout.EndHorizontal();

            if (evt.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(type);
                EditorGUILayout.PropertyField(spawnTime);

                // Campos condicionais por tipo
                if ((EventType)type.enumValueIndex == EventType.Target)
                {
                    EditorGUILayout.PropertyField(evt.FindPropertyRelative("position"));
                    EditorGUILayout.PropertyField(evt.FindPropertyRelative("size"));
                    EditorGUILayout.PropertyField(evt.FindPropertyRelative("lifetime"));
                    EditorGUILayout.PropertyField(evt.FindPropertyRelative("activationTime"));
                    EditorGUILayout.PropertyField(evt.FindPropertyRelative("fadeInDuration"));
                }
                else if ((EventType)type.enumValueIndex == EventType.Line)
                {
                    EditorGUILayout.PropertyField(evt.FindPropertyRelative("line"), true);
                    EditorGUILayout.PropertyField(evt.FindPropertyRelative("target"), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        // Botão para adicionar novo evento no final da lista
        if (GUILayout.Button("+ Adicionar Evento"))
        {
            events.arraySize++;
        }

        GUILayout.Space(10);

        // --- Resultado Gerado (somente leitura visual) ---
        EditorGUILayout.LabelField("Resultado Gerado (não editar manualmente)", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(generatedMap, true);
        GUI.enabled = true;

        GUILayout.Space(10);

        // --- Botão Gerar ---
        TargetMapAsset asset = (TargetMapAsset)target;

        if (GUILayout.Button("Gerar Mapa", GUILayout.Height(30)))
        {
            asset.GenerateMap();
            EditorUtility.SetDirty(asset);
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            $"Alvos gerados: {asset.GeneratedMap.Count}",
            MessageType.Info
        );

        // Aplica mudanças feitas via SerializedProperty de volta ao objeto
        serializedObject.ApplyModifiedProperties();
    }
}