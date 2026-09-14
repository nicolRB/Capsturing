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

    [MenuItem("Runicos/Injetor de Rúnicos")]
    public static void ShowWindow()
    {
        GetWindow<RunicInjectorWindow>("Injetor de Rúnicos");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ferramenta de Injeção de Rúnicos", EditorStyles.boldLabel);

        targetSaveFile = EditorGUILayout.TextField("Nome do Save", targetSaveFile);
        selectedSpecies = EditorGUILayout.ObjectField("Espécie", selectedSpecies, typeof(RunicSpecies), false) as RunicSpecies;
        if (selectedSpecies != lastSelectedSpecies)
        {
            lastSelectedSpecies = selectedSpecies;
            LoadSpeciesDefaults();
        }
        useCustomValues = EditorGUILayout.Toggle("Usar valores customizados", useCustomValues);

        if (useCustomValues)
        {
            DrawCustomFields();
        }
        else
        {
            EditorGUILayout.HelpBox("Ícone, modelo, atributos, elementos e habilidades serão copiados da espécie. O nível será 1 e o XP será 0.", MessageType.Info);
        }

        addToParty = EditorGUILayout.Toggle("Adicionar à Party", addToParty);

        using (new EditorGUI.DisabledScope(selectedSpecies == null))
        {
            if (GUILayout.Button("Injetar Rúnico no Save"))
            {
                InjectRunicToSave();
            }

            if (GUILayout.Button("Injetar Rúnico na Box de Runtime"))
            {
                InjectRunicToRuntimeBox();
            }
        }
    }

    private void DrawCustomFields()
    {
        customNickname = EditorGUILayout.TextField("Apelido (Opcional)", customNickname);
        level = Mathf.Max(1, EditorGUILayout.IntField("Nível", level));
        experience = Mathf.Max(0, EditorGUILayout.IntField("Experiência", experience));

        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        customIcon = EditorGUILayout.ObjectField("Ícone", customIcon, typeof(Sprite), false) as Sprite;
        customModel = EditorGUILayout.ObjectField("Modelo", customModel, typeof(GameObject), false) as GameObject;

        EditorGUILayout.LabelField("Atributos", EditorStyles.boldLabel);
        customMaxHP = EditorGUILayout.FloatField("HP Máximo", customMaxHP);
        customCurrentHP = EditorGUILayout.FloatField("HP Atual", customCurrentHP);
        customAttack = EditorGUILayout.FloatField("Ataque", customAttack);
        customDefense = EditorGUILayout.FloatField("Defesa", customDefense);
        customSpeed = EditorGUILayout.FloatField("Velocidade", customSpeed);
        customMagic = EditorGUILayout.FloatField("Magia", customMagic);
        customMagicDefense = EditorGUILayout.FloatField("Defesa Mágica", customMagicDefense);
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
            EditorUtility.DisplayDialog("Erro", "O SaveManager não foi encontrado na cena ativa. Entre no modo Play primeiro.", "OK");
            return;
        }

        SaveDataContainer saveData = SaveManager.Instance.LoadGame(targetSaveFile);
        RunicSaveData newRunic = CreateRunicData();

        if (addToParty)
        {
            saveData.partyIds.Add(newRunic.runicInstanceId);
        }
        
        saveData.boxStorage.Add(newRunic);

        // Salva de volta
        SaveManager.Instance.SaveGame(targetSaveFile, saveData);
        EditorUtility.DisplayDialog("Sucesso", $"Rúnico da espécie '{selectedSpecies.speciesName}' injetado com sucesso no save '{targetSaveFile}'!", "OK");
    }

    private void InjectRunicToRuntimeBox()
    {
        if (RunicStorageManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Erro", "O RunicStorageManager não foi encontrado na cena ativa. Entre no modo Play primeiro.", "OK");
            return;
        }

        RunicSaveData newRunic = CreateRunicData();

        RunicStorageManager.Instance.AddCapturedRunic(newRunic);
        EditorUtility.DisplayDialog("Sucesso", $"Rúnico da espécie '{selectedSpecies.speciesName}' injetado com sucesso na box de runtime!", "OK");
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
            runicIcon = useCustomValues && customIcon != null ? customIcon : selectedSpecies.speciesIcon,
            runicModel = useCustomValues && customModel != null
                ? customModel
                : selectedSpecies.speciesModels != null && selectedSpecies.speciesModels.Count > 0 ? selectedSpecies.speciesModels[0] : null,
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