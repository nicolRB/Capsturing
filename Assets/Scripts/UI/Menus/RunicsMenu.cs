using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class RunicsMenu : MonoBehaviour
{
    [Header("Box Pagination")]
    [SerializeField] private int boxPageIndex = 0;
    [SerializeField] private int boxRows = 3;
    [SerializeField] private int boxColumns = 7;
    [SerializeField] private TextMeshProUGUI pageCounter;
    [SerializeField] private GameObject pageFlipButtonPrevious;
    [SerializeField] private GameObject pageFlipButtonNext;
    private int page = 0;

    private int BoxSlotsPerPage => boxRows * boxColumns;
    private int BoxTotalPages => runicStorageManager == null ? 1
        : Mathf.Max(1, Mathf.CeilToInt((float)runicStorageManager.BoxStorage.Count / BoxSlotsPerPage));

    [Header("Slot Sizes")]
    [SerializeField] private float boxSlotSize = 1f;
    [SerializeField] private float partySlotSize = 1.2f;

    [Header("Inspector")]
    [SerializeField] private Image miniInspectorRunicIcon;
    [SerializeField] private TextMeshProUGUI nameField;
    [SerializeField] private GameObject statsFields;
    [SerializeField] private TextMeshProUGUI hpField;
    [SerializeField] private TextMeshProUGUI attackField;
    [SerializeField] private TextMeshProUGUI defenseField;
    [SerializeField] private TextMeshProUGUI magicField;
    [SerializeField] private TextMeshProUGUI magicDefenseField;

    [SerializeField] private Key inspectorKey = Key.E;

    [Header("References")]
    [SerializeField] private GameObject runicBox;
    [SerializeField] private GameObject runicParty;
    [SerializeField] private GameObject runicSlotPrefab;
    [SerializeField] private RunicStorageManager runicStorageManager;
    [SerializeField] private RunicDatabase runicDatabase;
    [SerializeField] private MenuManager menuManager;

    private GameObject selectedSlot;
    private GameObject hoveredSlot;
    private GridLayoutGroup boxGridLayout;

    public GameObject SelectedSlot => selectedSlot;
    public RunicStorageManager StorageManager => runicStorageManager;

    // Persistent slot lists avoid destroying and recreating UI objects.
    private List<RunicIconUI> boxSlotScripts = new List<RunicIconUI>();
    private List<RunicIconUI> partySlotScripts = new List<RunicIconUI>();

    void OnEnable()
    {
        if (runicStorageManager == null) runicStorageManager = FindFirstObjectByType<RunicStorageManager>();
        
        if (menuManager == null) menuManager = FindFirstObjectByType<MenuManager>();

        if (runicBox == null)
        {
            Transform boxTransform = transform.Find("Box");
            if (boxTransform != null) runicBox = boxTransform.gameObject;
        }

        if (runicParty == null)
        {
            Transform partyTransform = transform.Find("Party");
            if (partyTransform != null) runicParty = partyTransform.gameObject;
        }

        if (runicStorageManager == null || runicBox == null || runicParty == null || runicSlotPrefab == null)
        {
            Debug.LogError("RunicsMenu is missing a storage manager, Box, Party, or runic slot prefab.", this);
            return;
        }

        if (miniInspectorRunicIcon == null)
            miniInspectorRunicIcon = transform.Find("MiniInspector/SelectedRunicIcon/IconMask/RunicSprite")
                .GetComponent<Image>();

        if (nameField == null)
            nameField = transform.Find("MiniInspector/RunicName").GetComponent<TextMeshProUGUI>();

        if (statsFields == null)
            statsFields = transform.Find("MiniInspector/RunicStats").gameObject;

        if (hpField == null && statsFields != null)
            hpField = statsFields.transform.Find("HPField/FieldValue")
                .GetComponent<TextMeshProUGUI>();
            
        if (attackField == null && statsFields != null)
            attackField = statsFields.transform.Find("AttackField/FieldValue")
                .GetComponent<TextMeshProUGUI>();
            
        if (defenseField == null && statsFields != null)
            defenseField = statsFields.transform.Find("DefenseField/FieldValue")
                .GetComponent<TextMeshProUGUI>();
            
        if (magicField == null && statsFields != null)
            magicField = statsFields.transform.Find("MagicField/FieldValue")
                .GetComponent<TextMeshProUGUI>();
            
        if (magicDefenseField == null && statsFields != null)
            magicDefenseField = statsFields.transform.Find("MagicDefenseField/FieldValue")
                .GetComponent<TextMeshProUGUI>();

        UpdateMiniInspector();

        boxGridLayout = runicBox.GetComponent<GridLayoutGroup>();
        if (boxGridLayout != null)
        {
            boxGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            boxGridLayout.constraintCount = boxColumns;
        }

        // Create the fixed slot structure when the menu opens.
        InitializeSlotsStructure();

        RunicStorageManager.OnStorageChanged -= RefreshAllSlots;
        RunicStorageManager.OnStorageChanged += RefreshAllSlots;
        
        // Use instant positioning only when the menu opens.
        OpenMenuRefresh();
        UpdatePagination();
    }
    
    void Update()
    {
        if (Keyboard.current[inspectorKey].wasPressedThisFrame && (selectedSlot != null
        || hoveredSlot != null))
        {
            RunicIconUI slotScript = hoveredSlot != null ? hoveredSlot.GetComponent<RunicIconUI>() 
                : selectedSlot.GetComponent<RunicIconUI>();

            RunicSaveData data = slotScript.IsPartySlot
                ? StorageManager.GetPartyRunicBySlot(slotScript.SlotNumber)
                : StorageManager.GetBoxRunicBySlot(slotScript.SlotNumber);

            if (data != null)
                menuManager.OpenRunicInspectionMenu(data);
        }
    }

    public void ClearSelection(GameObject slot)
    {
        if (selectedSlot != slot) return;

        selectedSlot = null;
        slot.GetComponent<RunicIconUI>()?.SetSelected(false);
        UpdateCounterpartHighlights();
    }

    // Runs only during initial menu setup.
    void OpenMenuRefresh()
    {
        UpdatePartySlotsData(true);
        UpdateBoxSlotsData(true);
    }

    // Runs after storage changes during gameplay and preserves smooth animation.
    public void RefreshAllSlots()
    {
        page = Mathf.Clamp(page, 0, BoxTotalPages - 1);
        UpdatePartySlotsData(false);
        UpdateBoxSlotsData(false);
        UpdateCounterpartHighlights();
        UpdatePagination();
    }

    void UpdateBoxSlotsData(bool instant)
    {
        int startIndex = page * BoxSlotsPerPage;

        for (int localIndex = 0; localIndex < boxSlotScripts.Count; localIndex++)
        {
            int globalIndex = startIndex + localIndex;
            RunicIconUI slotScript = boxSlotScripts[localIndex];
            slotScript.ConfigureSlot(globalIndex, false, this);
            slotScript.gameObject.name = $"BoxSlot_{globalIndex}";

            RunicSaveData runicData = runicStorageManager.GetBoxRunicBySlot(globalIndex);
            bool isInParty = runicData != null && runicStorageManager.IsRunicInParty(runicData.runicInstanceId);

            slotScript.UpdateVisuals(runicData, isInParty, instant);
        }
    }

    void UpdatePartySlotsData(bool instant)
    {
        for (int i = 0; i < partySlotScripts.Count; i++)
        {
            RunicIconUI slotScript = partySlotScripts[i];
            slotScript.ConfigureSlot(i, true, this);
            slotScript.gameObject.name = $"PartySlot_{i}";

            RunicSaveData runicData = runicStorageManager.GetPartyRunicBySlot(i);
            
            slotScript.UpdateVisuals(runicData, false, instant);
        }
    }

    void OnDisable()
    {
        RunicStorageManager.OnStorageChanged -= RefreshAllSlots;
    }

    void OnValidate()
    {
        if (runicBox == null) return;
        var grid = runicBox.GetComponent<GridLayoutGroup>();
        if (grid != null) grid.constraintCount = boxColumns;
    }

    // Instantiate the fixed box and party slots.
    void InitializeSlotsStructure()
    {
        // Clear any existing references.
        ClearChildren(runicBox.transform);
        ClearChildren(runicParty.transform);
        boxSlotScripts.Clear();
        partySlotScripts.Clear();

        // Create box slots for the full page.
        for (int i = 0; i < BoxSlotsPerPage; i++)
        {
            GameObject newBoxSlot = Instantiate(runicSlotPrefab, runicBox.transform);
            newBoxSlot.transform.localScale = new Vector3(boxSlotSize, boxSlotSize, boxSlotSize);
            RunicIconUI script = newBoxSlot.GetComponent<RunicIconUI>();
            script.ConfigureSlot(i, false, this);
            boxSlotScripts.Add(script);
        }

        // Create party slots based on the player's maximum party size.
        int maxParty = runicStorageManager != null ? runicStorageManager.Player.MaxPartySize : 3;
        for (int i = 0; i < maxParty; i++)
        {
            GameObject newPartySlot = Instantiate(runicSlotPrefab, runicParty.transform);
            newPartySlot.transform.localScale = new Vector3(partySlotSize, partySlotSize, partySlotSize);
            RunicIconUI script = newPartySlot.GetComponent<RunicIconUI>();
            script.ConfigureSlot(i, true, this);
            partySlotScripts.Add(script);
        }
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    public void SelectSlot(GameObject clickedSlot)
    {
        RunicIconUI selectedSlotScript;
        RunicIconUI clickedSlotScript = clickedSlot.GetComponent<RunicIconUI>();
        
        if (selectedSlot == null)
        {
            if (!clickedSlotScript.IsPartySlot
                && runicStorageManager.GetBoxRunicBySlot(clickedSlotScript.SlotNumber) == null)
            {
                UpdateMiniInspector();
                return;
            }

            selectedSlot = clickedSlot;
            selectedSlotScript = selectedSlot.GetComponent<RunicIconUI>();
            selectedSlotScript.SetSelected(true);
            UpdateCounterpartHighlights();
            UpdateMiniInspector();
            return;
        }

        selectedSlotScript = selectedSlot.GetComponent<RunicIconUI>();

        if (selectedSlot == clickedSlot) { 
            selectedSlot = null; 
        }
        else if (selectedSlotScript.IsPartySlot && clickedSlotScript.IsPartySlot)
        {
            runicStorageManager.MovePositionInParty(
                selectedSlotScript.SlotNumber, 
                clickedSlotScript.SlotNumber
            );
        }
        else if (!selectedSlotScript.IsPartySlot && clickedSlotScript.IsPartySlot)
        {
            if (runicStorageManager.GetBoxRunicBySlot(selectedSlotScript.SlotNumber) == null) 
            {
                // If the box slot is empty, clear the selection.
                selectedSlotScript.SetSelected(false);
                UpdateMiniInspector();
                return;
            }
            if (runicStorageManager.IsRunicInParty(
                runicStorageManager.GetBoxRunicBySlot(selectedSlotScript.SlotNumber)?.runicInstanceId
            ))
            {
                if (runicStorageManager.GetBoxRunicBySlot(selectedSlotScript.SlotNumber)?.runicInstanceId == 
                runicStorageManager.GetBoxRunicBySlot(clickedSlotScript.SlotNumber)?.runicInstanceId)
                {
                    // If this runic is already in the clicked party slot, remove it.
                    runicStorageManager.RemoveFromPartyBySlot(clickedSlotScript.SlotNumber);
                }
                else {
                    // If a different runic is already in the party, swap positions.
                    int partyIndex = runicStorageManager.GetPartySlotIndex(
                        runicStorageManager.GetBoxRunicBySlot(selectedSlotScript.SlotNumber)?.runicInstanceId
                    );
                    if (partyIndex != -1)
                    {
                        runicStorageManager.MovePositionInParty(
                            partyIndex,
                            clickedSlotScript.SlotNumber
                        );
                    }
                }
            }
            else
            {
                runicStorageManager.MoveToPartyBySlot(
                    selectedSlotScript.SlotNumber,
                    clickedSlotScript.SlotNumber
                );
            }
        }
        else if(selectedSlotScript.IsPartySlot && !clickedSlotScript.IsPartySlot)
        {   
            if (runicStorageManager.GetBoxRunicBySlot(clickedSlotScript.SlotNumber) == null
            || runicStorageManager.GetBoxRunicBySlot(clickedSlotScript.SlotNumber)?.runicInstanceId == null
            || runicStorageManager.GetBoxRunicBySlot(clickedSlotScript.SlotNumber)?.runicInstanceId == 
                runicStorageManager.GetPartyRunicBySlot(selectedSlotScript.SlotNumber)?.runicInstanceId)
            {
                // If the box slot is empty, remove the runic from the party.
                runicStorageManager.RemoveFromPartyBySlot(selectedSlotScript.SlotNumber);
            }
            else
            {
                runicStorageManager.MoveToPartyBySlot(
                    clickedSlotScript.SlotNumber,
                    selectedSlotScript.SlotNumber
                );
            }
        }
        else
        {
            runicStorageManager.MovePositionInBoxBySlot(
                selectedSlotScript.SlotNumber,
                clickedSlotScript.SlotNumber
            );
        }
        clickedSlotScript.SetSelected(false);
        selectedSlot = null;
        selectedSlotScript.SetSelected(false);
        selectedSlotScript.ResetTargetScale();
        clickedSlotScript.ResetTargetScale();
        UpdateCounterpartHighlights();
        RefreshAllSlots();
        UpdateMiniInspector();
    }

    void UpdateMiniInspector()
    {
        if (selectedSlot == null)
        {
            if (miniInspectorRunicIcon != null)
            miniInspectorRunicIcon.color = new Color(0, 0, 0, 0);

            if (nameField != null)
                nameField.gameObject.SetActive(false);

            if (statsFields != null)
                statsFields.SetActive(false);

            return;
        }
        else if (statsFields != null)
            statsFields.SetActive(true);

        RunicIconUI iconScript = selectedSlot.GetComponent<RunicIconUI>();

        RunicSaveData runicData;

        if (iconScript != null)
            runicData = iconScript.IsPartySlot
                ? runicStorageManager.GetPartyRunicBySlot(iconScript.SlotNumber)
                : runicStorageManager.GetBoxRunicBySlot(iconScript.SlotNumber);
        else 
        {
            if (miniInspectorRunicIcon != null)
                miniInspectorRunicIcon.color = new Color(0, 0, 0, 0);

            return;
        }
        

        if (runicData == null)
        {
            if (miniInspectorRunicIcon != null)
                miniInspectorRunicIcon.color = new Color(0, 0, 0, 0);
            return;
        }

        if (miniInspectorRunicIcon != null)
        {
            miniInspectorRunicIcon.color = Color.white;
            Sprite runicSprite = runicStorageManager != null ? runicStorageManager.GetRunicIcon(runicData) : null;
            miniInspectorRunicIcon.sprite = runicSprite;
        }

        if (nameField != null)
        {
            nameField.gameObject.SetActive(true);

            if (runicData.nickname != null)
                nameField.text = runicData.nickname;
            else if (runicDatabase != null) 
            {
                string speciesName = runicData.speciesId != null ? 
                    runicDatabase.GetSpeciesById(runicData.speciesId).SpeciesName : "?";
                nameField.text = speciesName;
            }
            else 
                Debug.LogWarning("Runics Menu: Runic Database missing / not referenced.");
        }

        if (hpField != null)
            hpField.text = runicData.currentHP.ToString() + " / " + runicData.maxHP .ToString();
        if (attackField != null)
            attackField.text = runicData.attack.ToString();
        if (defenseField != null)
            defenseField.text = runicData.defense.ToString();
        if (magicField != null)
            magicField.text = runicData.magic.ToString();
        if (magicDefenseField != null)
            magicDefenseField.text = runicData.magicDefense.ToString();
    }

    private void ClearCounterpartHighlights()
    {
        foreach (var s in boxSlotScripts) s.SetCounterpartHighlight(false);
        foreach (var s in partySlotScripts) s.SetCounterpartHighlight(false);
    }

    private void UpdateCounterpartHighlights()
    {
        ClearCounterpartHighlights();

        if (selectedSlot == null) return;

        RunicIconUI selectedScript = selectedSlot.GetComponent<RunicIconUI>();
        string runicId = selectedScript.IsPartySlot
            ? runicStorageManager.GetPartyRunicBySlot(selectedScript.SlotNumber)?.runicInstanceId
            : runicStorageManager.GetBoxRunicBySlot(selectedScript.SlotNumber)?.runicInstanceId;

        if (string.IsNullOrEmpty(runicId)) return;

        if (selectedScript.IsPartySlot)
        {
            // contraparte fica na box, na página atual
            int boxIdx = runicStorageManager.GetBoxSlotIndex(runicId);
            int localIdx = boxIdx - (page * BoxSlotsPerPage);
            if (localIdx >= 0 && localIdx < boxSlotScripts.Count)
                boxSlotScripts[localIdx].SetCounterpartHighlight(true);
        }
        else
        {
            // contraparte fica na party, se esse rúnico estiver nela
            int partyIdx = runicStorageManager.GetPartySlotIndex(runicId);
            if (partyIdx != -1 && partyIdx < partySlotScripts.Count)
                partySlotScripts[partyIdx].SetCounterpartHighlight(true);
        }
    }

    public void ChangePage(int newPage)
    {
        if (newPage < 0 || newPage >= BoxTotalPages) 
            return;

        page = newPage;
        selectedSlot = null;
        RefreshAllSlots();
    }

    public void FlipPage(int pageAmount)
    {
        page = (page + pageAmount) % BoxTotalPages;
        if (page < 0)
            page += BoxTotalPages;
        
        selectedSlot = null;
        RefreshAllSlots();
    }

    void UpdatePagination()
    {
        bool canFlipPages = BoxTotalPages > 1;

        if (pageCounter != null)
            pageCounter.text = $"{page + 1}/{BoxTotalPages}";
        else
            Debug.LogWarning("Runics Menu: Page Counter missing / Not Referenced");

        SetPageFlipButtonState(pageFlipButtonPrevious, canFlipPages, "Previous");
        SetPageFlipButtonState(pageFlipButtonNext, canFlipPages, "Next");
    }

    private void SetPageFlipButtonState(GameObject buttonObject, bool canFlipPages, string buttonName)
    {
        if (buttonObject == null)
        {
            Debug.LogWarning($"Runics Menu: Page Flip Button {buttonName} missing / not referenced.");
            return;
        }

        if (buttonObject.TryGetComponent(out Button button))
            button.interactable = canFlipPages;
        else
            Debug.LogWarning($"Runics Menu: Page Flip Button {buttonName} is missing a Button component.", buttonObject);

        if (buttonObject.TryGetComponent(out ButtonHoverScale hoverScale))
            hoverScale.SetOn(canFlipPages);
    }

    public void SetHoveredSlot(GameObject slot)
    {
        hoveredSlot = slot;
    }
}