using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RunicsMenuScript : MonoBehaviour
{
    [Header("Box Pagination")]
    public int boxPageIndex = 0;
    public int boxRows = 3;
    public int boxColumns = 7;
    public int boxSlotsPerPage => boxRows * boxColumns;
    public int boxTotalPages => Mathf.CeilToInt((float)runicStorageManager.boxStorage.Count / boxSlotsPerPage);
    public int page = 0;

    [Header("Slot Sizes")]
    public float boxSlotSize = 1f;
    public float partySlotSize = 1.2f;

    [Header("References")]
    public GameObject runicBox;
    public GameObject runicParty;
    public GameObject runicSlotPrefab;
    public GameObject selectedSlot;
    public RunicStorageManager runicStorageManager;
    private GridLayoutGroup boxGridLayout;

    // Listas para manter os slots persistentes (evita destruir e recriar)
    private List<RunicIconScript> boxSlotScripts = new List<RunicIconScript>();
    private List<RunicIconScript> partySlotScripts = new List<RunicIconScript>();

    void OnEnable()
    {
        if (runicStorageManager == null) runicStorageManager = FindFirstObjectByType<RunicStorageManager>();

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
            Debug.LogError("RunicsMenuScript is missing a storage manager, Box, Party, or runic slot prefab.", this);
            return;
        }

        boxGridLayout = runicBox.GetComponent<GridLayoutGroup>();
        if (boxGridLayout != null)
        {
            boxGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            boxGridLayout.constraintCount = boxColumns;
        }

        // Cria a estrutura fixa de slots uma única vez ao abrir
        InitializeSlotsStructure();

        RunicStorageManager.OnStorageChanged -= RefreshAllSlots;
        RunicStorageManager.OnStorageChanged += RefreshAllSlots;
        
        // Aqui usamos true APENAS ao abrir o menu, para eles nascerem posicionados instantaneamente!
        OpenMenuRefresh();
    }

    // Método rodado apenas na abertura/instanciação inicial
    void OpenMenuRefresh()
    {
        UpdatePartySlotsData(true);
        UpdateBoxSlotsData(true);
    }

    // Método rodado pelo evento OnStorageChanged durante a gameplay (mantém a animação suave)
    public void RefreshAllSlots()
    {
        UpdatePartySlotsData(false);
        UpdateBoxSlotsData(false);
        UpdateCounterpartHighlights();
    }

    void UpdateBoxSlotsData(bool instant)
    {
        int startIndex = page * boxSlotsPerPage;

        for (int localIndex = 0; localIndex < boxSlotScripts.Count; localIndex++)
        {
            int globalIndex = startIndex + localIndex;
            RunicIconScript slotScript = boxSlotScripts[localIndex];
            slotScript.slotNumber = globalIndex;
            slotScript.gameObject.name = $"BoxSlot_{globalIndex}";

            RunicSaveData runicData = runicStorageManager.GetBoxRunic(globalIndex);
            bool isInParty = runicData != null && runicStorageManager.IsRunicInParty(runicData.runicInstanceId);

            slotScript.UpdateVisuals(runicData, isInParty, instant);
        }
    }

    void UpdatePartySlotsData(bool instant)
    {
        for (int i = 0; i < partySlotScripts.Count; i++)
        {
            RunicIconScript slotScript = partySlotScripts[i];
            slotScript.slotNumber = i;
            slotScript.gameObject.name = $"PartySlot_{i}";

            RunicSaveData runicData = runicStorageManager.GetPartyRunic(i);
            
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

    // Instancia os slots da Box e da Party fixos apenas uma vez
    void InitializeSlotsStructure()
    {
        // Limpa referências antigas se houver
        ClearChildren(runicBox.transform);
        ClearChildren(runicParty.transform);
        boxSlotScripts.Clear();
        partySlotScripts.Clear();

        // Instancia os slots da Box para a página inteira
        for (int i = 0; i < boxSlotsPerPage; i++)
        {
            GameObject newBoxSlot = Instantiate(runicSlotPrefab, runicBox.transform);
            newBoxSlot.transform.localScale = new Vector3(boxSlotSize, boxSlotSize, boxSlotSize);
            RunicIconScript script = newBoxSlot.GetComponent<RunicIconScript>();
            script.partySlot = false;
            script.runicsMenuScript = this;
            boxSlotScripts.Add(script);
        }

        // Instancia os slots da Party baseados no tamanho máximo do player
        int maxParty = runicStorageManager != null ? runicStorageManager.player.maxPartySize : 3;
        for (int i = 0; i < maxParty; i++)
        {
            GameObject newPartySlot = Instantiate(runicSlotPrefab, runicParty.transform);
            newPartySlot.transform.localScale = new Vector3(partySlotSize, partySlotSize, partySlotSize);
            RunicIconScript script = newPartySlot.GetComponent<RunicIconScript>();
            script.partySlot = true;
            script.runicsMenuScript = this;
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
        RunicIconScript selectedSlotScript;
        RunicIconScript clickedSlotScript = clickedSlot.GetComponent<RunicIconScript>();
        
        if (selectedSlot == null)
        {
            if (!clickedSlotScript.partySlot
                && runicStorageManager.GetBoxRunic(clickedSlotScript.slotNumber) == null)
            {
                return;
            }

            selectedSlot = clickedSlot;
            selectedSlotScript = selectedSlot.GetComponent<RunicIconScript>();
            selectedSlotScript.isSelected = true;
            selectedSlotScript.targetScale = selectedSlotScript.originalScale * selectedSlotScript.selectedScale;
            UpdateCounterpartHighlights();
            return;
        }

        selectedSlotScript = selectedSlot.GetComponent<RunicIconScript>();

        if (selectedSlot == clickedSlot) { 
            selectedSlot = null; 
        }
        else if (selectedSlotScript.partySlot && clickedSlotScript.partySlot)
        {
            runicStorageManager.MovePositionInParty(
                selectedSlotScript.slotNumber, 
                clickedSlotScript.slotNumber
            );
        }
        else if (!selectedSlotScript.partySlot && clickedSlotScript.partySlot)
        {
            if (runicStorageManager.GetBoxRunic(selectedSlotScript.slotNumber) == null) 
            {
                // Se o slot da box estiver vazio, apenas desmarca a seleção
                selectedSlotScript.isSelected = false;
                return;
            }
            if (runicStorageManager.IsRunicInParty(
                runicStorageManager.GetBoxRunic(selectedSlotScript.slotNumber)?.runicInstanceId
            ))
            {
                if (runicStorageManager.GetBoxRunic(selectedSlotScript.slotNumber)?.runicInstanceId == 
                runicStorageManager.GetPartyRunic(clickedSlotScript.slotNumber)?.runicInstanceId)
                {
                    // Se o rúnico da box já estiver no slot da party clicado, remove-o da party
                    runicStorageManager.RemoveFromPartyBySlot(clickedSlotScript.slotNumber);
                }
                else {
                    // Se o rúnico já estiver na party e for diferente, apenas troca de posição
                    int partyIndex = runicStorageManager.GetPartySlotIndex(
                        runicStorageManager.GetBoxRunic(selectedSlotScript.slotNumber)?.runicInstanceId
                    );
                    if (partyIndex != -1)
                    {
                        runicStorageManager.MovePositionInParty(
                            partyIndex,
                            clickedSlotScript.slotNumber
                        );
                    }
                }
            }
            else
            {
                runicStorageManager.MoveToPartyBySlot(
                    selectedSlotScript.slotNumber,
                    clickedSlotScript.slotNumber
                );
            }
        }
        else if(selectedSlotScript.partySlot && !clickedSlotScript.partySlot)
        {   
            if (runicStorageManager.GetBoxRunic(clickedSlotScript.slotNumber) == null
            || runicStorageManager.GetBoxRunic(clickedSlotScript.slotNumber)?.runicInstanceId == null
            || runicStorageManager.GetBoxRunic(clickedSlotScript.slotNumber)?.runicInstanceId == 
                runicStorageManager.GetPartyRunic(selectedSlotScript.slotNumber)?.runicInstanceId)
            {
                // Se o slot da box for vazio, remove o rúnico da party.
                runicStorageManager.RemoveFromPartyBySlot(selectedSlotScript.slotNumber);
            }
            else
            {
                runicStorageManager.MoveToPartyBySlot(
                    clickedSlotScript.slotNumber,
                    selectedSlotScript.slotNumber
                );
            }
        }
        else
        {
            runicStorageManager.MovePositionInBoxBySlot(
                selectedSlotScript.slotNumber,
                clickedSlotScript.slotNumber
            );
        }
        clickedSlotScript.isSelected = false;
        selectedSlot = null;
        selectedSlotScript.targetScale = selectedSlotScript.originalScale;
        clickedSlotScript.targetScale = clickedSlotScript.originalScale;
        UpdateCounterpartHighlights();
        RefreshAllSlots();
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

        RunicIconScript selectedScript = selectedSlot.GetComponent<RunicIconScript>();
        string runicId = selectedScript.partySlot
            ? runicStorageManager.GetPartyRunic(selectedScript.slotNumber)?.runicInstanceId
            : runicStorageManager.GetBoxRunic(selectedScript.slotNumber)?.runicInstanceId;

        if (string.IsNullOrEmpty(runicId)) return;

        if (selectedScript.partySlot)
        {
            // contraparte fica na box, na página atual
            int boxIdx = runicStorageManager.GetBoxSlotIndex(runicId);
            int localIdx = boxIdx - (page * boxSlotsPerPage);
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
        if (newPage < 0 || newPage >= boxTotalPages) return;
        page = newPage;
        selectedSlot = null;
        RefreshAllSlots();
    }
}