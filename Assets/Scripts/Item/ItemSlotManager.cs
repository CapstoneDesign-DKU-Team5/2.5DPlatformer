using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ItemSlotManager : MonoBehaviour
{
    [Header("���� & DB")]
    public GameObject[] itemSlots;           // 16�� ����
    public Item[] itemDatabase;              // ������ ScriptableObject �迭

    [Header("����")]
    public GameObject tooltipPanel;          // TooltipPanel (���� ��ġ)
    public TextMeshProUGUI tooltipText;      // Tooltip �� ���� �ؽ�Ʈ

    [Header("�ε� �г�")]
    public GameObject inventoryLoadingPanel; // �κ��丮 ���� �ε� �г�

    void Start()
    {
        // ���� �� ���ܵα�
        if (inventoryLoadingPanel != null)
            inventoryLoadingPanel.SetActive(false);

        LoadInventoryFromPlayFab();
    }

    void LoadInventoryFromPlayFab()
    {
        // �ҷ����� ���� �� �ε� �г� �ѱ�
        if (inventoryLoadingPanel != null)
            inventoryLoadingPanel.SetActive(true);

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            // --- ���� �ݹ� ---
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

            // �ε� �Ϸ� �� �г� ����
            if (inventoryLoadingPanel != null)
                inventoryLoadingPanel.SetActive(false);
        },
        error =>
        {
            Debug.LogError("Inventory load failed: " + error.GenerateErrorReport());
            // ���� �ÿ��� �г� ����
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
