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
    private float customWidth;
    private float customHeight;
    private int customMaxHP;
    private int customCurrentHP;
    private float customAttack;
    private float customDefense;
    private float customWeight;
    private float customKnockbackResistance;
    private float customSpeed;
    private float customWalkSpeed;
    private float customSprintSpeed;
    private float customMagic;
    private float customMagicDefense;
    private float customRecovery;
    private float customSpecialRecovery;
    private float customaggression;
    private float customSpecialaggression;
    private List<Skill> customBasicSkills;
    private List<Skill> customSkills;
    private List<Element> customElements;
    private CombatStyle customCombatStyle = CombatStyle.FaceOn;
    private RunicSpecies lastSelectedSpecies;
    private Vector2 scrollPosition;

    [MenuItem("Ingame Dev Tools/Runic/Runic Injector")]
    public static void ShowWindow()
    {
        GetWindow<RunicInjectorWindow>("Runic Injector");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

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
            EditorGUILayout.HelpBox("The icon, model, stats, Elements, and Skills will be copied from the species. Level will be 1 and XP will be 0.", MessageType.Info);
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

        EditorGUILayout.EndScrollView();
    }

    private void DrawCustomFields()
    {
        customNickname = EditorGUILayout.TextField("Nickname (Optional)", customNickname);
        level = Mathf.Max(1, EditorGUILayout.IntField("Level", level));
        experience = Mathf.Max(0, EditorGUILayout.IntField("Experience", experience));

        EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
        customIcon = EditorGUILayout.ObjectField("Icon", customIcon, typeof(Sprite), false) as Sprite;
        customModel = EditorGUILayout.ObjectField("Model", customModel, typeof(GameObject), false) as GameObject;

        EditorGUILayout.LabelField("Sizes", EditorStyles.boldLabel);
        customWidth = EditorGUILayout.FloatField("Width", customWidth);
        customHeight = EditorGUILayout.FloatField("Height", customHeight);

        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        customMaxHP = EditorGUILayout.IntField("Max HP", customMaxHP);
        customCurrentHP = EditorGUILayout.IntField("Current HP", customCurrentHP);
        customAttack = EditorGUILayout.FloatField("Attack", customAttack);
        customDefense = EditorGUILayout.FloatField("Defense", customDefense);
        customSpeed = EditorGUILayout.FloatField("Normal Speed", customSpeed);
        customWalkSpeed = EditorGUILayout.FloatField("Walk Speed", customSpeed);
        customSprintSpeed = EditorGUILayout.FloatField("Sprint Speed", customSpeed);
        customMagic = EditorGUILayout.FloatField("Magic", customMagic);
        customMagicDefense = EditorGUILayout.FloatField("Magic Defense", customMagicDefense);
        customRecovery = EditorGUILayout.FloatField("Recovery", customRecovery);
        customSpecialRecovery = EditorGUILayout.FloatField("Special Recovery", customSpecialRecovery);
        customaggression = EditorGUILayout.FloatField("aggression", customaggression);
        customSpecialaggression = EditorGUILayout.FloatField("Special aggression", customSpecialaggression);

        customElements = DrawAssetList("Elements", customElements);
        customBasicSkills = DrawAssetList("Basic Skills", customBasicSkills);
        customSkills = DrawAssetList("Skills", customSkills);

        EditorGUILayout.LabelField("Combat Settings", EditorStyles.boldLabel);
        customCombatStyle = (CombatStyle)EditorGUILayout.EnumPopup("Combat Style", customCombatStyle);
    }

    private List<T> DrawAssetList<T>(string label, List<T> items) where T : Object
    {
        if (items == null) items = new List<T>();

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        for (int i = 0; i < items.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            items[i] = (T)EditorGUILayout.ObjectField(items[i], typeof(T), false);
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                items.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add " + label)) items.Add(null);
        return items;
    }

    private void LoadSpeciesDefaults()
    {
        if (selectedSpecies == null) return;

        customNickname = selectedSpecies.SpeciesName;
        customIcon = selectedSpecies.SpeciesIcon;
        customModel = selectedSpecies.SpeciesModels != null && selectedSpecies.SpeciesModels.Count > 0
            ? selectedSpecies.SpeciesModels[0]
            : null;
        customWidth = selectedSpecies.Width;
        customHeight = selectedSpecies.Height;
        customMaxHP = selectedSpecies.BaseHP;
        customCurrentHP = selectedSpecies.BaseHP;
        customAttack = selectedSpecies.BaseAttack;
        customDefense = selectedSpecies.BaseDefense;
        customSpeed = selectedSpecies.BaseSpeed;
        customWalkSpeed = selectedSpecies.BaseWalkSpeed;
        customSprintSpeed = selectedSpecies.BaseSprintSpeed;
        customMagic = selectedSpecies.BaseMagic;
        customMagicDefense = selectedSpecies.BaseMagicDefense;
        customRecovery = selectedSpecies.Recovery;
        customSpecialRecovery = selectedSpecies.SpecialRecovery;
        customaggression = selectedSpecies.Aggression;
        customSpecialaggression = selectedSpecies.Specialaggression;
        customElements = selectedSpecies.Elements != null
            ? new List<Element>(selectedSpecies.Elements)
            : new List<Element>();
        customBasicSkills = selectedSpecies.BasicSkills != null
            ? new List<Skill>(selectedSpecies.BasicSkills)
            : new List<Skill>();
        customSkills = selectedSpecies.Skills != null
            ? new List<Skill>(selectedSpecies.Skills)
            : new List<Skill>();
        customCombatStyle = selectedSpecies.DefaultCombatStyle;
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
        EditorUtility.DisplayDialog("Success", $"Runic species '{selectedSpecies.SpeciesName}' was injected into save '{targetSaveFile}'.", "OK");
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
        EditorUtility.DisplayDialog("Success", $"Runic species '{selectedSpecies.SpeciesName}' was injected into the runtime box.", "OK");
    }

    private RunicSaveData CreateRunicData()
    {
        int defaultHP = selectedSpecies.BaseHP;
        List<string> elementIds = new List<string>();
        List<string> basicSkillIds = new List<string>();
        List<string> skillIds = new List<string>();
        List<Element> elementsToSave = useCustomValues ? customElements : selectedSpecies.Elements;
        List<Skill> basicSkillsToSave = useCustomValues ? customBasicSkills : selectedSpecies.BasicSkills;
        List<Skill> skillsToSave = useCustomValues ? customSkills : selectedSpecies.Skills;

        if (elementsToSave != null)
        {
            foreach (Element element in elementsToSave)
            {
                if (element != null) elementIds.Add(element.ElementId);
            }
        }

        if (basicSkillsToSave != null)
        {
            foreach (Skill skill in basicSkillsToSave)
            {
                if (skill != null) basicSkillIds.Add(skill.SkillId);
            }
        }

        if (skillsToSave != null)
        {
            foreach (Skill skill in skillsToSave)
            {
                if (skill != null) skillIds.Add(skill.SkillId);
            }
        }

        return new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = selectedSpecies.SpeciesId,
            nickname = useCustomValues && !string.IsNullOrWhiteSpace(customNickname) ? customNickname : selectedSpecies.SpeciesName,
            runtimeIcon = useCustomValues && customIcon != null ? customIcon : selectedSpecies.SpeciesIcon,
            runtimeModel = useCustomValues && customModel != null ? customModel : GetDefaultModel(),
            modelIndex = useCustomValues && customModel != null && selectedSpecies.SpeciesModels != null
                ? Mathf.Max(0, selectedSpecies.SpeciesModels.IndexOf(customModel))
                : 0,
            width  = useCustomValues ? customWidth : 1,
            height = useCustomValues ? customHeight : 1,
            level = useCustomValues ? level : 1,
            experience = useCustomValues ? experience : 0,
            currentHP = useCustomValues ? customCurrentHP : defaultHP,
            maxHP = useCustomValues ? customMaxHP : defaultHP,
            attack = useCustomValues ? customAttack : selectedSpecies.BaseAttack,
            defense = useCustomValues ? customDefense : selectedSpecies.BaseDefense,
            weight = useCustomValues ? customWeight : selectedSpecies.BaseWeight,
            knockbackResistance = useCustomValues ? customKnockbackResistance : selectedSpecies.BaseKnockbackResistance,
            speed = useCustomValues ? customSpeed : selectedSpecies.BaseSpeed,
            walkSpeed = useCustomValues ? customWalkSpeed : selectedSpecies.BaseWalkSpeed,
            sprintSpeed = useCustomValues ? customSprintSpeed : selectedSpecies.BaseSprintSpeed,
            magic = useCustomValues ? customMagic : selectedSpecies.BaseMagic,
            magicDefense = useCustomValues ? customMagicDefense : selectedSpecies.BaseMagicDefense,
            recovery = useCustomValues ? customRecovery : selectedSpecies.Recovery,
            specialRecovery = useCustomValues ? customSpecialRecovery : selectedSpecies.SpecialRecovery,
            aggression = useCustomValues ? customaggression : selectedSpecies.Aggression,
            specialAggression = useCustomValues ? customSpecialaggression : selectedSpecies.Specialaggression,
            elementIds = elementIds,
            learnedBasicSkillIds = basicSkillIds,
            learnedSkillIds = skillIds,
            combatStyle = customCombatStyle
        };
    }

    private GameObject GetDefaultModel()
    {
        return selectedSpecies.SpeciesModels != null && selectedSpecies.SpeciesModels.Count > 0
            ? selectedSpecies.SpeciesModels[0]
            : null;
    }
}