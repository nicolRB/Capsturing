using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipBlocker : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] MoreInfo info;

    public void OnPointerClick(PointerEventData eventData)
    {
        info.Unpin(); // define tooltipPinned = false, chama Hide() e desativa o blocker
        info = null;
        gameObject.SetActive(false);
    }

    public void SetInfo (MoreInfo info)
    {
        this.info = info;
    }
}