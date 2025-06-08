using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using Photon.Pun;

public class Gold : MonoBehaviourPun
{
    public int amount { get; private set; }

    [Header("UI (��Ÿ�ӿ� ã���ϴ�)")]
    public TextMeshProUGUI goldText;

    [Header("Rotation Settings")]
    [SerializeField, Tooltip("��� ������ Y�� ȸ�� �ӵ� (��/��)")]
    private float rotationSpeed = 90f;

    private void Awake()
    {
        // �ν����Ϳ� �� ���Դٸ� ��Ÿ�ӿ� ������ ã�� ����
        if (goldText == null)
        {
            var go = GameObject.FindWithTag("GoldTextUI");
            if (go != null)
                goldText = go.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogWarning("GoldTextUI �±װ� ������ UI �ؽ�Ʈ�� ã�� �� �����ϴ�!");
        }
    }

    private void Update()
    {
        // �� ������ Y�� ȸ��
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void Initialize(int dropAmount)
    {
        amount = dropAmount;
    }

    public void Pickup()
    {
        // 1) ����� Ŭ���̾�Ʈ�� �����Ϳ��Ը� ��û�ϰ� �ٷ� ����
        if (!photonView.IsMine)
        {
            AddVirtualCurrency();
            photonView.RPC(nameof(HandlePickup), RpcTarget.MasterClient);
            return;
        }

        // 2) ���� �����ڶ�� ���� ó��
        HandlePickup();
    }

    [PunRPC]
    private void HandlePickup()
    {
        // ������(������)���� �� ���� ����
        AddVirtualCurrency();                           // ������ Ŭ���̾�Ʈ �ڽ��� PlayFab ����
        photonView.RPC(nameof(RPC_AddCurrencyForOthers), RpcTarget.Others);
        PhotonNetwork.Destroy(photonView);              // �����Ͱ� ������Ʈ �ı�
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
                Debug.LogError("��� �߰� ����: " + error.GenerateErrorReport());
            }
        );
    }

    [PunRPC]
    private void RPC_AddCurrencyForOthers()
    {
        // �ٸ� Ŭ���̾�Ʈ(�񸶽���) ���ǿ����� PlayFab ����
        AddVirtualCurrency();
    }
}
