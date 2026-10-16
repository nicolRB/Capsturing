using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoreInfo : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Tooltip tooltip;
    [SerializeField] private TooltipBlocker blocker;
    private bool tooltipPinned;

    public void OnPointerClick(PointerEventData eventData)
    {
        tooltipPinned = true;
        blocker.gameObject.SetActive(true);
        blocker.SetInfo(this);
        blocker.transform.SetAsLastSibling();
        tooltip.transform.SetAsLastSibling();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltip.gameObject.SetActive(true);
        tooltip.Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!tooltipPinned)
            tooltip.Hide();
    }

    public void Unpin()
    {
        tooltipPinned = false;
        tooltip.Hide();
    }
}