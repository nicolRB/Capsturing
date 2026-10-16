using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visuals")]
    [SerializeField] private float originalScale = 1;
    [SerializeField] private float hoverScale = 1.25f;
    [SerializeField] private bool on = true;
    private float targetScale;

    void Awake()
    {
        originalScale = transform.localScale.x;
        targetScale = originalScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (on)
            ScaleUpdate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    void ScaleUpdate()
    {
        float scale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * 5f);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetOn(bool enable)
    {
        on = enable;

        if (!on)
        {
            targetScale = originalScale;
            transform.localScale = new Vector3(originalScale, originalScale, originalScale);
        }
    }
}
