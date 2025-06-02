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

    void Start()
    {
        UpdateUI();
        FetchPlayerName(); 
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
}
