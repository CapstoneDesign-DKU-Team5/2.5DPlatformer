using System.Linq;
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

    [Tooltip("���� �ݰ�")]
    public float detectRadius = 1f;

    [HideInInspector]
    public bool isCorrectPlayerOnBlock = false;

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);
        bool nowCorrect = false;
        string who = "";

        // OverlapSphere �� ��� Collider �˻�
        foreach (var hit in hits)
        {
           
            if (!hit.CompareTag("Player")) continue;

            // �� Collider�� ���� PhotonView ã��
            var view = hit.GetComponentInParent<PhotonView>();
            if (view == null) continue;

            bool isMasterPlayer = view.Owner == PhotonNetwork.MasterClient;

            // �� ����� �䱸�ϴ� Ÿ�Կ� ������
            if ((requiredPlayerType == PlayerType.Master && isMasterPlayer) ||
                (requiredPlayerType == PlayerType.NotMaster && !isMasterPlayer))
            {
                nowCorrect = true;
               
                who = view.Owner.NickName ?? view.gameObject.name;
                break;
            }
        }

        // ���°� �ٲ���� ���� ó��
        if (nowCorrect != isCorrectPlayerOnBlock)
        {
            isCorrectPlayerOnBlock = nowCorrect;

            if (nowCorrect)
            {
                Debug.Log($"[{name}]  {who} stepped ON!");
            }
            else
            {
                Debug.Log($"[{name}]  {requiredPlayerType} left.");
            }

            GameManager.instance.CheckGameClear();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
