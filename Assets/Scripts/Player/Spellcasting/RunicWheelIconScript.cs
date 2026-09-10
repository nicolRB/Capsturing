using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunicWheelIconScript : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private RunicWheelScript runicWheelScript;
    public Image runicIcon;
    public Sprite callbackSprite;
    public bool isSummoned;
    public GameObject icon;
    public GameObject frame;
    public string runicId;
    public float angle;

    
    public float originalScale;
    public float hoverScale;
    public float targetScale;
    private Sprite normalSprite;

    private void OnEnable()
    {
        if (runicWheelScript == null)
            runicWheelScript = FindFirstObjectByType<RunicWheelScript>();
        
        frame = transform.Find("IconFrame").gameObject;
        icon = transform.Find("IconMask/Icon").gameObject;
        runicIcon = icon.GetComponent<Image>();
    }

    void Update()
    {
        ScaleUpdate();
    }

    public void Setup(Sprite sprite, string id, float scale, bool summoned)
    {
        normalSprite = sprite;
        runicId = id;
        transform.localScale = new Vector3(scale, scale, scale);
        SetSummonedVisual(summoned);
    }

    public void SetSummonedVisual(bool summoned)
    {
        isSummoned = summoned;

        if (runicIcon != null)
        {
            runicIcon.sprite = summoned && callbackSprite != null
                ? callbackSprite
                : normalSprite;
        }
    }

    public void UpdateAngle()
    {
        Vector3 rotation = frame.transform.eulerAngles;
        rotation.z = angle;
        frame.transform.eulerAngles = rotation;
    }

    public void ScaleUpdate()
    {
        float scale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * 5f);
        transform.localScale = new Vector3(scale, scale, scale);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        runicWheelScript.SelectRunic(runicId);
    }
    

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = new Vector3(originalScale, originalScale, originalScale);
        targetScale = originalScale * hoverScale;
        runicWheelScript.symbolTargetAngle = angle;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}