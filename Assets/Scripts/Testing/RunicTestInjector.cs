using UnityEngine;
using UnityEngine.InputSystem;

public class RunicTestInjector : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Enables test injection in the Unity Editor and development builds.")]
    [SerializeField]
    private bool devMode = true;
    [Tooltip("Species used when creating the test runic.")]
    [SerializeField]
    private RunicSpecies defaultTestSpecies;
    [Tooltip("Shortcut displayed in debug messages for the injection action.")]
    [SerializeField]
    private Key injectShortcutKey = Key.F12;
    
    void Update()
    {
        // Restrict test injection to the Editor and development builds.
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
            Debug.LogWarning("RunicTestInjector: storage or save manager not found in the scene.");
            return;
        }

        RunicSaveData debugRunic = new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = defaultTestSpecies.SpeciesId,
            nickname = "Test Runic",
            modelIndex = 0,
            level = 1,
            currentHP = defaultTestSpecies.BaseHP,
            maxHP = defaultTestSpecies.BaseHP,
            attack = defaultTestSpecies.BaseAttack,
            defense = defaultTestSpecies.BaseDefense,
            speed = defaultTestSpecies.BaseSpeed,
            magic = defaultTestSpecies.BaseMagic,
            magicDefense = defaultTestSpecies.BaseMagicDefense
        };

        // Add the generated runic directly to the current runtime box.
        RunicStorageManager.Instance.AddCapturedRunic(debugRunic);
        Debug.Log("Test runic injected with key " + injectShortcutKey + ".");
    }
}