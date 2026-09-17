using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunicWheelIconUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RunicWheelUI runicWheelScript;
    [SerializeField] private Image runicIcon;
    [SerializeField] private Sprite callbackSprite;
    [SerializeField] private bool isSummoned;
    [SerializeField] private GameObject icon;
    [SerializeField] private GameObject frame;
    [SerializeField] private string runicId;
    [SerializeField] private float angle;

    
    [SerializeField] private float originalScale;
    [SerializeField] private float hoverScale;
    [SerializeField] private float targetScale;
    [SerializeField] private Sprite normalSprite;

    private void OnEnable()
    {
        if (runicWheelScript == null)
            runicWheelScript = FindFirstObjectByType<RunicWheelUI>();
            
        if (frame == null)
        frame = transform.Find("IconFrame")?.gameObject;

        if (icon == null)
            icon = transform.Find("IconMask/Icon")?.gameObject;

        if (runicIcon == null && icon != null)
            runicIcon = icon.GetComponent<Image>();
    }

    void Update()
    {
        ScaleUpdate();
    }

    public void Setup(Sprite sprite, string id, float scale, float angle, bool summoned)
    {
        normalSprite = sprite;
        originalScale = scale;
        targetScale = scale;
        runicId = id;
        transform.localScale = new Vector3(scale, scale, scale);
        this.angle = angle;
        UpdateAngle();
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
        runicWheelScript.SetSymbolTargetAngle(angle);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}