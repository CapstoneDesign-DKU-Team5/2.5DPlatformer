using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ItemSlotManager : MonoBehaviour
{
    public GameObject[] itemSlots;           // 16�� ������ �巡���ؼ� ����
    public Item[] itemDatabase;              // ������ ScriptableObject �迭

    public GameObject tooltipPanel;          // TooltipPanel ������ (���� �ִ� ���� ������Ʈ)
    public TextMeshProUGUI tooltipText;      // Tooltip ���� ���� �ؽ�Ʈ

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
                    int rawUses = items[i].RemainingUses ?? 1;
                    int itemCount = matchedItem.usesPerItem > 0 ? rawUses / matchedItem.usesPerItem : rawUses;

                    AssignToSlot(itemSlots[i], matchedItem, itemCount);


                }
            }

            // ���� ���� ����
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
            trigger.enabled = false; // ���콺 �̺�Ʈ�� ��Ȱ��ȭ
        }
    }


    void AssignToSlot(GameObject slot, Item item, int quantity)
    {
        Transform iconTransform = slot.transform.Find("IconImg");

        if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
        {
            iconImage.sprite = item.icon;
        }

        EventTriggerListener trigger = slot.GetComponent<EventTriggerListener>();
        if (trigger == null)
            trigger = slot.AddComponent<EventTriggerListener>();

        trigger.item = item;
        trigger.quantity = quantity;
        trigger.tooltipManager = this;
    }


}
