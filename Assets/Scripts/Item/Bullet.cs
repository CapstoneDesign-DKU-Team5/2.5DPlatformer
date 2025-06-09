using UnityEngine;
using Photon.Pun;
using HelloWorld;

public class Bullet : MonoBehaviourPun
{
    [Header("=== Bullet Data ===")]
    [SerializeField, Tooltip("이 총알에 대응하는 Item SO 에셋")]
    private Item bulletData;    // Inspector에서 할당

    [Header("=== Movement Settings ===")]
    [SerializeField, Tooltip("총알 속도")]
    private float speed = 15f;
    [SerializeField, Tooltip("생존 시간 (초)")]
    private float lifeTime = 5f;

    [Header("=== Damage Settings ===")]
    [SerializeField, Tooltip("데미지 반경")]
    private float damageRadius = 5f;

    private Vector3 direction;
    private int damageAmount;
    private bool exploded = false;

    private void Start()
    {
        // SO에서 데미지값 가져오기
        damageAmount = 25;
        // lifeTime 후 자동 폭발
        Invoke(nameof(Explode), lifeTime);
    }

    /// <summary>
    /// 발사 방향 설정
    /// </summary>
    public void Initialize(Vector3 dir)
    {
       
        float sideSign = Mathf.Sign(dir.x);
        if (sideSign == 0f)
            sideSign = 1f;  // 클릭이 정직선 위일 때 기본 우측

        
        direction = new Vector3(sideSign, 0f, 0f);
    }

    private void Update()
    {
        // 직진
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 첫 충돌 시에도 폭발
        Explode();
    }

    /// <summary>
    /// 반경 내 몬스터에게 데미지 적용 후 총알 제거
    /// </summary>
    private void Explode()
    {
        if (!photonView.IsMine || exploded) return;
        exploded = true;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            damageRadius,
            direction,
            0.1f,
            LayerMask.GetMask("Enemy") // Enemy만 체크
        );

        foreach (var hit in hits)
        {
            Debug.Log($"[Bullet] Hit {hit.collider.name}");

            // Monster 컴포넌트 가져오기 (부모에 있을 수도 있으니 root에서도 확인)
            Monster monster = hit.collider.GetComponent<Monster>() ?? hit.collider.GetComponentInParent<Monster>();

            if (monster != null)
            {
                Debug.Log($"[Bullet] Damaging monster {monster.name}");
                monster.OnDamaged(transform.position, damageAmount);
                break; // 한 마리만 처리 (여러 마리 처리하고 싶으면 break 제거)
            }
        }

        PhotonNetwork.Destroy(gameObject);
    }

    // (디버그용) 씬 뷰에 반경 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
