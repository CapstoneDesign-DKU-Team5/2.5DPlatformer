using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro; // 추가

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
    public TextMeshProUGUI inviteCodeText;
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

        GameObject prefabToSpawn = PhotonNetwork.IsMasterClient
            ? masterClientPrefab
            : otherClientPrefab;
        // 플레이어 스폰 위치 결정
        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? new Vector3(0f, 3f, -3f)
            : new Vector3(-1.5f, 3f, -3f);

        PhotonNetwork.Instantiate(prefabToSpawn.name, spawnPosition, Quaternion.identity);

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



}