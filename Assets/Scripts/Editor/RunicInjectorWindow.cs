using UnityEngine;
using UnityEditor;

public class RunicInjectorWindow : EditorWindow
{
    private string targetSaveFile = "save_1";
    private RunicSpecies selectedSpecies;
    private string customNickname = "";
    private int level = 1;
    private bool addToParty = false;

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
        customNickname = EditorGUILayout.TextField("Apelido (Opcional)", customNickname);
        level = EditorGUILayout.IntField("Nível", level);
        addToParty = EditorGUILayout.Toggle("Adicionar à Party", addToParty);

        if (GUILayout.Button("Injetar Rúnico no Save"))
        {
            InjectRunicToSave();
        }

        if (GUILayout.Button("Injetar Rúnico na Box de Runtime"))
        {
            InjectRunicToRuntimeBox();
        }
    }

    private void InjectRunicToSave()
    {
        if (SaveManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Erro", "O SaveManager não foi encontrado na cena ativa. Entre no modo Play primeiro.", "OK");
            return;
        }

        // Carrega os dados atuais
        SaveDataContainer saveData = SaveManager.Instance.LoadGame(targetSaveFile);

        // Cria o rúnico de save básico
        RunicSaveData newRunic = new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = selectedSpecies.speciesId,
            nickname = customNickname,
            level = level,
            currentHP = 100f * level,
            maxHP = 100f * level,
            attack = 10f * level,
            defense = 10f * level,
            speed = 10f,
            magic = 10f * level,
            magicDefense = 10f * level
        };

        if (addToParty)
        {
            saveData.partyIds.Add(newRunic.runicInstanceId);
            // Nota: Dependendo de como seu RunicStorageManager guarda os dados, 
            // certifique-se de onde a lista de rúnicos totais fica armazenada.
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

        // Cria o rúnico de runtime básico
        RunicSaveData newRunic = new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = selectedSpecies.speciesId,
            nickname = customNickname,
            level = level,
            currentHP = 100f * level,
            maxHP = 100f * level,
            attack = 10f * level,
            defense = 10f * level,
            speed = 10f,
            magic = 10f * level,
            magicDefense = 10f * level
        };

        RunicStorageManager.Instance.AddCapturedRunic(newRunic);
        EditorUtility.DisplayDialog("Sucesso", $"Rúnico da espécie '{selectedSpecies.speciesName}' injetado com sucesso na box de runtime!", "OK");
    }
}