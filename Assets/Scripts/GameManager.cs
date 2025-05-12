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

    private void OnClickReady()
    {
        isReady = !isReady;
        // ��ư �ؽ�Ʈ ��� (��: "�غ� �Ϸ�" �� "�غ� ���")
        readyButton.GetComponentInChildren<TextMeshProUGUI>().text
            = isReady ? "�غ� ���" : "�غ� �Ϸ�";

        // MasterClient �� StartButton Ȱ��ȭ ���� RPC ��û
        photonView.RPC(nameof(RPC_SetStartInteractable), RpcTarget.MasterClient, isReady);
    }

    private void OnClickStart()
    {
        // ��ο��� ��Ʈ�� ��� RPC
        photonView.RPC(nameof(RPC_EnablePlayerControl), RpcTarget.AllBuffered);
        startButton.interactable = false; // �ٽ� ���α�
    }

    [PunRPC]
    private void RPC_SetStartInteractable(bool canStart)
    {
        // MasterClient�� ����ϹǷ� �ٷ� ó��
        startButton.interactable = canStart;
    }

    [PunRPC]
    private void RPC_EnablePlayerControl()
    {
        // ��� NetworkPlayer�� �Է� ���
        foreach (var np in FindObjectsOfType<NetworkPlayer>())
            np.EnableControl();
    }




}