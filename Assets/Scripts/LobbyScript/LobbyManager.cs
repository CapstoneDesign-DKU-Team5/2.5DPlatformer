using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private string gameVersion = "1.0";

    [Header("UI References")]
    public TextMeshProUGUI connectionInfoText;
    public Button matchmakingButton;
    public Button lobbyButton;

    [Header("Lobby Panel")]
    public GameObject lobbyCreatePanel;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button closeButton;
    public TMP_InputField inviteCodeInput;
    

    private string currentInviteCode = "";

    private void Awake()
    {
        matchmakingButton.onClick.AddListener(Connect);
        lobbyButton.onClick.AddListener(OpenLobbyPanel);
        createRoomButton.onClick.AddListener(CreateInviteRoom);
        joinRoomButton.onClick.AddListener(JoinInviteRoom);
        closeButton.onClick.AddListener(CloseLobbyPanel);
    }

    private void Start()
    {
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        matchmakingButton.interactable = false;
        connectionInfoText.text = "������ ������ ���� ��...";
        lobbyCreatePanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        matchmakingButton.interactable = true;
        connectionInfoText.text = "�¶���: ������ ������ �����";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        matchmakingButton.interactable = false;

        connectionInfoText.text = "��������: ������ ������ ������� ����\n���� ��õ���...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Connect()
    {
        matchmakingButton.interactable = false;

        if (PhotonNetwork.IsConnected)
        {
            connectionInfoText.text = "���� ������ ������ ã����...";
            var expectedProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", false } };
            PhotonNetwork.JoinRandomRoom(expectedProperties, 0);
        }
        else
        {
            connectionInfoText.text = "��������: ������ ������ ������� ���� \n���� ��õ� ��...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Matchmaking�� �븸 ����
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2, CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", false } }, CustomRoomPropertiesForLobby = new[] { "inviteOnly" } });
    }

    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "�� ���� ����";
        PhotonNetwork.LoadLevel("GameScene");
    }

    /// <summary>
    /// �κ� �г� ����
    /// </summary>
    private void OpenLobbyPanel()
    {
        lobbyCreatePanel.SetActive(true);
    }

    /// <summary>
    /// �ʴ� �ڵ� ��� �� ����
    /// </summary>
    private void CreateInviteRoom()
    {
        currentInviteCode = GenerateInviteCode();
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", true }, { "code", currentInviteCode } },
            CustomRoomPropertiesForLobby = new[] { "inviteOnly", "code" }
        };

        PhotonNetwork.CreateRoom(currentInviteCode, options);

        Debug.Log($"[�ʴ� �ڵ�] �� ���� �ʴ� �ڵ��: {currentInviteCode}");
    }

    /// <summary>
    /// �ʴ� �ڵ� ��� �� ���� �õ�
    /// </summary>
    private void JoinInviteRoom()
    {
        string inputCode = inviteCodeInput.text.Trim();
        if (!string.IsNullOrEmpty(inputCode))
        {
            PhotonNetwork.JoinRoom(inputCode);
        }
        else
        {
            Debug.LogWarning("�ʴ� �ڵ带 �Է��ϼ���.");
            connectionInfoText.text = "�ʴ� �ڵ带 �Է��ϼ���.";
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"�� ���� ����: {message}");
        connectionInfoText.text = "�ʴ�� ���� �������� �ʰų� �ο��� ���� á���ϴ�.";
    }

    /// <summary>
    /// ������ ���� �ʴ��ڵ� ������ (��: 6�ڸ� �빮��/����)
    /// </summary>
    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        return new string(code);
    }

    private void CloseLobbyPanel()
    {
        lobbyCreatePanel.SetActive(false);
    }
}
