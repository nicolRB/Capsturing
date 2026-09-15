using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SpellSelectUI : MonoBehaviour
{
    [Header("Spell Selection")]
    private List<GameObject> spellIcons = new List<GameObject>();
    [Tooltip("Horizontal spacing between spell icons.")]
    [SerializeField] private float iconSpacing = 100f;
    [Tooltip("Reference size of each spell icon.")]
    [SerializeField] private float iconSize = 80f;
    [Tooltip("Scale applied to each spell icon.")]
    [SerializeField] private float iconScale = 0.91f;
    [Tooltip("Vertical offset applied to the selected spell icon.")]
    [SerializeField] private float selectionIndicatorOffset = 10f;
    private int spellCount = 0;

    [Header("Cooldown Visuals")]
    [SerializeField] private Color cooldownTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("References")]
    [SerializeField] private SpellcastingCoordinator spellcastingCoordinator;
    [SerializeField] private GameObject spellIconPrefab; // Prefab for the spell icon UI element

    void Start()
    {
        if (spellcastingCoordinator == null) spellcastingCoordinator = FindFirstObjectByType<SpellcastingCoordinator>();

        if (spellIconPrefab == null) Debug.LogError("SpellSelectUI: Spell icon prefab is not assigned.");
    }

    void Update()
    {
        if (spellcastingCoordinator != null)
        {
            UpdateSelectionIndicator(spellcastingCoordinator.SpellIndex);
            UpdateCoolDownTimers();
        }   
    }

    public void Setup(SpellcastingCoordinator spellcastingCoordinator){
        this.spellcastingCoordinator = spellcastingCoordinator;
    }

    public void UpdateSpellList()
    {
        // Clear existing spell icons.
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Create icons from the Spells registered by the coordinator.
        if (spellcastingCoordinator.Spells != null)
        {
            foreach (var spell in spellcastingCoordinator.Spells)
            {
                GameObject spellSelectIcon = Instantiate(spellIconPrefab, transform);
                // Each icon prefab contains a child named "SpellIcon".
                var spellIconImage = spellSelectIcon.transform.Find("SpellIcon").GetComponent<UnityEngine.UI.Image>();
                if (spellIconImage != null)
                {
                    spellIconImage.sprite = spell.SpellIcon;
                }
                spellSelectIcon.transform.localScale = Vector3.one * iconScale;
                spellCount++;
            }
        }

        ArrangeSpellIcons();
    }

    // Position spell icons along a centered horizontal line.
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

    // Smoothly move the selected spell icon by the configured offset.
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
            if (spellcastingCoordinator.SpellCooldowns[i] <= 0)
            {
                icon.color = Color.white;
                coolDownTime.text = "";
            }
            else
            {
                icon.color = cooldownTint;
                coolDownTime.text = spellcastingCoordinator.SpellCooldowns[i].ToString("F0");
            }
        }
    }
}
