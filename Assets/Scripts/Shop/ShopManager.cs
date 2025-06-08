using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("���� ���� ����")]
    public GameObject[] shopSlots; // 16���� ���� ����
    [Header("���� �г�")]
    public GameObject tooltipPanel; // ���� UI �г�
    public TextMeshProUGUI tooltipText; // ���� �ؽ�Ʈ
    [Header("�ε� �г�")]
    public GameObject shopLoadingPanel;

    [Header("�÷��̾� ��� ǥ��")]
    public TextMeshProUGUI goldText; // ��� ��ġ ǥ�� �ؽ�Ʈ

    [Header("���� UI")]
    public Button buyButton; // ���� ��ư
    
    

    private CatalogItem selectedItem; // ���� ���õ� ������

    void Start()
    {
        if (shopLoadingPanel != null)
            shopLoadingPanel.SetActive(false);
        LoadShopItems();
        LoadGold();
        buyButton.onClick.AddListener(BuySelectedItem);
        buyButton.interactable = false;
    }

    #region ���� �ε�

    // ���� ������ �ҷ�����
    void LoadShopItems()
    {
        // �ҷ����� ���� �� �ε� �г� �ѱ�
        if (shopLoadingPanel != null)
            shopLoadingPanel.SetActive(true);

        PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest
        {
            CatalogVersion = "1.0"
        }, result =>
        {
            // --- ���� �ݹ� ---
            List<CatalogItem> items = result.Catalog;

            int i = 0;
            for (; i < shopSlots.Length && i < items.Count; i++)
                AssignToSlot(shopSlots[i], items[i]);

            for (; i < shopSlots.Length; i++)
                ClearSlot(shopSlots[i]);

            // �ε� ������ �г� ����
            if (shopLoadingPanel != null)
                shopLoadingPanel.SetActive(false);
        }, error =>
        {
            Debug.LogError("īŻ�α� �ε� ����: " + error.GenerateErrorReport());
            // ���� �ÿ��� �г� ����
            if (shopLoadingPanel != null)
                shopLoadingPanel.SetActive(false);
        });
    }


    // ���Կ� ������ �Ҵ�
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

    // ������ �ʴ� ���� �ʱ�ȭ
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

    #region ��� �ҷ�����

    // ���� ��� ���� �ҷ�����
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
            Debug.LogError("��� �ε� ����: " + error.GenerateErrorReport());
            goldText.text = "0";
        });
    }

    #endregion

    #region ������ ����

    // ������ ������ ����
    void BuySelectedItem()
    {
        if (selectedItem == null)
            return;

        if (!selectedItem.VirtualCurrencyPrices.ContainsKey("GD"))
        {
            ShowResult("�� �������� ������ �� �����ϴ�.");
            return;
        }

        int price = (int)selectedItem.VirtualCurrencyPrices["GD"];

        
        tooltipText.text = "���� �������Դϴ�...";
        buyButton.interactable = false;

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            int currentGold = result.VirtualCurrency.ContainsKey("GD") ? result.VirtualCurrency["GD"] : 0;

            if (currentGold < price)
            {
                ShowResult("�ݾ��� �����մϴ�.");
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
                ShowResult("���Ű� �Ϸ�Ǿ����ϴ�.");
                LoadGold();
                buyButton.interactable = false; 
                selectedItem = null;
            },
            error =>
            {
                Debug.LogError("���� ����: " + error.GenerateErrorReport());
                ShowResult("�ݾ��� �����մϴ�.");
                buyButton.interactable = true; // ���� �� �ٽ� ���� �� �ֵ���
            });
        },
        error =>
        {
            Debug.LogError("�κ��丮 Ȯ�� ����: " + error.GenerateErrorReport());
            ShowResult("���� �� ������ �߻��߽��ϴ�.");
            buyButton.interactable = true;
        });
    }


    // ���� ��� �޽��� ǥ��
    void ShowResult(string message)
    {
        
        tooltipText.text = message;
    }

    #endregion
}
