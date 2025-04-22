using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro; // �߰�

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
        // �÷��̾� ���� ��ġ ����
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
            inviteCodeText.text = "�� �ʴ� �ڵ�: ����";
        }
    }



}