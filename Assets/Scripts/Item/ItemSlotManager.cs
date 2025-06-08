using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ItemSlotManager : MonoBehaviour
{
    [Header("슬롯 & DB")]
    public GameObject[] itemSlots;           // 16개 슬롯
    public Item[] itemDatabase;              // 아이템 ScriptableObject 배열

    [Header("툴팁")]
    public GameObject tooltipPanel;          // TooltipPanel (씬에 배치)
    public TextMeshProUGUI tooltipText;      // Tooltip 안 설명 텍스트

    [Header("로딩 패널")]
    public GameObject inventoryLoadingPanel; // 인벤토리 전용 로딩 패널

    void Start()
    {
        // 시작 시 숨겨두기
        if (inventoryLoadingPanel != null)
            inventoryLoadingPanel.SetActive(false);

        LoadInventoryFromPlayFab();
    }

    void LoadInventoryFromPlayFab()
    {
        // 불러오기 시작 전 로딩 패널 켜기
        if (inventoryLoadingPanel != null)
            inventoryLoadingPanel.SetActive(true);

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            // --- 성공 콜백 ---
            List<ItemInstance> items = result.Inventory;
            int i = 0;
            for (; i < itemSlots.Length && i < items.Count; i++)
            {
                string itemId = items[i].ItemId;
                Item matchedItem = System.Array.Find(itemDatabase, item => item.itemId == itemId);
                if (matchedItem != null)
                {
                    int rawUses = items[i].RemainingUses ?? 1;
                    int itemCount = matchedItem.usesPerItem > 0
                        ? rawUses / matchedItem.usesPerItem
                        : rawUses;
                    AssignToSlot(itemSlots[i], matchedItem, itemCount);
                }
            }
            for (; i < itemSlots.Length; i++)
                ClearSlot(itemSlots[i]);

            // 로딩 완료 후 패널 끄기
            if (inventoryLoadingPanel != null)
                inventoryLoadingPanel.SetActive(false);
        },
        error =>
        {
            Debug.LogError("Inventory load failed: " + error.GenerateErrorReport());
            // 에러 시에도 패널 끄기
            if (inventoryLoadingPanel != null)
                inventoryLoadingPanel.SetActive(false);
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
        var trigger = slot.GetComponent<EventTriggerListener>();
        if (trigger != null)
            trigger.enabled = false;
    }

    void AssignToSlot(GameObject slot, Item item, int quantity)
    {
        Transform iconTransform = slot.transform.Find("IconImg");
        if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
            iconImage.sprite = item.icon;

        var trigger = slot.GetComponent<EventTriggerListener>();
        if (trigger == null)
            trigger = slot.AddComponent<EventTriggerListener>();

        trigger.item = item;
        trigger.quantity = quantity;
        trigger.tooltipManager = this;
    }
}
