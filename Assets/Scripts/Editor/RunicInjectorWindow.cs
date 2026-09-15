using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RunicInjectorWindow : EditorWindow
{
    private string targetSaveFile = "save_test_save";
    private RunicSpecies selectedSpecies;
    private string customNickname = "";
    private int level = 1;
    private int experience;
    private bool addToParty = false;
    private bool useCustomValues;
    private Sprite customIcon;
    private GameObject customModel;
    private float customMaxHP;
    private float customCurrentHP;
    private float customAttack;
    private float customDefense;
    private float customSpeed;
    private float customMagic;
    private float customMagicDefense;
    private RunicSpecies lastSelectedSpecies;

    [MenuItem("Runic/Runic Injector")]
    public static void ShowWindow()
    {
        GetWindow<RunicInjectorWindow>("Runic Injector");
    }

    private void OnGUI()
    {
        GUILayout.Label("Runic Injection Tool", EditorStyles.boldLabel);

        targetSaveFile = EditorGUILayout.TextField("Save Name", targetSaveFile);
        selectedSpecies = EditorGUILayout.ObjectField("Species", selectedSpecies, typeof(RunicSpecies), false) as RunicSpecies;
        if (selectedSpecies != lastSelectedSpecies)
        {
            lastSelectedSpecies = selectedSpecies;
            LoadSpeciesDefaults();
        }
        useCustomValues = EditorGUILayout.Toggle("Use Custom Values", useCustomValues);

        if (useCustomValues)
        {
            DrawCustomFields();
        }
        else
        {
            EditorGUILayout.HelpBox("The icon, model, stats, elements, and skills will be copied from the species. Level will be 1 and XP will be 0.", MessageType.Info);
        }

        addToParty = EditorGUILayout.Toggle("Add to Party", addToParty);

        using (new EditorGUI.DisabledScope(selectedSpecies == null))
        {
            if (GUILayout.Button("Inject Runic into Save"))
            {
                InjectRunicToSave();
            }

            if (GUILayout.Button("Inject Runic into Runtime Box"))
            {
                InjectRunicToRuntimeBox();
            }
        }
    }

    private void DrawCustomFields()
    {
        customNickname = EditorGUILayout.TextField("Nickname (Optional)", customNickname);
        level = Mathf.Max(1, EditorGUILayout.IntField("Level", level));
        experience = Mathf.Max(0, EditorGUILayout.IntField("Experience", experience));

        EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
        customIcon = EditorGUILayout.ObjectField("Icon", customIcon, typeof(Sprite), false) as Sprite;
        customModel = EditorGUILayout.ObjectField("Model", customModel, typeof(GameObject), false) as GameObject;

        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        customMaxHP = EditorGUILayout.FloatField("Max HP", customMaxHP);
        customCurrentHP = EditorGUILayout.FloatField("Current HP", customCurrentHP);
        customAttack = EditorGUILayout.FloatField("Attack", customAttack);
        customDefense = EditorGUILayout.FloatField("Defense", customDefense);
        customSpeed = EditorGUILayout.FloatField("Speed", customSpeed);
        customMagic = EditorGUILayout.FloatField("Magic", customMagic);
        customMagicDefense = EditorGUILayout.FloatField("Magic Defense", customMagicDefense);
    }

    private void LoadSpeciesDefaults()
    {
        if (selectedSpecies == null) return;

        customNickname = selectedSpecies.speciesName;
        customIcon = selectedSpecies.speciesIcon;
        customModel = selectedSpecies.speciesModels != null && selectedSpecies.speciesModels.Count > 0
            ? selectedSpecies.speciesModels[0]
            : null;
        customMaxHP = selectedSpecies.baseHP;
        customCurrentHP = selectedSpecies.baseHP;
        customAttack = selectedSpecies.baseAttack;
        customDefense = selectedSpecies.baseDefense;
        customSpeed = selectedSpecies.baseSpeed;
        customMagic = selectedSpecies.baseMagic;
        customMagicDefense = selectedSpecies.baseMagicDefense;
    }

    private void InjectRunicToSave()
    {
        if (SaveManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "SaveManager was not found in the active scene. Enter Play mode first.", "OK");
            return;
        }

        SaveDataContainer saveData = SaveManager.Instance.LoadGame(targetSaveFile);
        RunicSaveData newRunic = CreateRunicData();

        if (addToParty)
        {
            saveData.partyIds.Add(newRunic.runicInstanceId);
        }
        
        saveData.boxStorage.Add(newRunic);

        // Save the updated data.
        SaveManager.Instance.SaveGame(targetSaveFile, saveData);
        EditorUtility.DisplayDialog("Success", $"Runic species '{selectedSpecies.speciesName}' was injected into save '{targetSaveFile}'.", "OK");
    }

    private void InjectRunicToRuntimeBox()
    {
        if (RunicStorageManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "RunicStorageManager was not found in the active scene. Enter Play mode first.", "OK");
            return;
        }

        RunicSaveData newRunic = CreateRunicData();

        RunicStorageManager.Instance.AddCapturedRunic(newRunic);
        EditorUtility.DisplayDialog("Success", $"Runic species '{selectedSpecies.speciesName}' was injected into the runtime box.", "OK");
    }

    private RunicSaveData CreateRunicData()
    {
        float defaultHP = selectedSpecies.baseHP;
        List<string> elementIds = new List<string>();
        List<string> basicSkillIds = new List<string>();
        List<string> skillIds = new List<string>();

        if (selectedSpecies.elements != null)
        {
            foreach (Element element in selectedSpecies.elements)
            {
                if (element != null) elementIds.Add(element.elementId);
            }
        }

        if (selectedSpecies.basicSkills != null)
        {
            foreach (Skill skill in selectedSpecies.basicSkills)
            {
                if (skill != null) basicSkillIds.Add(skill.skillId);
            }
        }

        if (selectedSpecies.skills != null)
        {
            foreach (Skill skill in selectedSpecies.skills)
            {
                if (skill != null) skillIds.Add(skill.skillId);
            }
        }

        return new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = selectedSpecies.speciesId,
            nickname = useCustomValues && !string.IsNullOrWhiteSpace(customNickname) ? customNickname : selectedSpecies.speciesName,
            modelIndex = useCustomValues && customModel != null && selectedSpecies.speciesModels != null
                ? Mathf.Max(0, selectedSpecies.speciesModels.IndexOf(customModel))
                : 0,
            level = useCustomValues ? level : 1,
            experience = useCustomValues ? experience : 0,
            currentHP = useCustomValues ? customCurrentHP : defaultHP,
            maxHP = useCustomValues ? customMaxHP : defaultHP,
            attack = useCustomValues ? customAttack : selectedSpecies.baseAttack,
            defense = useCustomValues ? customDefense : selectedSpecies.baseDefense,
            speed = useCustomValues ? customSpeed : selectedSpecies.baseSpeed,
            magic = useCustomValues ? customMagic : selectedSpecies.baseMagic,
            magicDefense = useCustomValues ? customMagicDefense : selectedSpecies.baseMagicDefense,
            elementIds = elementIds,
            learnedBasicSkillIds = basicSkillIds,
            learnedSkillIds = skillIds
        };
    }
}