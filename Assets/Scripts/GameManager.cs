using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    // �̱��� �ν��Ͻ�
    public static GameManager Instance;

    public GameObject playerPrefab; // �÷��̾� ������

    private int Height = 0;
    public bool isGameOVer { get; private set; } // ���ӿ��� ����

    private void Awake()
    {
        // �̱��� �ʱ�ȭ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �� ��ȯ �� ����
        }
        else
        {
            Destroy(gameObject); // �ߺ� ����
        }
    }

    private void Start()
    {
        // �÷��̾� ���� ��ġ ����
        Vector3 spawnPosition = PhotonNetwork.IsMasterClient
            ? new Vector3(0f, 3f, -3f)
            : new Vector3(-1.5f, 3f, -3f);

        // ��Ʈ��ũ�� �÷��̾� ����
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
    }

    public void UpdateHeight()
    {
        // ���� �÷��̾��� y���� �������� ���� ����
        GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer != null)
        {
            Height = Mathf.Max(Height, Mathf.RoundToInt(localPlayer.transform.position.y));

            // UIManager.instance.UpdateHeightText(Height); // UIManager�� �������� �����Ƿ� �ּ� ó��
        }
    }

    public void EndGame()
    {
        isGameOVer = true;
        // ���� ���� ó�� ���� (���� ����)
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom(); // �� ������
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    // ����ȭ ó��
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(Height);
        }
        else
        {
            Height = (int)stream.ReceiveNext();
            // UIManager.instance.UpdateHeightText(Height); // UIManager�� �����Ƿ� �ּ�
        }
    }
}
