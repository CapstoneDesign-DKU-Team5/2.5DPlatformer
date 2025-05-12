using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro; // 추가
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
    public GameObject playerPrefab;
    public TextMeshProUGUI inviteCodeText;
    

    [Header("UI (GameScene)")]
    public Button startButton; 
    public Button readyButton;

    [Header("UI Setting (GameScene)")]
    [SerializeField]
    private bool isReady = false;

    private int height = 0;
    public bool isGameover { get; private set; }

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
    }

   

    private void Start()
    {
        // 플레이어 스폰 위치 결정
        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? new Vector3(0f, 3f, -3f)
            : new Vector3(-1.5f, 3f, -3f);

        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);

        ShowInviteCode();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom();
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

    private void OnClickReady()
    {
        isReady = !isReady;
        // 버튼 텍스트 토글 (예: "준비 완료" ↔ "준비 취소")
        readyButton.GetComponentInChildren<TextMeshProUGUI>().text
            = isReady ? "준비 취소" : "준비 완료";

        // MasterClient 쪽 StartButton 활성화 상태 RPC 요청
        photonView.RPC(nameof(RPC_SetStartInteractable), RpcTarget.MasterClient, isReady);
    }

    private void OnClickStart()
    {
        // 모두에게 컨트롤 허용 RPC
        photonView.RPC(nameof(RPC_EnablePlayerControl), RpcTarget.AllBuffered);
        startButton.interactable = false; // 다시 꺼두기
    }

    [PunRPC]
    private void RPC_SetStartInteractable(bool canStart)
    {
        // MasterClient만 통과하므로 바로 처리
        startButton.interactable = canStart;
    }

    [PunRPC]
    private void RPC_EnablePlayerControl()
    {
        // 모든 NetworkPlayer에 입력 허용
        foreach (var np in FindObjectsOfType<NetworkPlayer>())
            np.EnableControl();
    }




}