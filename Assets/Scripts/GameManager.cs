using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro; // 추가
using UnityEngine.UI;

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
    [Tooltip("마스터 클라이언트용 플레이어 프리팹 (Resources 폴더 안)")]
    public GameObject masterClientPrefab;
    [Tooltip("일반 클라이언트용 플레이어 프리팹 (Resources 폴더 안)")]
    public GameObject otherClientPrefab;

    [Header("Pause UI")]
    [Tooltip("ESC 시 활성화할 Canvas")]
    public GameObject pauseCanvas;
    [Tooltip("Pause Canvas 안의 Exit Room 버튼")]
    public Button exitButton;

    [Header("UI Canvases")]
    [Tooltip("인게임 UI Canvas")]
    public GameObject uiCanvas;
    [Tooltip("Game Over Canvas")]
    public GameObject gameOverCanvas;

    private int aliveCount;
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
        
        Debug.Log(aliveCount+"명 있음");
    }

   

    private void Start()
    {
        aliveCount = PhotonNetwork.CurrentRoom.PlayerCount;
        // 2) UI 초기 상태
        if (uiCanvas != null) uiCanvas.SetActive(true);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        
        isGameover = false;

        // Exit 버튼에 리스너 추가
        if (exitButton != null)
            exitButton.onClick.AddListener(() => PhotonNetwork.LeaveRoom());

        // 2) 플레이어 스폰
        GameObject prefabToSpawn = PhotonNetwork.IsMasterClient
            ? masterClientPrefab
            : otherClientPrefab;

        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? new Vector3(0f, 3f, -3f)
            : new Vector3(-1.5f, 3f, -3f);

        PhotonNetwork.Instantiate(prefabToSpawn.name, spawnPosition, Quaternion.identity);

        // 3) 초대 코드 표시
        ShowInviteCode();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        aliveCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[GameManager] 플레이어 입장 → aliveCount: {aliveCount}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        aliveCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"[GameManager] 플레이어 퇴장 → aliveCount: {aliveCount}");
        CheckGameOver();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && pauseCanvas != null)
        {
            pauseCanvas.SetActive(!pauseCanvas.activeSelf);
        }
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

        // 최대 인원이면 텍스트 비활성화
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
                : "알 수 없음";

            inviteCodeText.text = $"방 초대 코드: {code}";
        }
        else
        {
            inviteCodeText.text = "방 초대 코드: 없음";
        }
    }



    [PunRPC]
    private void RPC_PlayerDied()
    {
        aliveCount--;
        Debug.Log($"[GameManager] 플레이어 사망 → 남은 aliveCount: {aliveCount}");
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (!isGameover && aliveCount <= 0)
        {
            isGameover = true;
            uiCanvas?.SetActive(false);
            gameOverCanvas?.SetActive(true);
        }
    }
}