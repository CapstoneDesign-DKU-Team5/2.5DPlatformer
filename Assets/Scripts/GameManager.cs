using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    // 싱글턴 인스턴스
    public static GameManager Instance;

    public GameObject playerPrefab; // 플레이어 프리팹

    private int Height = 0;
    public bool isGameOVer { get; private set; } // 게임오버 상태

    private void Awake()
    {
        // 싱글턴 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
        }
        else
        {
            Destroy(gameObject); // 중복 제거
        }
    }

    private void Start()
    {
        // 플레이어 스폰 위치 결정
        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? new Vector3(0f, 3f, -3f)
            : new Vector3(-1.5f, 3f, -3f);

        // 네트워크로 플레이어 생성
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
    }

    public void UpdateHeight()
    {
        // 로컬 플레이어의 y값을 기준으로 높이 갱신
        GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer != null)
        {
            Height = Mathf.Max(Height, Mathf.RoundToInt(localPlayer.transform.position.y));

            // UIManager.instance.UpdateHeightText(Height); // UIManager가 존재하지 않으므로 주석 처리
        }
    }

    public void EndGame()
    {
        isGameOVer = true;
        // 게임 오버 처리 로직 (추후 구현)
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom(); // 방 나가기
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    // 동기화 처리
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(Height);
        }
        else
        {
            Height = (int)stream.ReceiveNext();
            // UIManager.instance.UpdateHeightText(Height); // UIManager가 없으므로 주석
        }
    }
}
