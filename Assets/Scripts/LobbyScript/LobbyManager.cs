using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class LobbyManager : MonoBehaviourPunCallbacks
{
    private string gameVersion = "1.0";
    [Header("References")]
    [Tooltip("ConnectionInfo,MatchMaker")]
    public TextMeshProUGUI connectionInfoText;
    public Button matchmakingButton;

    private void Awake()
    {
        matchmakingButton.onClick.AddListener(Connect);
    }
    //게임 실행과 동시에 마스터 서버 접속 시도
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        //룸 접속 버튼 잠시 비활성화
        matchmakingButton.interactable = false;

        //접속 시도 로딩중 텍스트
        connectionInfoText.text = "마스터 서버에 접속 중...";
    }
    // 마스터 서버 접속 성공 시 자동 실행
    public override void OnConnectedToMaster()
    {
        //룸 접속 버튼 활성화
        matchmakingButton.interactable = true;
        //접속 정보 표시
        connectionInfoText.text = "온라인: 마스터 서버에 연결됨";
    }
    //마스터 서버 접속 실패 시 자동 실행
    public override void OnDisconnected(DisconnectCause cause)
    {
        // 룸 접속 버튼 비활성화
        matchmakingButton.interactable = false;
        // 접속 정보 표시 변경
        connectionInfoText.text = "오프라인: 마스터 서버와 연결되지 않음 \n 접속 재시도중...";

        // 재접속 시도
        PhotonNetwork.ConnectUsingSettings();
    }

    //룸 접속 시도
    public void Connect()
    {
        //중복 접속을 막기위한 조치
        matchmakingButton.interactable = false;

        //마스터 서버 접속중
        if(PhotonNetwork.IsConnected)
        {
            //룸 접속 실행
            connectionInfoText.text = "참가 가능한 게임을 찾는중...";
            PhotonNetwork.JoinRandomRoom();

        }
        else
        {
            //마스터 서버에 접속 중이 아니라면 다시 접속 시도
            connectionInfoText.text = "오프라인: 마스터 서버와 연결되지 않음 \n접속 재시도 중...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    //랜덤 룸 참가에 실패한 경우 자동 실행
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        //4명 수용가능한 빈 방 생성
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    //룸에 참가 완료된 경우 자동 실행
    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "방 참가 성공";
        PhotonNetwork.LoadLevel("GameScene");
    }


}
