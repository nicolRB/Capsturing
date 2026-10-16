using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunicSkillsUI : MonoBehaviour
{
    [Header("Skill Icons Settings")]
    [SerializeField] private int maxSkills = 3;

    [Header("Cooldown Visuals")]
    [SerializeField] private Color cooldownTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("References")]
    [SerializeField] private RunicCommands runicCommands;
    [SerializeField] private GameObject skillIconPrefab;

    private List<GameObject> skillIcons = new List<GameObject>(); 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        runicCommands = FindFirstObjectByType<RunicCommands>();
        if (runicCommands == null)
            Debug.LogWarning("Runic Skills UI: Missing Runic Commands");

        if (skillIconPrefab == null)
            Debug.LogWarning("Runic Skills UI: Missing Skill Icon Prefab");
    }

    // Update is called once per frame
    void Update()
    {
        if (skillIcons.Count > 0 && runicCommands.SummonedRunic != null)
            UpdateSkillIcons();
    }

    private void UpdateSkillIcons()
    {
        int i = 0;
        foreach (GameObject skillIcon in skillIcons)
        {
            Image skillIconImage = skillIcon.transform.Find("IconMask/IconSprite")?
                .GetComponent<Image>();

            TextMeshProUGUI cooldownTimer = skillIcon.transform.Find("CooldownTimer")
                .GetComponent<TextMeshProUGUI>();

            if (skillIconImage != null)
            {
                skillIconImage.sprite = runicCommands.SummonedRunic.Skills[i].SkillSprite;
            }

            Skill skill = runicCommands.SummonedRunic.Skills[i];
            float skillCooldown = runicCommands.SummonedRunic.Combat.SkillCooldowns[skill];

            if (skillCooldown > 0)
            {
                if (skillIconImage != null)
                    skillIconImage.color = cooldownTint;

                if (cooldownTimer != null)
                    cooldownTimer.text = skillCooldown.ToString("0");
            }
            else
            {
                if (skillIconImage != null)
                    skillIconImage.color = Color.white;

                if (cooldownTimer != null)
                    cooldownTimer.text = "";
            }

            i++;
        }
    }

    public void BuildSkillIcons()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        skillIcons.Clear();

        if (runicCommands.SummonedRunic != null)
        {
            int skillAmount = Mathf.Min(runicCommands.SummonedRunic.Skills.Count, maxSkills);
            for (int i = 0; i < skillAmount; i++)
            {
                GameObject skillIcon = Instantiate(skillIconPrefab, transform);
                
                Image skillIconImage = skillIcon.transform.Find("IconMask/IconSprite")?
                    .GetComponent<Image>();

                if (skillIconImage != null)
                {
                    skillIconImage.sprite = runicCommands.SummonedRunic.Skills[i].SkillSprite;
                }
                skillIcons.Add(skillIcon);
            }
        }
    }
}
