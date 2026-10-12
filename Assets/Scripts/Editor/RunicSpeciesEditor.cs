using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RunicSpecies))]
public class RunicSpeciesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Updates the serialized representation
        serializedObject.Update();

        // Draws the remaining script properties that we haven't handled manually
        // (You can list the other fields or use a loop through the properties to draw the rest)
        DrawPropertiesExcluding(serializedObject, "packSpecies", "packSpawnChance", "minPackSize", "maxPackSize");

        // Automatically draws all other standard fields, except for the pack fields.
        SerializedProperty packSpeciesProp = serializedObject.FindProperty("packSpecies");
        
        // Draws teh main bool
        EditorGUILayout.PropertyField(packSpeciesProp);

        // If 'packSpecies' is checked (true), it draws the related fields below it.
        if (packSpeciesProp.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("packSpawnChance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minPackSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPackSize"));
        }

        // Apply changes
        serializedObject.ApplyModifiedProperties();
    }
}