using UnityEngine;
using Photon.Pun;
using HelloWorld;

public class Bullet : MonoBehaviourPun
{
    [Header("=== Bullet Data ===")]
    [SerializeField, Tooltip("이 총알에 대응하는 Item SO 에셋")]
    private Item bulletData;

    [Header("=== Movement Settings ===")]
    [SerializeField, Tooltip("총알 속도")]
    private float speed = 15f;
    [SerializeField, Tooltip("생존 시간 (초)")]
    private float lifeTime = 5f;
    [SerializeField, Tooltip("총알 회전 속도 (deg/sec)")]
    private float rotationSpeed = 720f; // 초당 720도 회전 (기본값)

    [Header("=== Damage Settings ===")]
    [SerializeField, Tooltip("데미지 반경")]
    private float damageRadius = 5f;

    private Vector3 direction;
    private int damageAmount;
    private bool exploded = false;
    private Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f);
    private Vector3 targetScale = new Vector3(0.5f, 0.5f, 0.5f);
    private float elapsedTime = 0f;

    private void Start()
    {
        damageAmount = 25;
        transform.localScale = startScale;
        Invoke(nameof(Explode), lifeTime);
    }

    public void Initialize(Vector3 dir)
    {
        float sideSign = Mathf.Sign(dir.x);
        if (sideSign == 0f)
            sideSign = 1f;

        direction = new Vector3(sideSign, 0f, 0f);
    }

    private void Update()
    {
        // 이동
        transform.position += direction * speed * Time.deltaTime;

        // 회전 (x축 기준)
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);

        // 스케일 점점 커지게
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / lifeTime);
        transform.localScale = Vector3.Lerp(startScale, targetScale, t);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (!photonView.IsMine || exploded) return;
        exploded = true;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            damageRadius,
            direction,
            0.1f,
            LayerMask.GetMask("Enemy")
        );

        foreach (var hit in hits)
        {
            Debug.Log($"[Bullet] Hit {hit.collider.name}");
            Monster monster = hit.collider.GetComponent<Monster>() ?? hit.collider.GetComponentInParent<Monster>();

            if (monster != null)
            {
                Debug.Log($"[Bullet] Damaging monster {monster.name}");
                monster.OnDamaged(transform.position, damageAmount);
                break;
            }
        }

        PhotonNetwork.Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
