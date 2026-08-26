using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Microsoft.Unity.VisualStudio.Editor;

public class SpellSelectUI : MonoBehaviour
{
    [Header("Spell Selection")]
    private List<GameObject> spellIcons = new List<GameObject>();
    public float iconSpacing = 100f; // Spacing between spell icons
    public float iconSize = 80f; // Size of each spell icon
    public float iconScale = 0.91f; // Scale of each spell icon
    public float selectionIndicatorOffset = 10f; // Offset for the selection indicator
    private int spellCount = 0;

    [Header("Cooldown Visuals")]
    public Color cooldownTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("References")]
    public SpellcastingScript spellcastingScript;
    public GameObject spellIconPrefab; // Prefab for the spell icon UI element

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (spellcastingScript == null) spellcastingScript = FindFirstObjectByType<SpellcastingScript>();

        if (spellIconPrefab == null) Debug.LogError("SpellSelectUI: Spell icon prefab is not assigned.");
    }

    // Update is called once per frame
    void Update()
    {
        if (spellcastingScript != null)
        {
            UpdateSelectionIndicator(spellcastingScript.spellIndex);
            UpdateCoolDownTimers();
        }   
    }

    public void UpdateSpellList()
    {
        // Clear existing spell icons
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Create new spell icons based on the spells in the SpellcastingScript
        if (spellcastingScript.spells != null)
        {
            foreach (var spell in spellcastingScript.spells)
            {
                GameObject spellSelectIcon = Instantiate(spellIconPrefab, transform);
                // each spellSelectIcon has a child called "SpellIcon" with an Image component
                var spellIconImage = spellSelectIcon.transform.Find("SpellIcon").GetComponent<UnityEngine.UI.Image>();
                if (spellIconImage != null)
                {
                    spellIconImage.sprite = spell.spellIcon;
                }
                spellSelectIcon.transform.localScale = Vector3.one * iconScale;
                spellCount++;
            }
        }

        ArrangeSpellIcons();
    }

    // Method for positioning spells along a horizontal line, centered on the parent object
    public void ArrangeSpellIcons()
    {
        float totalWidth = (spellCount - 1) * iconSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < spellCount; i++)
        {
            Transform spellIconTransform = transform.GetChild(i);
            if (spellIconTransform != null)
            {
                spellIconTransform.localPosition = new Vector3(startX + i * iconSpacing, 0f, 0f);
            }
        }
    }

    // Method to update the selection indicator position based on the selected spell index in the SpellcastingScript
    // Moves the selected spell icon up by selectionIndicatorOffset units smoothly and returns the previously selected spell icon
    // to its original position
    public void UpdateSelectionIndicator(int selectedIndex)
    {
        for (int i = 0; i < spellCount; i++)
        {
            Transform spellIconTransform = transform.GetChild(i);
            if (spellIconTransform != null)
            {
                Vector3 targetPosition = new Vector3(spellIconTransform.localPosition.x, (i == selectedIndex) 
                ? selectionIndicatorOffset : 0f, 0f);
                spellIconTransform.localPosition = Vector3.Lerp(spellIconTransform.localPosition, targetPosition, 
                Time.deltaTime * 10f);
            }
        }
    }

    void UpdateCoolDownTimers()
    {
        for (int i = 0; i < spellCount; i++)
        {
            TextMeshProUGUI coolDownTime = transform.GetChild(i).transform.Find("CoolDownTime").GetComponent<TextMeshProUGUI>();
            UnityEngine.UI.Image icon = transform.GetChild(i).transform.Find("SpellIcon").GetComponent<UnityEngine.UI.Image>();
            if (spellcastingScript.spellCooldowns[i] <= 0)
            {
                icon.color = Color.white;
                coolDownTime.text = "";
            }
            else
            {
                icon.color = cooldownTint;
                coolDownTime.text = spellcastingScript.spellCooldowns[i].ToString("F0");
            }
        }
    }
}
