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
        connectionInfoText.text = "마스터 서버에 접속 중...";
        lobbyCreatePanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        matchmakingButton.interactable = true;
        connectionInfoText.text = "온라인: 마스터 서버에 연결됨";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        matchmakingButton.interactable = false;

        connectionInfoText.text = "오프라인: 마스터 서버와 연결되지 않음\n접속 재시도중...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Connect()
    {
        matchmakingButton.interactable = false;

        if (PhotonNetwork.IsConnected)
        {
            connectionInfoText.text = "참가 가능한 게임을 찾는중...";
            var expectedProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", false } };
            PhotonNetwork.JoinRandomRoom(expectedProperties, 0);
        }
        else
        {
            connectionInfoText.text = "오프라인: 마스터 서버와 연결되지 않음 \n접속 재시도 중...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Matchmaking용 룸만 생성
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2, CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", false } }, CustomRoomPropertiesForLobby = new[] { "inviteOnly" } });
    }

    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "방 참가 성공";
        PhotonNetwork.LoadLevel("GameScene");
    }

    /// <summary>
    /// 로비 패널 열기
    /// </summary>
    private void OpenLobbyPanel()
    {
        lobbyCreatePanel.SetActive(true);
    }

    /// <summary>
    /// 초대 코드 기반 룸 생성
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

        Debug.Log($"[초대 코드] 이 방의 초대 코드는: {currentInviteCode}");
    }

    private void JoinInviteRoom()
    {
        string inputCode = inviteCodeInput.text.Trim();
        if (!string.IsNullOrEmpty(inputCode))
        {
            PhotonNetwork.JoinRoom(inputCode);
        }
        else
        {
            Debug.LogWarning("초대 코드를 입력하세요.");
            connectionInfoText.text = "초대 코드를 입력하세요.";
        }
    }


    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"방 참가 실패: {message}");
        connectionInfoText.text = "초대된 방이 존재하지 않거나 인원이 가득 찼습니다.";
    }

    /// <summary>
    /// 간단한 랜덤 초대코드 생성기 (예: 6자리 대문자/숫자)
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

