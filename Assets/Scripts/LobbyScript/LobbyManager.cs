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
    //���� ����� ���ÿ� ������ ���� ���� �õ�
    private void Start()
    {
        //���ӹ��� ����
        PhotonNetwork.GameVersion = gameVersion;
        //�����ͼ��� ���� �õ�
        PhotonNetwork.ConnectUsingSettings();

        //�� ���� ��ư ��� ��Ȱ��ȭ
        matchmakingButton.interactable = false;

        //���� �õ� �ε��� �ؽ�Ʈ
        connectionInfoText.text = "������ ������ ���� ��...";
    }
    // ������ ���� ���� ���� �� �ڵ� ����
    public override void OnConnectedToMaster()
    {
        //�� ���� ��ư Ȱ��ȭ
        matchmakingButton.interactable = true;
        //���� ���� ǥ��
        connectionInfoText.text = "�¶���: ������ ������ �����";
    }
    //������ ���� ���� ���� �� �ڵ� ����
    public override void OnDisconnected(DisconnectCause cause)
    {
        // �� ���� ��ư ��Ȱ��ȭ
        matchmakingButton.interactable = false;
        // ���� ���� ǥ�� ����
        connectionInfoText.text = "��������: ������ ������ ������� ���� \n ���� ��õ���...";

        // ������ �õ�
        PhotonNetwork.ConnectUsingSettings();
    }

    //�� ���� �õ�
    public void Connect()
    {
        //�ߺ� ������ �������� ��ġ
        matchmakingButton.interactable = false;

        //������ ���� ������
        if(PhotonNetwork.IsConnected)
        {
            //�� ���� ����
            connectionInfoText.text = "���� ������ ������ ã����...";
            PhotonNetwork.JoinRandomRoom();

        }
        else
        {
            //������ ������ ���� ���� �ƴ϶�� �ٽ� ���� �õ�
            connectionInfoText.text = "��������: ������ ������ ������� ���� \n���� ��õ� ��...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    //���� �� ������ ������ ��� �ڵ� ����
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        //4�� ���밡���� �� �� ����
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    //�뿡 ���� �Ϸ�� ��� �ڵ� ����
    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "�� ���� ����";
        PhotonNetwork.LoadLevel("GameScene");
    }


}
