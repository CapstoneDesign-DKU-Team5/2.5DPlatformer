using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("상점 슬롯 관련")]
    public GameObject[] shopSlots; // 16개의 상점 슬롯
    [Header("툴팁 패널")]
    public GameObject tooltipPanel; // 툴팁 UI 패널
    public TextMeshProUGUI tooltipText; // 툴팁 텍스트

    [Header("플레이어 골드 표시")]
    public TextMeshProUGUI goldText; // 골드 수치 표시 텍스트

    [Header("구매 UI")]
    public Button buyButton; // 구매 버튼
    
    

    private CatalogItem selectedItem; // 현재 선택된 아이템

    void Start()
    {
        LoadShopItems();
        LoadGold();
        buyButton.onClick.AddListener(BuySelectedItem);
        buyButton.interactable = false;
    }

    #region 상점 로딩

    // 상점 아이템 불러오기
    void LoadShopItems()
    {
        PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest
        {
            CatalogVersion = "1.0"
        }, result =>
        {
            List<CatalogItem> items = result.Catalog;

            int i = 0;
            for (; i < shopSlots.Length && i < items.Count; i++)
            {
                AssignToSlot(shopSlots[i], items[i]);
            }

            for (; i < shopSlots.Length; i++)
            {
                ClearSlot(shopSlots[i]);
            }
        }, error =>
        {
            Debug.LogError("카탈로그 로딩 실패: " + error.GenerateErrorReport());
        });
    }

    // 슬롯에 아이템 할당
    void AssignToSlot(GameObject slot, CatalogItem item)
    {
        Transform iconTransform = slot.transform.Find("IconImg");
        if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
        {
            if (!string.IsNullOrEmpty(item.CustomData))
            {
                var customData = JsonUtility.FromJson<ItemCustomData>(item.CustomData);
                Sprite iconSprite = Resources.Load<Sprite>($"Icons/{customData.Icon}");
                iconImage.sprite = iconSprite;
                iconImage.enabled = iconSprite != null;
            }
        }

        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                selectedItem = item;
                string price = item.VirtualCurrencyPrices.ContainsKey("GD")
                    ? item.VirtualCurrencyPrices["GD"].ToString()
                    : "0";

                tooltipText.text = $"<b>{item.DisplayName}</b> / {price}\n\n{item.Description}";
                tooltipPanel.SetActive(true);
                buyButton.interactable = true;
            });
        }
    }

    // 사용되지 않는 슬롯 초기화
    void ClearSlot(GameObject slot)
    {
        Transform iconTransform = slot.transform.Find("IconImg");
        if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }
    }

    [System.Serializable]
    private class ItemCustomData
    {
        public string Icon;
    }

    #endregion

    #region 골드 불러오기

    // 유저 골드 정보 불러오기
    void LoadGold()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            if (result.VirtualCurrency.ContainsKey("GD"))
            {
                goldText.text = result.VirtualCurrency["GD"].ToString();
            }
            else
            {
                goldText.text = "0";
            }
        },
        error =>
        {
            Debug.LogError("골드 로딩 실패: " + error.GenerateErrorReport());
            goldText.text = "0";
        });
    }

    #endregion

    #region 아이템 구매

    // 선택한 아이템 구매
    void BuySelectedItem()
    {
        if (selectedItem == null)
            return;

        if (!selectedItem.VirtualCurrencyPrices.ContainsKey("GD"))
        {
            ShowResult("이 아이템은 구매할 수 없습니다.");
            return;
        }

        int price = (int)selectedItem.VirtualCurrencyPrices["GD"];

        
        tooltipText.text = "구매 진행중입니다...";
        buyButton.interactable = false;

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            int currentGold = result.VirtualCurrency.ContainsKey("GD") ? result.VirtualCurrency["GD"] : 0;

            if (currentGold < price)
            {
                ShowResult("금액이 부족합니다.");
                buyButton.interactable = true; 
                return;
            }

            PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
            {
                CatalogVersion = "1.0",
                ItemId = selectedItem.ItemId,
                Price = price,
                VirtualCurrency = "GD"
            }, purchaseResult =>
            {
                ShowResult("구매가 완료되었습니다.");
                LoadGold();
                buyButton.interactable = false; 
                selectedItem = null;
            },
            error =>
            {
                Debug.LogError("구매 실패: " + error.GenerateErrorReport());
                ShowResult("금액이 부족합니다.");
                buyButton.interactable = true; // 실패 시 다시 눌릴 수 있도록
            });
        },
        error =>
        {
            Debug.LogError("인벤토리 확인 실패: " + error.GenerateErrorReport());
            ShowResult("구매 시 오류가 발생했습니다.");
            buyButton.interactable = true;
        });
    }


    // 구매 결과 메시지 표시
    void ShowResult(string message)
    {
        
        tooltipText.text = message;
    }

    #endregion
}
