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
    public Item[] itemDatabase; // 아이템 ScriptableObject 배열
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
            Debug.LogWarning($"itemSlots 배열 크기를 {maxInventoryCount}로 맞춰주세요. 현재 길이: {(itemSlots == null ? 0 : itemSlots.Length)}");
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
        // 숫자키 1~8 입력 처리
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

            // 슬롯별 uses 초기값 설정
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
            Debug.LogError("인벤토리 로드 실패: " + error.GenerateErrorReport());
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
                // slotUses[i] <= 0 이면 “사라진 슬롯”으로 간주
                if (slotUses[i] <= 0)
                {
                    // 아이콘 숨기기
                    if (iconImage != null)
                        iconImage.enabled = false;

                    // 버튼 비활성화
                    if (slotButton != null)
                    {
                        slotButton.onClick.RemoveAllListeners();
                        slotButton.interactable = false;
                    }

                    continue;
                }

                //==============================
                // 여기가 원래 아이콘을 켜던 구간
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
                    Debug.LogWarning($"ItemDatabase에 '{itemId}'에 해당하는 아이템이 없거나, 아이콘이 할당되지 않았습니다.");
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
                // 원래 “빈 슬롯” 처리
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
                // 슬롯 비활성화
                var slotObj = itemSlots[index];
                var iconImage = slotObj.transform.Find("IconImg")?.GetComponent<Image>();
                var slotButton = slotObj.GetComponent<Button>();
                if (iconImage != null)
                    iconImage.enabled = false;
                if (slotButton != null)
                    slotButton.interactable = false;

                // 인벤토리 리스트에는 남겨두되, 더 이상 사용되지 않는 상태가 됨
            }

            UpdateInventoryUI();
        },
        error =>
        {
            Debug.LogError("아이템 사용 실패: " + error.GenerateErrorReport());
        });
    }

    // 4) Heal / DamageBuff 효과 처리
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

    // 5) HealOverOneMinute: 60초에 걸쳐 healAmount 만큼 회복
    private IEnumerator HealOverOneMinute(int healAmount)
    {
        float totalHeal = healAmount;
        float perSecond = totalHeal / 60f;

        for (int i = 0; i < 60; i++)
        {
            if (targetPlayer != null)
            {
                targetPlayer.ApplyHeal(perSecond); // NetworkPlayer 쪽에 public void ApplyHeal(float amount) 추가 필요
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // 6) ApplyDamageBuff: duration 동안 공격력에 multiplier 곱함
    private IEnumerator ApplyDamageBuff(float multiplier, float duration)
    {
        if (targetPlayer == null)
            yield break;

        int originalPower = targetPlayer.GetPower();   // NetworkPlayer에 public int GetPower() 필요
        int buffedPower = Mathf.RoundToInt(originalPower * multiplier);
        targetPlayer.SetPower(buffedPower);            // NetworkPlayer에 public void SetPower(int newPower) 필요

        yield return new WaitForSeconds(duration);

        targetPlayer.SetPower(originalPower);
    }
}
