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
    [Tooltip("이 블록에 올라와야 하는 플레이어 타입")]
    public PlayerType requiredPlayerType;

    [Tooltip("감지 반경")]
    public float detectRadius = 1f;

    [HideInInspector]
    public bool isCorrectPlayerOnBlock = false;

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);
        bool nowCorrect = false;
        string who = "";

        // OverlapSphere 내 모든 Collider 검사
        foreach (var hit in hits)
        {
           
            if (!hit.CompareTag("Player")) continue;

            // 이 Collider가 속한 PhotonView 찾기
            var view = hit.GetComponentInParent<PhotonView>();
            if (view == null) continue;

            bool isMasterPlayer = view.Owner == PhotonNetwork.MasterClient;

            // 이 블록이 요구하는 타입에 맞으면
            if ((requiredPlayerType == PlayerType.Master && isMasterPlayer) ||
                (requiredPlayerType == PlayerType.NotMaster && !isMasterPlayer))
            {
                nowCorrect = true;
               
                who = view.Owner.NickName ?? view.gameObject.name;
                break;
            }
        }

        // 상태가 바뀌었을 때만 처리
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
