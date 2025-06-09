using UnityEngine;
using Photon.Pun;

public enum PlayerType
{
    Master,
    NotMaster
}

public class ClearBlock : MonoBehaviour
{
    [Tooltip("이 블록에 올라와야 하는 플레이어 타입")]
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
        Debug.Log("밟았음");

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
