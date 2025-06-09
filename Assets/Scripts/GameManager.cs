using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro; // �߰�
using UnityEngine.UI;
using HelloWorld;

public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameManager instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindAnyObjectByType<GameManager>();
            }
            return m_instance;
        }
    }

    private static GameManager m_instance;

    [Header("Player & Room Info")]
    [Tooltip("������ Ŭ���̾�Ʈ�� �÷��̾� ������ (Resources ���� ��)")]
    public GameObject masterClientPrefab;
    [Tooltip("�Ϲ� Ŭ���̾�Ʈ�� �÷��̾� ������ (Resources ���� ��)")]
    public GameObject otherClientPrefab;

    [Header("Pause UI")]
    [Tooltip("ESC �� Ȱ��ȭ�� Canvas")]
    public GameObject pauseCanvas;
    [Tooltip("Pause Canvas ���� Exit Room ��ư")]
    public Button exitButton;

    [Header("UI Canvases")]
    [Tooltip("�ΰ��� UI Canvas")]
    public GameObject uiCanvas;
    [Tooltip("Game Over Canvas")]
    public GameObject gameOverCanvas;
    public Button gameOverButton;

    [Header("Clear Blocks")]
    [SerializeField] private ClearBlock[] clearBlocks;
    [Tooltip("Game Clear Canvas")]
    public GameObject gameClearCanvas;
    public TextMeshProUGUI gameClearText;
    public Button gameClearButton;

    private bool isGameCleared = false;

    private int aliveCount;

    private float clearTimer = 0f;
    private bool timerRunning = false;

    public bool isGameover { get; private set; }

    public TextMeshProUGUI inviteCodeText;
    private int height = 0;


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(height);
        }
        else
        {
            height = (int)stream.ReceiveNext();
        }
    }

    private void Awake()
    {
        if (instance != this)
        {
            Destroy(gameObject);
        }
        
        Debug.Log(aliveCount+"�� ����");
    }

   

    private void Start()
    {
        aliveCount = PhotonNetwork.CurrentRoom.PlayerCount;
        // 2) UI �ʱ� ����
        if (uiCanvas != null) uiCanvas.SetActive(true);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        
        isGameover = false;

        // Exit ��ư�� ������ �߰�
        if (exitButton != null)
            exitButton.onClick.AddListener(() => PhotonNetwork.LeaveRoom());

        if (gameClearButton !=null)
            gameClearButton.onClick.AddListener(() => PhotonNetwork.LeaveRoom());

        if (gameOverButton != null)
            gameOverButton.onClick.AddListener(() => PhotonNetwork.LeaveRoom());

        // 2) �÷��̾� ����
        GameObject prefabToSpawn = PhotonNetwork.IsMasterClient
            ? masterClientPrefab
            : otherClientPrefab;

        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? new Vector3(0f, 3f, -3f)
            : new Vector3(-1.5f, 3f, -3f);

        PhotonNetwork.Instantiate(prefabToSpawn.name, spawnPosition, Quaternion.identity);

        // 3) �ʴ� �ڵ� ǥ��
        ShowInviteCode();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        aliveCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[GameManager] �÷��̾� ���� �� aliveCount: {aliveCount}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        aliveCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[GameManager] �÷��̾� ���� �� aliveCount: {aliveCount}");
        CheckGameOver();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && pauseCanvas != null)
        {
            pauseCanvas.SetActive(!pauseCanvas.activeSelf);
        }

        // �� �� �� �̻� �����ϸ� Ÿ�̸� ����
        if (!timerRunning && aliveCount >= 2 && !isGameCleared)
        {
            timerRunning = true;
            clearTimer = 0f;
        }

        if (timerRunning)
            clearTimer += Time.deltaTime;
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
    }


    private void ShowInviteCode()
    {
        if (inviteCodeText == null || !PhotonNetwork.InRoom)
            return;

        Room currentRoom = PhotonNetwork.CurrentRoom;

        // �ִ� �ο��̸� �ؽ�Ʈ ��Ȱ��ȭ
        if (currentRoom.PlayerCount >= currentRoom.MaxPlayers)
        {
            inviteCodeText.gameObject.SetActive(false);
            return;
        }

        if (currentRoom.CustomProperties.TryGetValue("inviteOnly", out object isInviteOnly) &&
            (bool)isInviteOnly)
        {
            string code = currentRoom.CustomProperties.TryGetValue("code", out object codeValue)
                ? codeValue.ToString()
                : "�� �� ����";

            inviteCodeText.text = $"�� �ʴ� �ڵ�: {code}";
        }
        else
        {
            inviteCodeText.text = "";
        }
    }



    [PunRPC]
    private void RPC_PlayerDied()
    {
        aliveCount--;
        Debug.Log($"[GameManager] �÷��̾� ��� �� ���� aliveCount: {aliveCount}");
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (!isGameover && aliveCount <= 1)
        {
            isGameover = true;
            uiCanvas?.SetActive(false);
            gameOverCanvas?.SetActive(true);
        }
    }


    public void CheckGameClear()
    {
        if (isGameCleared) return;

        bool allCorrect = true;

        foreach (var block in clearBlocks)
        {
            if (!block.isCorrectPlayerOnBlock)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            isGameCleared = true;
            Debug.Log("[GameManager] ��� ���� ���� �� ���� Ŭ����!");
            photonView.RPC(nameof(RPC_GameClear), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_GameClear()
    {
        Debug.Log("[GameManager] ���� Ŭ����!");
        uiCanvas?.SetActive(false);
        gameClearCanvas?.SetActive(true);
        // ���⿡ Ŭ���� ����/�ִϸ��̼� � �߰� ����
        // �� Ÿ�̸Ӹ� ��:��:�� �������� �����ؼ� Text�� ����
        int totalSeconds = Mathf.FloorToInt(clearTimer);
        int h = totalSeconds / 3600;
        int m = (totalSeconds % 3600) / 60;
        int s = totalSeconds % 60;
        gameClearText.text = $"{h:00}:{m:00}:{s:00}";

        foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            player.photonView.RPC(nameof(NetworkPlayer.RPC_AllTriggerClear), RpcTarget.AllBuffered);
        }
    }
}