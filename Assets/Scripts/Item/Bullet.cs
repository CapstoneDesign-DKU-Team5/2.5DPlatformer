using UnityEngine;
using Photon.Pun;
using HelloWorld;

public class Bullet : MonoBehaviourPun
{
    [Header("=== Bullet Data ===")]
    [SerializeField, Tooltip("�� �Ѿ˿� �����ϴ� Item SO ����")]
    private Item bulletData;    // Inspector���� �Ҵ�

    [Header("=== Movement Settings ===")]
    [SerializeField, Tooltip("�Ѿ� �ӵ�")]
    private float speed = 15f;
    [SerializeField, Tooltip("���� �ð� (��)")]
    private float lifeTime = 5f;

    [Header("=== Damage Settings ===")]
    [SerializeField, Tooltip("������ �ݰ�")]
    private float damageRadius = 5f;

    private Vector3 direction;
    private int damageAmount;
    private bool exploded = false;

    private void Start()
    {
        // SO���� �������� ��������
        damageAmount = 25;
        // lifeTime �� �ڵ� ����
        Invoke(nameof(Explode), lifeTime);
    }

    /// <summary>
    /// �߻� ���� ����
    /// </summary>
    public void Initialize(Vector3 dir)
    {
       
        float sideSign = Mathf.Sign(dir.x);
        if (sideSign == 0f)
            sideSign = 1f;  // Ŭ���� ������ ���� �� �⺻ ����

        
        direction = new Vector3(sideSign, 0f, 0f);
    }

    private void Update()
    {
        // ����
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // ù �浹 �ÿ��� ����
        Explode();
    }

    /// <summary>
    /// �ݰ� �� ���Ϳ��� ������ ���� �� �Ѿ� ����
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
            LayerMask.GetMask("Enemy") // Enemy�� üũ
        );

        foreach (var hit in hits)
        {
            Debug.Log($"[Bullet] Hit {hit.collider.name}");

            // Monster ������Ʈ �������� (�θ� ���� ���� ������ root������ Ȯ��)
            Monster monster = hit.collider.GetComponent<Monster>() ?? hit.collider.GetComponentInParent<Monster>();

            if (monster != null)
            {
                Debug.Log($"[Bullet] Damaging monster {monster.name}");
                monster.OnDamaged(transform.position, damageAmount);
                break; // �� ������ ó�� (���� ���� ó���ϰ� ������ break ����)
            }
        }

        PhotonNetwork.Destroy(gameObject);
    }

    // (����׿�) �� �信 �ݰ� �ð�ȭ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
