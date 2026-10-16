using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReturnArrow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visuals")]
    [SerializeField] private float originalScale = 1;
    [SerializeField] private float hoverScale = 1.25f;
    private float targetScale;

    [Header("References")]
    [SerializeField] private MenuManager menuManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (menuManager == null)
            menuManager = FindFirstObjectByType<MenuManager>();

        originalScale = transform.localScale.x;

        targetScale = originalScale;
    }

    void OnEnable()
    {
        transform.localScale = new Vector3(originalScale, originalScale, originalScale);
        targetScale = originalScale;
    }

    // Update is called once per frame
    void Update()
    {
        ScaleUpdate();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        menuManager.Escape();
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
}
