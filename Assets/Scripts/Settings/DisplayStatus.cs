using TMPro;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class DisplayStatus : MonoBehaviour
{
    public PlayerStat playerStat;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI goldText; 

    void Start()
    {
        UpdateUI();
        FetchPlayerName();
        FetchGold(); 
    }

    public void UpdateUI()
    {
        if (playerStat != null)
        {
            hpText.text = $"{playerStat.hp}";
            powerText.text = $"{playerStat.power}";
        }
    }

    void FetchPlayerName()
    {
        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            string displayName = result.AccountInfo.TitleInfo.DisplayName;
            nameText.text = displayName;
            Debug.Log("DisplayName: " + displayName);
        },
        error =>
        {
            Debug.LogError("Failed to get DisplayName: " + error.GenerateErrorReport());
            nameText.text = "Unknown";
        });
    }

    void FetchGold()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            if (result.VirtualCurrency.ContainsKey("GD"))
            {
                int goldAmount = result.VirtualCurrency["GD"];
                goldText.text = goldAmount.ToString(); 
            }
        },
        error =>
        {
            Debug.LogError("Failed to get gold: " + error.GenerateErrorReport());
            goldText.text = "0";
        });
    }
}
