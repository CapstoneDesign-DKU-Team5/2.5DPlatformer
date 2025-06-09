using UnityEngine;
using Photon.Pun;
using HelloWorld;

public class Bullet : MonoBehaviourPun
{
    [Header("=== Bullet Data ===")]
    [SerializeField, Tooltip("�� �Ѿ˿� �����ϴ� Item SO ����")]
    private Item bulletData;

    [Header("=== Movement Settings ===")]
    [SerializeField, Tooltip("�Ѿ� �ӵ�")]
    private float speed = 15f;
    [SerializeField, Tooltip("���� �ð� (��)")]
    private float lifeTime = 5f;
    [SerializeField, Tooltip("�Ѿ� ȸ�� �ӵ� (deg/sec)")]
    private float rotationSpeed = 720f; // �ʴ� 720�� ȸ�� (�⺻��)

    [Header("=== Damage Settings ===")]
    [SerializeField, Tooltip("������ �ݰ�")]
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
        // �̵�
        transform.position += direction * speed * Time.deltaTime;

        // ȸ�� (x�� ����)
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);

        // ������ ���� Ŀ����
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
