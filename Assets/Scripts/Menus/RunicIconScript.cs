using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunicIconScript : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Information/Status")]
    public int slotNumber;
    public bool inParty = false; 
    public bool partySlot = false;

    [Header("Visuals")]
    public float originalScale;
    public float hoverScale;
    public float selectedScale;
    public float targetScale;
    public bool isSelected = false;
    public bool counterpartSelected = false;
    public Color emptyColor = Color.gray;

    [Header("Counterpart Highlight")]
    public Image counterpartOutlineImage; // um Image extra no prefab, atrás do Frame, com sprite de borda
    public Color counterpartHighlightColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float counterpartFadeSpeed = 6f;

    [Header("References")]
    public GameObject frame;
    public GameObject runicIconImage;
    public RunicsMenuScript runicsMenuScript;

    void Start()
    {
        if (frame == null) 
            frame = transform.Find("Frame").gameObject;
        if (runicIconImage == null) 
            runicIconImage = transform.Find("IconMask/RunicIconImage").gameObject;
        if (runicsMenuScript == null) 
            runicsMenuScript = transform.parent.parent.GetComponent<RunicsMenuScript>();

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
        
        // Slot de party fica sempre em 0°. Slot da box usa o inParty (-90° ou 0°).
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
            inParty = targetIsInParty; // Deixa o Update() fazer o Lerp suave
        }

        Image iconImage = runicIconImage.GetComponent<Image>();

        if (runicData != null && runicData.runicIcon != null)
        {
            iconImage.sprite = runicData.runicIcon;
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
            if (runicsMenuScript.selectedSlot == gameObject)
            {
                runicsMenuScript.selectedSlot = null;
                isSelected = false;
            }
            else if (partySlot)
            {
                runicsMenuScript.runicStorageManager.RemoveFromPartyBySlot(slotNumber);
                isSelected = false;
            }
            else
            {
                if (!inParty)
                {    
                    int partySlotIndex = runicsMenuScript.runicStorageManager.GetNextAvailablePartySlot();
                    if (partySlotIndex != -1) 
                    {
                        runicsMenuScript.runicStorageManager.MoveToPartyBySlot(slotNumber, partySlotIndex);
                        // O inParty vai mudar organicamente e acionar o Lerp no próximo Refresh/Update
                    }
                }
                else
                {
                    // Se já estiver na party, acha qual slot da party ele ocupa e remove
                    int partyIndex = runicsMenuScript.runicStorageManager.GetPartySlotIndex(
                        runicsMenuScript.runicStorageManager.GetBoxRunicBySlot(slotNumber)?.runicInstanceId
                    );
                    if (partyIndex != -1)
                    {
                        runicsMenuScript.runicStorageManager.RemoveFromPartyBySlot(partyIndex);
                    }
                }
            }
        }
    }
}