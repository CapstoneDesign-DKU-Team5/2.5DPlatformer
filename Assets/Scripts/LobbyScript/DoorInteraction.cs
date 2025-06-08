using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class DoorInteraction : MonoBehaviourPunCallbacks
{
    public enum DoorType
    {
        Lobby,          // 초대 코드 룸 생성/입장
        Matchmaking,    // 매치메이킹
        Shop,
        Item
    }

    [Header("Door Settings")]
    public DoorType doorType;
    public string playerTag = "Player";   // 플레이어 오브젝트에 붙은 태그

    [Header("UI References (DoorInteraction)")]
    public TextMeshProUGUI connectionInfoText;
    // LobbyManager의 connectionInfoText를 공유해도 되고, 문마다 별도 텍스트를 써도 됩니다.

    [Header("Panels (문 종류별)")]
    public GameObject lobbyCreatePanel;   // Lobby 문에서 열릴 패널
    public GameObject shopPanel;          // Shop 문에서 열릴 패널
    public GameObject itemPanel;          // Item 문에서 열릴 패널
    // Matchmaking 문은 패널이 없으므로 Inspector에서는 할당하지 않습니다.

    [Header("Lobby Panel (초대 코드 전용)")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button closeButton;
    public TMP_InputField inviteCodeInput;

    [Header("References")]
    public LobbyManager lobbyManager;     // 씬에 있는 LobbyManager를 Inspector에서 드래그하여 연결

    private string currentInviteCode = "";
    private bool _playerInRange = false;

    private void Awake()
    {
        // 로비 문(LobbyOnly)에서만 버튼 리스너를 등록
        if (doorType == DoorType.Lobby)
        {
            createRoomButton.onClick.AddListener(CreateInviteRoom);
            joinRoomButton.onClick.AddListener(JoinInviteRoom);
            closeButton.onClick.AddListener(CloseLobbyPanel);
        }
    }

    private void Start()
    {
        // 시작 시 모든 패널을 비활성화
        if (lobbyCreatePanel != null) lobbyCreatePanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (itemPanel != null) itemPanel.SetActive(false);

        // Inspector에 LobbyManager를 할당하지 않았다면 자동으로 찾아 연결
        if (lobbyManager == null)
        {
            lobbyManager = Object.FindAnyObjectByType<LobbyManager>();
            if (lobbyManager == null)
            {
                Debug.LogError("DoorInteraction: LobbyManager를 찾을 수 없습니다! 씬에 LobbyManager가 있어야 합니다.");
            }
        }
    }
    private void Update()
    {
        if (!_playerInRange) return;

        // 위 화살표 또는 W 키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            OpenOrInteract();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerInRange = false;
    }
    private void OnTriggerStay(Collider other)
    {
        // 태그가 playerTag가 아닌 경우 무시
        if (!other.CompareTag(playerTag)) return;

        // 위쪽 방향키를 누르면 상호작용
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            OpenOrInteract();
        }
    }

    /// <summary>
    /// doorType에 따라 패널을 켜거나 매치메이킹을 요청합니다.
    /// </summary>
    public void OpenOrInteract()
    {
        switch (doorType)
        {
            case DoorType.Lobby:
                if (lobbyCreatePanel != null)
                    lobbyCreatePanel.SetActive(true);
                break;

            case DoorType.Matchmaking:
                if (lobbyManager != null)
                    lobbyManager.StartMatchmaking();
                break;

            case DoorType.Shop:
                if (shopPanel != null)
                    shopPanel.SetActive(true);
                break;

            case DoorType.Item:
                if (itemPanel != null)
                    itemPanel.SetActive(true);
                break;
        }
    }

    #region ─── 로비 초대 코드 관련 ───

    private void CloseLobbyPanel()
    {
        if (lobbyCreatePanel != null)
            lobbyCreatePanel.SetActive(false);
    }

    /// <summary>
    /// Lobby 문에서 “방 생성” 버튼을 클릭했을 때 호출됩니다.
    /// </summary>
    private void CreateInviteRoom()
    {
        // Photon이 Ready 상태인지 확인
        if (lobbyManager == null || !PhotonNetwork.IsConnectedAndReady)
        {
            connectionInfoText.text = "오프라인: 로비 생성 불가\n재연결 시도 중...";
            lobbyManager?.ConnectToPhoton();
            return;
        }

        currentInviteCode = GenerateInviteCode();
        var options = new RoomOptions
        {
            MaxPlayers = 2,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", true }, { "code", currentInviteCode } },
            CustomRoomPropertiesForLobby = new[] { "inviteOnly", "code" }
        };

        PhotonNetwork.CreateRoom(currentInviteCode, options);
        Debug.Log($"[초대 코드] 이 방의 초대 코드는: {currentInviteCode}");
    }

    /// <summary>
    /// Lobby 문에서 “방 입장” 버튼을 클릭했을 때 호출됩니다.
    /// </summary>
    private void JoinInviteRoom()
    {
        if (lobbyManager == null || !PhotonNetwork.IsConnectedAndReady)
        {
            connectionInfoText.text = "오프라인: 초대 코드 참가 불가\n재연결 시도 중...";
            lobbyManager?.ConnectToPhoton();
            return;
        }

        string inputCode = inviteCodeInput.text.Trim().ToUpper();
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

    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = chars[random.Next(chars.Length)];
        return new string(code);
    }

    #endregion
}
