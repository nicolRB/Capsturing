using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TargetMapAsset))]
public class TargetMapAssetEditor : Editor
{
    // SerializedProperty references for the asset fields.
    private SerializedProperty mapSettings;
    private SerializedProperty events;
    private SerializedProperty generatedMap;

    private void OnEnable()
    {
        // Find properties by their field names in the ScriptableObject.
        mapSettings  = serializedObject.FindProperty("mapSettings");
        events       = serializedObject.FindProperty("events");
        generatedMap = serializedObject.FindProperty("generatedMap");
    }

    public override void OnInspectorGUI()
    {
        // Synchronize the serialized object with the target asset.
        serializedObject.Update();

        // --- Map Settings ---
        EditorGUILayout.PropertyField(mapSettings, true);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Events (edit here)", EditorStyles.boldLabel);

        // --- Events array (manual, with type-specific fields) ---
        events.arraySize = EditorGUILayout.IntField("Count", events.arraySize);

        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty evt = events.GetArrayElementAtIndex(i);

            SerializedProperty type          = evt.FindPropertyRelative("type");
            SerializedProperty spawnTime     = evt.FindPropertyRelative("spawnTime");

            // Event header with a remove button.
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            evt.isExpanded = EditorGUILayout.Foldout(evt.isExpanded, $"Event {i}  [{(EventType)type.enumValueIndex}]", true);
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                events.DeleteArrayElementAtIndex(i);
                break; // Avoid iterating over the modified array.
            }
            EditorGUILayout.EndHorizontal();

            if (evt.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(type);
                EditorGUILayout.PropertyField(spawnTime);

                // Type-specific fields.
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

        // Add a new event at the end of the list.
        if (GUILayout.Button("+ Add Event"))
        {
            events.arraySize++;
            SerializedProperty newEvent = events.GetArrayElementAtIndex(events.arraySize - 1);
            ApplyDefaultEventValues(newEvent);
        }

        GUILayout.Space(10);

        // --- Generated Result (read-only view) ---
        EditorGUILayout.LabelField("Generated Result (do not edit manually)", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(generatedMap, true);
        GUI.enabled = true;

        GUILayout.Space(10);

        // --- Generate button ---
        TargetMapAsset asset = (TargetMapAsset)target;

        if (GUILayout.Button("Generate Map", GUILayout.Height(30)))
        {
            asset.GenerateMap();
            EditorUtility.SetDirty(asset);
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            $"Generated targets: {asset.GeneratedMap.Count}",
            MessageType.Info
        );

        // Apply SerializedProperty changes back to the asset.
        serializedObject.ApplyModifiedProperties();
    }

    private void ApplyDefaultEventValues(SerializedProperty evt)
    {
        evt.FindPropertyRelative("type").enumValueIndex = (int)EventType.Target;
        evt.FindPropertyRelative("spawnTime").floatValue = 0f;

        // Target fields.
        evt.FindPropertyRelative("position").vector2Value = Vector2.zero;
        evt.FindPropertyRelative("size").floatValue = 1f;
        evt.FindPropertyRelative("lifetime").floatValue = 1f;
        evt.FindPropertyRelative("activationTime").floatValue = 0.5f;
        evt.FindPropertyRelative("fadeInDuration").floatValue = 0.5f;

        // Line sub-settings.
        SerializedProperty line = evt.FindPropertyRelative("line");
        line.FindPropertyRelative("startPos").vector2Value = Vector2.zero;
        line.FindPropertyRelative("endPos").vector2Value = Vector2.zero;
        line.FindPropertyRelative("amount").intValue = 1;
        line.FindPropertyRelative("arc").floatValue = 0f;
        line.FindPropertyRelative("duration").floatValue = 1f;

        // Target sub-settings (used by Line events).
        SerializedProperty target = evt.FindPropertyRelative("target");
        target.FindPropertyRelative("size").floatValue = 1f;
        target.FindPropertyRelative("lifetime").floatValue = 1f;
        target.FindPropertyRelative("activationTime").floatValue = 0.5f;
        target.FindPropertyRelative("fadeInDuration").floatValue = 0.5f;
    }
}