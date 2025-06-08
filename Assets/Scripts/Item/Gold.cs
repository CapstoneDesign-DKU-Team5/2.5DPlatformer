using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using Photon.Pun;

public class Gold : MonoBehaviourPun
{
    public int amount { get; private set; }

    [Header("UI (런타임에 찾습니다)")]
    public TextMeshProUGUI goldText;

    [Header("Rotation Settings")]
    [SerializeField, Tooltip("골드 코인의 Y축 회전 속도 (도/초)")]
    private float rotationSpeed = 90f;

    private void Awake()
    {
        // 인스펙터에 안 들어왔다면 런타임에 씬에서 찾아 연결
        if (goldText == null)
        {
            var go = GameObject.FindWithTag("GoldTextUI");
            if (go != null)
                goldText = go.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogWarning("GoldTextUI 태그가 설정된 UI 텍스트를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        // 매 프레임 Y축 회전
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void Initialize(int dropAmount)
    {
        amount = dropAmount;
    }

    public void Pickup()
    {
        if (!photonView.IsMine) return;

        AddVirtualCurrency();
        photonView.RPC(nameof(RPC_AddCurrencyForOthers), RpcTarget.Others);
        PhotonNetwork.Destroy(photonView);
    }

    private void AddVirtualCurrency()
    {
        PlayFabClientAPI.AddUserVirtualCurrency(
            new AddUserVirtualCurrencyRequest
            {
                VirtualCurrency = "GD",
                Amount = amount
            },
            result =>
            {
                if (goldText != null)
                    goldText.text = result.Balance.ToString();
            },
            error =>
            {
                Debug.LogError("골드 추가 실패: " + error.GenerateErrorReport());
            }
        );
    }

    [PunRPC]
    private void RPC_AddCurrencyForOthers()
    {
        AddVirtualCurrency();
    }
}
