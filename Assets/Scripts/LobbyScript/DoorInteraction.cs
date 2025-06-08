using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class DoorInteraction : MonoBehaviourPunCallbacks
{
    public enum DoorType
    {
        Lobby,          // �ʴ� �ڵ� �� ����/����
        Matchmaking,    // ��ġ����ŷ
        Shop,
        Item
    }

    [Header("Door Settings")]
    public DoorType doorType;
    public string playerTag = "Player";   // �÷��̾� ������Ʈ�� ���� �±�

    [Header("UI References (DoorInteraction)")]
    public TextMeshProUGUI connectionInfoText;
    // LobbyManager�� connectionInfoText�� �����ص� �ǰ�, ������ ���� �ؽ�Ʈ�� �ᵵ �˴ϴ�.

    [Header("Panels (�� ������)")]
    public GameObject lobbyCreatePanel;   // Lobby ������ ���� �г�
    public GameObject shopPanel;          // Shop ������ ���� �г�
    public GameObject itemPanel;          // Item ������ ���� �г�
    // Matchmaking ���� �г��� �����Ƿ� Inspector������ �Ҵ����� �ʽ��ϴ�.

    [Header("Lobby Panel (�ʴ� �ڵ� ����)")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button closeButton;
    public TMP_InputField inviteCodeInput;

    [Header("References")]
    public LobbyManager lobbyManager;     // ���� �ִ� LobbyManager�� Inspector���� �巡���Ͽ� ����

    private string currentInviteCode = "";
    private bool _playerInRange = false;

    private void Awake()
    {
        // �κ� ��(LobbyOnly)������ ��ư �����ʸ� ���
        if (doorType == DoorType.Lobby)
        {
            createRoomButton.onClick.AddListener(CreateInviteRoom);
            joinRoomButton.onClick.AddListener(JoinInviteRoom);
            closeButton.onClick.AddListener(CloseLobbyPanel);
        }
    }

    private void Start()
    {
        // ���� �� ��� �г��� ��Ȱ��ȭ
        if (lobbyCreatePanel != null) lobbyCreatePanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (itemPanel != null) itemPanel.SetActive(false);

        // Inspector�� LobbyManager�� �Ҵ����� �ʾҴٸ� �ڵ����� ã�� ����
        if (lobbyManager == null)
        {
            lobbyManager = Object.FindAnyObjectByType<LobbyManager>();
            if (lobbyManager == null)
            {
                Debug.LogError("DoorInteraction: LobbyManager�� ã�� �� �����ϴ�! ���� LobbyManager�� �־�� �մϴ�.");
            }
        }
    }
    private void Update()
    {
        if (!_playerInRange) return;

        // �� ȭ��ǥ �Ǵ� W Ű�� ������ ��
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
        // �±װ� playerTag�� �ƴ� ��� ����
        if (!other.CompareTag(playerTag)) return;

        // ���� ����Ű�� ������ ��ȣ�ۿ�
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            OpenOrInteract();
        }
    }

    /// <summary>
    /// doorType�� ���� �г��� �Ѱų� ��ġ����ŷ�� ��û�մϴ�.
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

    #region ������ �κ� �ʴ� �ڵ� ���� ������

    private void CloseLobbyPanel()
    {
        if (lobbyCreatePanel != null)
            lobbyCreatePanel.SetActive(false);
    }

    /// <summary>
    /// Lobby ������ ���� ������ ��ư�� Ŭ������ �� ȣ��˴ϴ�.
    /// </summary>
    private void CreateInviteRoom()
    {
        // Photon�� Ready �������� Ȯ��
        if (lobbyManager == null || !PhotonNetwork.IsConnectedAndReady)
        {
            connectionInfoText.text = "��������: �κ� ���� �Ұ�\n�翬�� �õ� ��...";
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
        Debug.Log($"[�ʴ� �ڵ�] �� ���� �ʴ� �ڵ��: {currentInviteCode}");
    }

    /// <summary>
    /// Lobby ������ ���� ���塱 ��ư�� Ŭ������ �� ȣ��˴ϴ�.
    /// </summary>
    private void JoinInviteRoom()
    {
        if (lobbyManager == null || !PhotonNetwork.IsConnectedAndReady)
        {
            connectionInfoText.text = "��������: �ʴ� �ڵ� ���� �Ұ�\n�翬�� �õ� ��...";
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
            Debug.LogWarning("�ʴ� �ڵ带 �Է��ϼ���.");
            connectionInfoText.text = "�ʴ� �ڵ带 �Է��ϼ���.";
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"�� ���� ����: {message}");
        connectionInfoText.text = "�ʴ�� ���� �������� �ʰų� �ο��� ���� á���ϴ�.";
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
