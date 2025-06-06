using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using Photon.Pun;
using HelloWorld;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public float damagePerSecond = 10f;
    public float damageInterval = 1f;
    public int maxInventoryCount = 8;
    public GameObject[] itemSlots = new GameObject[8];
    public Item[] itemDatabase; // ������ ScriptableObject �迭
    private int[] slotUses;
    
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
        slotUses = new int[maxInventoryCount];
    }

    private void Start()
    {
        InvokeRepeating(nameof(ApplyDamage), damageInterval, damageInterval);
        LoadInventory();
    }

    private void Update()
    {
        // ����Ű 1~8 �Է� ó��
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                UseItem(i);
            }
        }
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
            var seenItemIds = new HashSet<string>();

            foreach (var instance in result.Inventory)
            {
                if (seenItemIds.Contains(instance.ItemId))
                    continue;

                seenItemIds.Add(instance.ItemId);
                playerItems.Add(instance);

                if (seenItemIds.Count >= maxInventoryCount)
                    break;
            }

            // ���Ժ� uses �ʱⰪ ����
            for (int i = 0; i < playerItems.Count; i++)
            {
                var matchedItem = System.Array.Find(itemDatabase, item => item.itemId == playerItems[i].ItemId);
                slotUses[i] = matchedItem != null ? matchedItem.usesPerItem : 0;
            }
            for (int i = playerItems.Count; i < maxInventoryCount; i++)
            {
                slotUses[i] = 0;
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
                // slotUses[i] <= 0 �̸� ������� ���ԡ����� ����
                if (slotUses[i] <= 0)
                {
                    // ������ �����
                    if (iconImage != null)
                        iconImage.enabled = false;

                    // ��ư ��Ȱ��ȭ
                    if (slotButton != null)
                    {
                        slotButton.onClick.RemoveAllListeners();
                        slotButton.interactable = false;
                    }

                    continue;
                }

                //==============================
                // ���Ⱑ ���� �������� �Ѵ� ����
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
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.onClick.AddListener(() => UseItem(i));
                }
                //==============================
            }
            else
            {
                // ���� ���� ���ԡ� ó��
                if (iconImage != null)
                    iconImage.sprite = null;
                if (iconImage != null)
                    iconImage.enabled = false;

                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.interactable = false;
                }
            }
        }
    }

    private void UseItem(int index)
    {
        if (index < 0 || index >= playerItems.Count) return;

        ItemInstance instance = playerItems[index];
        Item matchedItem = System.Array.Find(itemDatabase, item => item.itemId == instance.ItemId);

        if (matchedItem == null || !matchedItem.consumable) return;

        if (slotUses[index] <= 0) return;

        var request = new ConsumeItemRequest
        {
            ItemInstanceId = instance.ItemInstanceId,
            ConsumeCount = 1
        };

        PlayFabClientAPI.ConsumeItem(request, result =>
        {
            slotUses[index]--;

            ApplyItemEffect(matchedItem);

            if (slotUses[index] <= 0)
            {
                // ���� ��Ȱ��ȭ
                var slotObj = itemSlots[index];
                var iconImage = slotObj.transform.Find("IconImg")?.GetComponent<Image>();
                var slotButton = slotObj.GetComponent<Button>();
                if (iconImage != null)
                    iconImage.enabled = false;
                if (slotButton != null)
                    slotButton.interactable = false;

                // �κ��丮 ����Ʈ���� ���ܵε�, �� �̻� ������ �ʴ� ���°� ��
            }

            UpdateInventoryUI();
        },
        error =>
        {
            Debug.LogError("������ ��� ����: " + error.GenerateErrorReport());
        });
    }

    // 4) Heal / DamageBuff ȿ�� ó��
    private void ApplyItemEffect(Item item)
    {
        switch (item.effectType)
        {
            case ItemEffectType.Heal:
                StartCoroutine(HealOverOneMinute(item.healAmount));
                break;

            case ItemEffectType.DamageBuff:
                StartCoroutine(ApplyDamageBuff(item.damageMultiplier, item.buffDuration));
                break;

            default:
                break;
        }
    }

    // 5) HealOverOneMinute: 60�ʿ� ���� healAmount ��ŭ ȸ��
    private IEnumerator HealOverOneMinute(int healAmount)
    {
        float totalHeal = healAmount;
        float perSecond = totalHeal / 60f;

        for (int i = 0; i < 60; i++)
        {
            if (targetPlayer != null)
            {
                targetPlayer.ApplyHeal(perSecond); // NetworkPlayer �ʿ� public void ApplyHeal(float amount) �߰� �ʿ�
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // 6) ApplyDamageBuff: duration ���� ���ݷ¿� multiplier ����
    private IEnumerator ApplyDamageBuff(float multiplier, float duration)
    {
        if (targetPlayer == null)
            yield break;

        int originalPower = targetPlayer.GetPower();   // NetworkPlayer�� public int GetPower() �ʿ�
        int buffedPower = Mathf.RoundToInt(originalPower * multiplier);
        targetPlayer.SetPower(buffedPower);            // NetworkPlayer�� public void SetPower(int newPower) �ʿ�

        yield return new WaitForSeconds(duration);

        targetPlayer.SetPower(originalPower);
    }
}
