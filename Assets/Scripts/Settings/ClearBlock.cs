using UnityEngine;
using Photon.Pun;

public enum PlayerType
{
    Master,
    NotMaster
}

public class ClearBlock : MonoBehaviour
{
    [Tooltip("�� ��Ͽ� �ö�;� �ϴ� �÷��̾� Ÿ��")]
    public PlayerType requiredPlayerType;

    [HideInInspector]
    public bool isCorrectPlayerOnBlock = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var view = other.GetComponent<PhotonView>();
        if (view == null || !view.IsMine) return;

        bool isMaster = PhotonNetwork.IsMasterClient;
        isCorrectPlayerOnBlock = (requiredPlayerType == PlayerType.Master && isMaster) ||
                                 (requiredPlayerType == PlayerType.NotMaster && !isMaster);
        Debug.Log("�����");

        GameManager.instance.CheckGameClear();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var view = other.GetComponent<PhotonView>();
        if (view == null || !view.IsMine) return;

        isCorrectPlayerOnBlock = false;

        GameManager.instance.CheckGameClear();
    }
}
