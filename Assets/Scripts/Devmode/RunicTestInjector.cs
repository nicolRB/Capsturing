using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RunicTestInjector : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Enables test injection in the Unity Editor and development builds.")]
    [SerializeField] private bool devMode = true;

    [Tooltip("Species used when creating the test runic.")]
    [SerializeField] private RunicSpecies defaultTestSpecies;

    [Tooltip("Shortcut displayed in debug messages for the injection action.")]
    [SerializeField] private Key injectShortcutKey = Key.F12;
    
    [Tooltip("Pick the specific stats for the runic instead of using the species base stats.")]
    [SerializeField] private bool useCustomStats;

    [Header("Custom Basic Stats")]
    [SerializeField] private string nickname;
    [SerializeField] private int level;
    [SerializeField] private int hp;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int magic;
    [SerializeField] private int magicDefense;
    [SerializeField] private float weight;
    [SerializeField] private float knockbackResistance;
    [SerializeField] private float speed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private List<Element> elements;

    [Header("Custom Skills")]
    [SerializeField] private List<Skill> basicSkills;
    [SerializeField] private List<Skill> skills;
    
    [Header("Custom Behavior")]

    [Header("References")]
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private RunicStorageManager runicStorageManager;
    [SerializeField] private RunicDatabase runicDatabase;

    void Start()
    {
        if (saveManager == null) saveManager = FindFirstObjectByType<SaveManager>();

        if (runicStorageManager == null) runicStorageManager = FindFirstObjectByType<RunicStorageManager>();
    }
    
    void Update()
    {
        // Restrict test injection to the Editor and development builds.
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!devMode) return;

        if (Keyboard.current == null) return;

        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
        {
            InjectDebugRunic();
        }
        #endif
    }

    public void InjectDebugRunic()
    {
        if (defaultTestSpecies == null)
        {
            Debug.LogWarning("RunicTestInjector: Default Test Species is not assigned!");
            return;
        }

        if (runicDatabase == null)
        {
            Debug.LogWarning("RunicTestInjector: Runic Database is not assigned!");
            return;
        }

        RunicSpecies species = runicDatabase.GetSpeciesById(defaultTestSpecies.SpeciesId);
        if (species == null)
        {
            Debug.LogWarning($"RunicTestInjector: Species '{defaultTestSpecies.SpeciesId}' is not registered in the Runic Database.");
            return;
        }
        
        if (runicStorageManager == null || saveManager == null)
        {
            Debug.LogWarning("RunicTestInjector: storage or save manager not found in the scene.");
            return;
        }

        List<string> basicSkillIds = GetRegisteredSkillIds(species.BasicSkills);
        List<string> skillIds = GetRegisteredSkillIds(species.Skills);
        List<string> elementIds = GetRegisteredElementIds(species.Elements);

        RunicSaveData debugRunic = new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = species.SpeciesId,
            nickname = "Test Runic",
            runtimeIcon = species.SpeciesIcon,
            runtimeModel = species.SpeciesModels != null && species.SpeciesModels.Count > 0
                ? species.SpeciesModels[0]
                : null,
            modelIndex = 0,
            width = species.Width,
            height = species.Height,
            level = 1,
            currentHP = species.BaseHP,
            maxHP = species.BaseHP,
            attack = species.BaseAttack,
            defense = species.BaseDefense,
            weight = species.BaseWeight,
            knockbackResistance = species.BaseKnockbackResistance,
            speed = species.BaseSpeed,
            walkSpeed = species.BaseWalkSpeed,
            sprintSpeed = species.BaseSprintSpeed,
            magic = species.BaseMagic,
            magicDefense = species.BaseMagicDefense,
            recovery = species.Recovery,
            specialRecovery = species.SpecialRecovery,
            aggression = species.Aggression,
            specialAggression = species.Specialaggression,
            learnedBasicSkillIds = basicSkillIds,
            learnedSkillIds = skillIds,
            elementIds = elementIds,
            combatStyle = species.DefaultCombatStyle
        };

        // Add the generated runic directly to the current runtime box.
        RunicStorageManager.Instance.AddCapturedRunic(debugRunic);
        Debug.Log("Test runic injected with key " + injectShortcutKey + ".");
    }

    private List<string> GetRegisteredSkillIds(List<Skill> speciesSkills)
    {
        List<string> skillIds = new List<string>();
        if (speciesSkills == null) return skillIds;

        foreach (Skill skill in speciesSkills)
        {
            if (skill == null) continue;

            Skill registeredSkill = runicDatabase.GetSkillById(skill.SkillId);
            if (registeredSkill == null)
            {
                Debug.LogWarning($"RunicTestInjector: Skill '{skill.SkillId}' is not registered in the Runic Database.");
                continue;
            }

            skillIds.Add(registeredSkill.SkillId);
        }

        return skillIds;
    }

    private List<string> GetRegisteredElementIds(List<Element> speciesElements)
    {
        List<string> elementIds = new List<string>();
        if (speciesElements == null) return elementIds;

        foreach (Element element in speciesElements)
        {
            if (element == null) continue;

            Element registeredElement = runicDatabase.GetElementById(element.ElementId);
            if (registeredElement == null)
            {
                Debug.LogWarning($"RunicTestInjector: Element '{element.ElementId}' is not registered in the Runic Database.");
                continue;
            }

            elementIds.Add(registeredElement.ElementId);
        }

        return elementIds;
    }
}