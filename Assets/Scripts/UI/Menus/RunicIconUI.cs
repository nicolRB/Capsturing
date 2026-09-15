using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunicIconUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Information/Status")]
    private int slotNumber;
    private bool inParty;
    private bool partySlot;

    [Header("Visuals")]
    private float originalScale;
    [SerializeField] private float hoverScale;
    [SerializeField] private float selectedScale;
    private float targetScale;
    private bool isSelected;
    private bool counterpartSelected;
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Counterpart Highlight")]
    [Tooltip("Optional outline image behind the frame used to highlight the matching slot.")]
    [SerializeField] private Image counterpartOutlineImage;
    [SerializeField] private Color counterpartHighlightColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float counterpartFadeSpeed = 6f;

    [Header("References")]
    [SerializeField] private GameObject frame;
    [SerializeField] private GameObject runicIconImage;
    private RunicsMenu runicsMenuScript;

    public int SlotNumber => slotNumber;
    public bool IsInParty => inParty;
    public bool IsPartySlot => partySlot;
    public float OriginalScale => originalScale;
    public float SelectedScale => selectedScale;

    public void ConfigureSlot(int number, bool isPartySlot, RunicsMenu menu)
    {
        slotNumber = number;
        partySlot = isPartySlot;
        runicsMenuScript = menu;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        targetScale = selected ? originalScale * selectedScale : originalScale;
    }

    public void ResetTargetScale()
    {
        targetScale = originalScale;
    }

    void Start()
    {
        if (frame == null) 
            frame = transform.Find("Frame").gameObject;
        if (runicIconImage == null) 
            runicIconImage = transform.Find("IconMask/RunicIconImage").gameObject;
        if (runicsMenuScript == null) 
            runicsMenuScript = transform.parent.parent.GetComponent<RunicsMenu>();

        originalScale = transform.localScale.x;
        targetScale = originalScale;

        if (counterpartOutlineImage == null)
            counterpartOutlineImage = transform.Find("Highlight")?.GetComponent<Image>();

        if (counterpartOutlineImage != null)
        {
            Color c = counterpartHighlightColor;
            c.a = 0f;
            counterpartOutlineImage.color = c;
        }
    }

    void Update()
    {
        FrameRotation();
        ScaleUpdate();
        CounterpartVisualUpdate();
    }

    void FrameRotation()
    {
        if (frame == null) return;
        
        // Party slots always use -90 degrees; box slots rotate based on party membership.
        float targetRotation = partySlot ? -90f : (inParty ? -90f : 0f); 

        Vector3 rotation = frame.transform.eulerAngles;
        rotation.z = Mathf.LerpAngle(rotation.z, targetRotation, Time.deltaTime * 5f);
        frame.transform.eulerAngles = rotation;
    }

    public void SetFrameRotationInstant(bool targetIsInParty)
    {
        inParty = targetIsInParty;
        if (frame == null) frame = transform.Find("Frame").gameObject;
        
        float targetRotation = partySlot ? -90f : (inParty ? -90f : 0f);
        Vector3 rotation = frame.transform.eulerAngles;
        rotation.z = targetRotation;
        frame.transform.eulerAngles = rotation;
    }

    public void ScaleUpdate()
    {
        float scale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * 5f);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    public void UpdateVisuals(RunicSaveData runicData, bool targetIsInParty, bool instant = false)
    {
        if (instant)
        {
            SetFrameRotationInstant(targetIsInParty);
        }
        else
        {
            inParty = targetIsInParty;
        }

        Image iconImage = runicIconImage.GetComponent<Image>();

        Sprite runicIcon = runicData != null && runicsMenuScript.StorageManager != null
            ? runicsMenuScript.StorageManager.GetRunicIcon(runicData)
            : null;

        if (runicIcon != null)
        {
            iconImage.sprite = runicIcon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = true;
            iconImage.color = emptyColor;
        }
    }

    void CounterpartVisualUpdate()
    {
        if (counterpartOutlineImage == null) return;

        float targetAlpha = counterpartSelected ? counterpartHighlightColor.a : 0f;
        Color c = counterpartOutlineImage.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * counterpartFadeSpeed);
        counterpartOutlineImage.color = c;
    }

    public void SetCounterpartHighlight(bool active)
    {
        counterpartSelected = active;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = new Vector3(originalScale, originalScale, originalScale);
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = isSelected ? originalScale * selectedScale : originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) runicsMenuScript.SelectSlot(gameObject);
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (runicsMenuScript.SelectedSlot == gameObject)
            {
                runicsMenuScript.ClearSelection(gameObject);
            }
            else if (IsPartySlot)
            {
                runicsMenuScript.StorageManager.RemoveFromPartyBySlot(SlotNumber);
                SetSelected(false);
            }
            else
            {
                if (!IsInParty)
                {    
                    int partySlotIndex = runicsMenuScript.StorageManager.GetNextAvailablePartySlot();
                    if (partySlotIndex != -1) 
                    {
                        runicsMenuScript.StorageManager.MoveToPartyBySlot(SlotNumber, partySlotIndex);
                    }
                }
                else
                {
                    // If already in the party, find its party slot and remove it.
                    int partyIndex = runicsMenuScript.StorageManager.GetPartySlotIndex(
                        runicsMenuScript.StorageManager.GetBoxRunicBySlot(SlotNumber)?.runicInstanceId
                    );
                    if (partyIndex != -1)
                    {
                        runicsMenuScript.StorageManager.RemoveFromPartyBySlot(partyIndex);
                    }
                }
            }
        }
    }
}