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
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }
        else
        {
            navMeshAgent.enabled = true;
            navMeshAgent.SetDestination(target.position);
        }
    }

    public virtual void OnDamaged(Vector3 attackerPos) {

        if (damaged)
        {
            return;
        }

        // X·Î Æ¨±æÁö Z·Î Æ¨±æÁö °áÁ¤
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
