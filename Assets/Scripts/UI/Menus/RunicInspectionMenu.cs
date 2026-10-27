using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RunicInspectionMenu : MonoBehaviour
{
    const int maxSkillCount = 3;

    [Header("Data")]
    [SerializeField] private RunicSpecies species;
    [SerializeField] private Sprite runicSprite;
    [SerializeField] private Sprite runeSprite;

    [Header("Stat/Info Fields")]
    [SerializeField] private Image runicIcon;
    [SerializeField] private Image runicIconBg;
    [SerializeField] private Image runeIcon;
    [SerializeField] private TextMeshProUGUI nameField;
    [SerializeField] private TextMeshProUGUI speciesField;
    [SerializeField] private TextMeshProUGUI levelField;
    [SerializeField] private TextMeshProUGUI experienceField;
    [SerializeField] private TextMeshProUGUI NextLevelExpField;
    [SerializeField] private TextMeshProUGUI hpField;
    [SerializeField] private TextMeshProUGUI attackField;
    [SerializeField] private TextMeshProUGUI defenseField;
    [SerializeField] private TextMeshProUGUI magicField;
    [SerializeField] private TextMeshProUGUI magicDefenseField;
    [SerializeField] private TextMeshProUGUI speedsField;
    [SerializeField] private TextMeshProUGUI weightField;
    [SerializeField] private TextMeshProUGUI combatStyleField;

    [Header("Other Stats Fields")]
    [SerializeField] private TextMeshProUGUI knockbackResField;
    [SerializeField] private TextMeshProUGUI recoveryField;
    [SerializeField] private TextMeshProUGUI specialRecoveryField;
    [SerializeField] private TextMeshProUGUI aggressionField;
    [SerializeField] private TextMeshProUGUI specialAggressionField;

    [Header("SkillFields")]
    [SerializeField] private List<Skill> skills = new List<Skill>();
    [SerializeField] private List<Skill> basicSkills = new List<Skill>();
    [SerializeField] private Transform basicSkillsList;
    [SerializeField] private List<Transform> SkillBlocks = new List<Transform>();

    [Header("Elements")]
    [SerializeField] private List<Element> elements;
    [SerializeField] private Transform elementsList;
    
    [Header("References")]
    [SerializeField] private RunicDatabase database;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject elementTagPrefab;
    [SerializeField] private GameObject basicSkillPrefab;

    void Start()
    {
        if (menuManager == null)
            menuManager = FindFirstObjectByType<MenuManager>();

        if (RunicStorageManager.Instance == null)
            Debug.LogWarning("Runic Inspection Menu: Storage Manager missing / not referenced");

        if (menuManager == null)
            Debug.LogWarning("Runic Inspection Menu: Menu Manager missing / not referenced");

        if (database == null)
            Debug.LogWarning("Runic Inspection Menu: Runic Database missing / not referenced");
    }

    public void SetupPage(RunicSaveData data)
    {
        if (data != null && RunicStorageManager.Instance && database != null)
        {
            species = database.GetSpeciesById(data.speciesId);
            runicSprite = RunicStorageManager.Instance.GetRunicIcon(data);
            // runeSprite = storageManage.GetRuneFromRunic(data);
            
            skills = database.GetSkillsByIds(data.learnedSkillIds);
            basicSkills = database.GetSkillsByIds(data.learnedBasicSkillIds);

            elements = database.GetElementsByIds(data.elementIds);

            runicIcon.sprite = runicSprite;
            runicIcon.color = runicSprite != null ? Color.white : Color.black;

            if (runicIconBg != null)
                runicIconBg.color = runicSprite != null ? Color.white : new Color(0,0,0,0);

            string speciesName = species != null && !string.IsNullOrEmpty(species.SpeciesName)
                ? species.SpeciesName : "?";
            bool hasNickname = !string.IsNullOrEmpty(data.nickname);

            nameField.text = hasNickname ? data.nickname : speciesName;

            speciesField.text = speciesName;
            speciesField.gameObject.SetActive(hasNickname);

            levelField.text = $"Lvl {data.level}";
            experienceField.text = data.experience.ToString();
            hpField.text = data.currentHP.ToString() + " / " + data.maxHP.ToString();
            attackField.text = data.attack.ToString();
            defenseField.text = data.defense.ToString();
            magicField.text = data.magic.ToString();
            magicDefenseField.text = data.magicDefense.ToString();
            speedsField.text = data.walkSpeed.ToString() + " / " + data.speed.ToString() + " / " 
                + data.sprintSpeed.ToString();
            weightField.text = data.weight.ToString("0.00") + " Kg";
            combatStyleField.text = data.combatStyle.ToString();

            // More Stats
            knockbackResField.text = data.knockbackResistance.ToString("0.0");
            recoveryField.text = data.recovery.ToString("0.0") + "s";
            specialRecoveryField.text = data.specialRecovery.ToString("0.0") + "s";
            aggressionField.text = (data.aggression * 100).ToString("0");
            specialAggressionField.text = (data.specialAggression * 100).ToString("0");

            SetUpBasicSkills();

            SetUpSkills();

            SetUpElements();
        }
        else
            Debug.LogError("Inspection Page needs RunicStorageManager, RunicDatabase and RunicSaveData to setup.");
    }

    void SetUpBasicSkills()
    {
        // Clears out the children
        foreach (Transform child in basicSkillsList)
        {
            Destroy(child.gameObject);
        }

        // Instantiates the new basic skills in the Scroll View Port Content
        foreach (Skill basicSkill in basicSkills)
        {
            GameObject skillLine = Instantiate(basicSkillPrefab, basicSkillsList);
            
            // Ensures that the local scale is not distorted when instantiating within layouts.
            skillLine.transform.localScale = Vector3.one;

            // Fill in the text fields.
            skillLine.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = basicSkill.SkillName;
            skillLine.transform.Find("Power").GetComponent<TextMeshProUGUI>().text = basicSkill.Power.ToString("0.0");
            skillLine.transform.Find("Cooldown").GetComponent<TextMeshProUGUI>().text = basicSkill.Cooldown.ToString("0.0");
            skillLine.transform.Find("Range").GetComponent<TextMeshProUGUI>().text = basicSkill.Range.ToString("0.0");
        }
    }

    void SetUpSkills()
    {
        for (int i = 0; i < SkillBlocks.Count; i++)
        {
            Transform block = SkillBlocks[i];
            Skill skill = i < skills.Count ? skills[i] : null;

            foreach (Transform child in block)
                child.gameObject.SetActive(skill != null);

            if (skill == null) continue;

            // false até o sistema de troca de skills existir
            var switchButton = block.Find("SwitchSkillButton");
            if (switchButton != null) switchButton.gameObject.SetActive(false);

            SetText(block, "SkillName", skill.SkillName);
            SetText(block, "FieldNames/TypeField/FieldValue", SkillDisplay.GetSkillTypeLabel(skill));
            SetText(block, "FieldNames/PowerField/FieldValue", skill.Power.ToString("0.0"));
            SetText(block, "FieldNames/CooldownField/FieldValue", skill.Cooldown.ToString("0.0"));
            SetText(block, "FieldNames/RangeField/FieldValue", skill.Range.ToString("0.0"));

            Transform tag = block.Find("ElementTag");
            if (tag == null) continue;

            Element element = skill.SkillElement;
            tag.gameObject.SetActive(element != null);
            if (element == null) continue;

            tag.GetComponent<Image>().color = element.ElementColor;
            var label = tag.Find("ElementName").GetComponent<TextMeshProUGUI>();
            label.text = element.ElementName;
            label.color = element.ElementColor;
        }
    }

    static void SetText(Transform root, string path, string value)
    {
        Transform t = root.Find(path);
        if (t != null) t.GetComponent<TextMeshProUGUI>().text = value;
    }

    void SetUpElements()
    {
        foreach (Transform tag in elementsList)
        {
            // Clears out the old tags
            Destroy(tag.gameObject);
        }

        foreach (Element element in elements)
        {
            var tag = Instantiate(elementTagPrefab, elementsList)
                .GetComponent<Image>();
            tag.color = element.ElementColor;

            var label = tag.transform.Find("ElementName").GetComponent<TextMeshProUGUI>();
            label.text = element.ElementName;
            label.color = element.ElementColor;
        }
    }
}
