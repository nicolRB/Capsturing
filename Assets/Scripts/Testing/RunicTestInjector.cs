using UnityEngine;
using UnityEngine.InputSystem;

public class RunicTestInjector : MonoBehaviour
{
    public bool devMode = true;
    public RunicSpecies defaultTestSpecies;
    public Key injectShortcutKey = Key.F12;
    
    void Update()
    {
        // Só funciona se o devMode estiver ligado E estivermos rodando no Editor ou build de desenvolvimento
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!devMode) return;

        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
        {
            InjectDebugRunic();
        }
        #endif
    }

    public void InjectDebugRunic()
    {
        if (RunicStorageManager.Instance == null || SaveManager.Instance == null)
        {
            Debug.LogWarning("Gerenciadores de Storage ou Save não encontrados na cena.");
            return;
        }

        RunicSaveData debugRunic = new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = defaultTestSpecies.speciesId,
            nickname = "Rúnico de Teste",
            runicIcon = defaultTestSpecies.speciesIcon,
            runicModel = defaultTestSpecies.speciesModels.Count > 0 ? defaultTestSpecies.speciesModels[0] : null,
            level = 1,
            currentHP = defaultTestSpecies.baseHP,
            maxHP = defaultTestSpecies.baseHP,
            attack = defaultTestSpecies.baseAttack,
            defense = defaultTestSpecies.baseDefense,
            speed = defaultTestSpecies.baseSpeed,
            magic = defaultTestSpecies.baseMagic,
            magicDefense = defaultTestSpecies.baseMagicDefense
        };

        // Adiciona direto na box do storage atual
        RunicStorageManager.Instance.AddCapturedRunic(debugRunic);
        Debug.Log("Rúnico de teste injetado via tecla " + injectShortcutKey.ToString() + "!");
    }
}