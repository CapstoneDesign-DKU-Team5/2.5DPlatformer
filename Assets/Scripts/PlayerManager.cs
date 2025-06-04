using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using Photon.Pun;
using HelloWorld;

public class PlayerManager : MonoBehaviour
{
    public float damagePerSecond = 10f;
    public float damageInterval = 1f;
    public int maxInventoryCount = 8;
    public GameObject[] itemSlots = new GameObject[8];
    public Item[] itemDatabase; // ������ ScriptableObject �迭

    [HideInInspector]
    public List<ItemInstance> playerItems = new List<ItemInstance>();

    public NetworkPlayer targetPlayer;

    private void Awake()
    {
        if (targetPlayer == null)
        {
            NetworkPlayer[] allPlayers = Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var np in allPlayers)
            {
                PhotonView pv = np.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    targetPlayer = np;
                    break;
                }
            }
        }
        if (itemSlots == null || itemSlots.Length != maxInventoryCount)
        {
            Debug.LogWarning($"itemSlots �迭 ũ�⸦ {maxInventoryCount}�� �����ּ���. ���� ����: {(itemSlots == null ? 0 : itemSlots.Length)}");
        }
    }

    private void Start()
    {
        InvokeRepeating(nameof(ApplyDamage), damageInterval, damageInterval);
        LoadInventory();
    }

    private void ApplyDamage()
    {
        if (targetPlayer == null)
        {
            NetworkPlayer[] allPlayers = Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var np in allPlayers)
            {
                PhotonView pv = np.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    targetPlayer = np;
                    break;
                }
            }
            if (targetPlayer == null) return;
        }
        int damageInt = Mathf.FloorToInt(damagePerSecond);
        if (damageInt > 0)
        {
            targetPlayer.TakeDamage(damageInt);
        }
    }

    public void LoadInventory()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            playerItems.Clear();
            int count = Mathf.Min(maxInventoryCount, result.Inventory.Count);
            for (int i = 0; i < count; i++)
            {
                playerItems.Add(result.Inventory[i]);
            }
            UpdateInventoryUI();
        },
        error =>
        {
            Debug.LogError("�κ��丮 �ε� ����: " + error.GenerateErrorReport());
        });
    }

    private void UpdateInventoryUI()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            GameObject slotObj = itemSlots[i];
            if (slotObj == null) continue;

            Transform iconTransform = slotObj.transform.Find("IconImg");
            Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            Button slotButton = slotObj.GetComponent<Button>();

            if (i < playerItems.Count)
            {
                string itemId = playerItems[i].ItemId;
                Item matchedItem = System.Array.Find(itemDatabase, item => item.itemId == itemId);

                if (matchedItem != null && iconImage != null)
                {
                    iconImage.sprite = matchedItem.icon;
                    iconImage.enabled = true;
                }
                else if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                    Debug.LogWarning($"ItemDatabase�� '{itemId}'�� �ش��ϴ� �������� ���ų�, �������� �Ҵ���� �ʾҽ��ϴ�.");
                }

                if (slotButton != null)
                {
                    slotButton.interactable = true;
                }
            }
            else
            {
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }
                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.interactable = false;
                }
            }
        }
    }
}
