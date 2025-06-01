using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private string gameVersion = "1.0";

    [Header("UI References (LobbyManager)")]
    public TextMeshProUGUI connectionInfoText;

    // 매치메이킹을 요청한 상태인지 저장하는 플래그
    private bool wantsMatchmaking = false;

    private void Start()
    {
        // Photon 초기 연결 시도
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
        connectionInfoText.text = "마스터 서버에 접속 중...";
    }

    public override void OnConnectedToMaster()
    {
        connectionInfoText.text = "온라인: 마스터 서버에 연결됨";

        // 만약 플레이어가 매치메이킹을 요청한 상태라면, 연결이 완료된 시점에 매치메이킹 흐름을 타도록 한다
        if (wantsMatchmaking)
        {
            wantsMatchmaking = false;
            TryJoinOrCreateMatchmakingRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        connectionInfoText.text = $"오프라인: 마스터 서버와 연결되지 않음\n재접속 시도 중...";
        // 재접속
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>
    /// 외부(문에서) 매치메이킹을 요청할 때 호출하는 메서드
    /// </summary>
    public void StartMatchmaking()
    {
        wantsMatchmaking = true;

        if (PhotonNetwork.IsConnectedAndReady)
        {
            // 이미 준비가 끝난 상태라면 즉시 JoinRandomRoom 흐름을 탄다
            TryJoinOrCreateMatchmakingRoom();
        }
        else
        {
            // 아직 연결 중이라면, OnConnectedToMaster에서 자동으로 호출되도록 메시지만 띄운다
            connectionInfoText.text = "매치메이킹 준비 중...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // 실제로 JoinRandomRoom → 실패 시 CreateRoom 을 실행하는 메서드
    private void TryJoinOrCreateMatchmakingRoom()
    {
        connectionInfoText.text = "참가 가능한 게임을 찾는 중...";
        var expectedProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", false } };
        PhotonNetwork.JoinRandomRoom(expectedProperties, 0);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // PhotonNetwork가 준비되지 않았다면(Connecting/Joining 상태) 재연결을 시도
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            wantsMatchmaking = true;
            connectionInfoText.text = "네트워크 준비 중... 재연결 시도";
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        // Ready 상태면 방이 없는 것이므로 새 방 생성
        connectionInfoText.text = "매치메이킹용 방 생성 중...";
        PhotonNetwork.CreateRoom(
            null,
            new RoomOptions
            {
                MaxPlayers = 2,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "inviteOnly", false } },
                CustomRoomPropertiesForLobby = new[] { "inviteOnly" }
            }
        );
    }

    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "방 참가 성공";
        PhotonNetwork.LoadLevel("GameScene");
    }

    /// <summary>
    /// (Optional) 외부에서 Photon 재연결만 필요할 때 호출할 수 있도록 공개해둔다.
    /// </summary>
    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            connectionInfoText.text = "네트워크 재연결 중...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }
}
