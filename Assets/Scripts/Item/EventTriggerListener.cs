using UnityEngine;
using UnityEngine.EventSystems;

public class EventTriggerListener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Item item;
    public ItemSlotManager tooltipManager;


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipManager != null && item != null)
        {
            tooltipManager.tooltipText.text = $"<b>{item.displayName}</b>\n\n{item.description}";
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipManager != null)
        {
            tooltipManager.tooltipText.text = "";
        }
    }
}
