using UnityEditor;

[CustomEditor(typeof(Runic))]
public class RunicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "capturable", "captureMap", "targetPerfectWeight", "targetGoodWeight", "targetMissWeight", 
        "CCModifier", "CCMultiplier", "perfectBonus", "perfectMultiplier", "goodBonus",
        "goodMultiplier", "missBonus", "missMultiplier", "captureChainsPrefab", "chainYAngleMinVariance",
        "chainYAngleMaxVariance", "chainMinZAngle", "chainMaxZAngle", "minChainNumber",
        "maxChainNumber", "chainSpawnInterval");

        SerializedProperty capturable = serializedObject.FindProperty("capturable");

        EditorGUILayout.PropertyField(capturable);

        if (capturable.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("captureMap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetPerfectWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetGoodWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetMissWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CCModifier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CCMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("perfectBonus"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("perfectMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("goodBonus"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("goodMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("missBonus"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("missMultiplier"));

            SerializedProperty captureChains = serializedObject.FindProperty("captureChainsPrefab");
            EditorGUILayout.PropertyField(captureChains);
            if (captureChains.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chainYAngleMinVariance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chainYAngleMaxVariance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chainMinZAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chainMaxZAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minChainNumber"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxChainNumber"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chainSpawnInterval"));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}