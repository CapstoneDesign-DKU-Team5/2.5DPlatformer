using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ItemSlotManager : MonoBehaviour
{
    public GameObject[] itemSlots;           // 16개 슬롯을 드래그해서 넣음
    public Item[] itemDatabase;              // 아이템 ScriptableObject 배열

    public GameObject tooltipPanel;          // TooltipPanel 프리팹 (씬에 있는 고정 오브젝트)
    public TextMeshProUGUI tooltipText;      // Tooltip 안의 설명 텍스트

    void Start()
    {
        LoadInventoryFromPlayFab();
    }

    void LoadInventoryFromPlayFab()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            List<ItemInstance> items = result.Inventory;

            int i = 0;
            for (; i < itemSlots.Length && i < items.Count; i++)
            {
                string itemId = items[i].ItemId;
                Item matchedItem = System.Array.Find(itemDatabase, item => item.itemId == itemId);

                if (matchedItem != null)
                {
                    AssignToSlot(itemSlots[i], matchedItem);
                }
            }

            // 남은 슬롯 비우기
            for (; i < itemSlots.Length; i++)
            {
                ClearSlot(itemSlots[i]);
            }
        },
        error =>
        {
            Debug.LogError("Inventory load failed: " + error.GenerateErrorReport());
        });
    }

    void ClearSlot(GameObject slot)
    {
        Transform iconTransform = slot.transform.Find("IconImg");
        if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
        {
            iconImage.sprite = null;
            iconImage.enabled = false; 
        }

        EventTriggerListener trigger = slot.GetComponent<EventTriggerListener>();
        if (trigger != null)
        {
            trigger.enabled = false; // 마우스 이벤트도 비활성화
        }
    }


    void AssignToSlot(GameObject slot, Item item)
    {
        Transform iconTransform = slot.transform.Find("IconImg");

        if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
        {
            iconImage.sprite = item.icon;
        }

        // 이벤트 트리거 컴포넌트 추가 및 설정
        EventTriggerListener trigger = slot.GetComponent<EventTriggerListener>();
        if (trigger == null)
        {
            trigger = slot.AddComponent<EventTriggerListener>();
        }

        trigger.item = item;
        trigger.tooltipManager = this;
    }

}
