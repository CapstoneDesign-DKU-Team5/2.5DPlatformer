using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    protected Rigidbody rigidBody;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected bool damaged = false;

    protected NavMeshAgent navMeshAgent;
    public Transform target;

    protected virtual void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.enabled = false;
    }

    protected virtual void Update()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                target = p.transform;
                navMeshAgent.enabled = true;   // ★ 여기서 한 번만 켜줌
            }
            return;                     // 아직 못 찾았으면 종료
        }

        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;

            navMeshAgent.SetDestination(target.position);
        }
    }

    public virtual void OnDamaged(Vector3 attackerPos) {

        if (damaged)
        {
            return;
        }

        // X로 튕길지 Z로 튕길지 결정
        bool isXOrZ = Mathf.Abs(transform.eulerAngles.y) % 180 == 0;

        int dir;
        rigidBody.linearVelocity = Vector3.zero;

        if (isXOrZ)
        {
            dir = transform.position.x - attackerPos.x > 0 ? 1 : -1;
            Vector3 dirVec = new Vector3(dir * 2, 0, 0);
            rigidBody.AddForce(dirVec, ForceMode.Impulse);
        }
        else
        {
            dir = transform.position.z - attackerPos.z > 0 ? 1 : -1;
            Vector3 dirVec = new Vector3(0, 0, dir * 2);
            rigidBody.AddForce(dirVec, ForceMode.Impulse);
        }

        damaged = true;
        Invoke("OffDamaged", 0.9f);
    }

    protected virtual void OffDamaged()
    {
        damaged = false;
    }
}
