using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;

public class Monster : MonoBehaviour
{
    protected BoxCollider childBox;

    protected Rigidbody rigidBody;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected bool damaged = false;

    protected NavMeshAgent navMeshAgent;
    public Transform target;
    public Vector3 targetPos;

    bool monsterXOrZ; //true¸é x
    float HP = 3;

    enum State
    {
        IDLE,
        CHASE,
        ATTACK,
        KILLED
    }

    State state;

    protected virtual void Awake()
    {
        monsterXOrZ = Approximately(Mathf.Abs(transform.eulerAngles.y % 180f));

        childBox = GetComponentInChildren<BoxCollider>();

        rigidBody = GetComponent<Rigidbody>();       
        spriteRenderer = GetComponent<SpriteRenderer>();

        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        navMeshAgent.enabled = false;
        navMeshAgent.updateRotation = false;

        state = State.IDLE;
        HP = 3;
        StartCoroutine(StateMachine());
    }

    protected IEnumerator StateMachine()
    {
        while (HP > 0)
        {
            yield return StartCoroutine(state.ToString());
        }
    }
    protected IEnumerator IDLE()
    {
        AnimatorStateInfo curAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (curAnimStateInfo.IsName("IdleNormal") == false)
        {
            animator.Play("IdleNormal", 0, 0);
        }

        yield return null;
    }

    protected IEnumerator CHASE()
    {

        AnimatorStateInfo curAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (curAnimStateInfo.IsName("Walk") == false)
        {
            animator.Play("Walk", 0, 0);

            yield return null;
        }

        Debug.Log("¹üÀ§ ³»ºÎ·Î µé¾î¿È");

        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            Debug.Log("Attack");
        }
        else if (navMeshAgent.remainingDistance > childBox.size.x * 5 || !isSameDir(target))
        {

            Debug.Log("¸ØÃã");
            target = null;
            navMeshAgent.SetDestination(transform.position);
            yield return null;
            state = State.IDLE;
        }
        else
        {
            yield return new WaitForSeconds(curAnimStateInfo.length);
        }
    }

    public void SetTarget(Transform t)
    {
        if (isSameDir(t))
        {
            target = t;
            navMeshAgent.enabled = true;

            CalTargetPos();

            navMeshAgent.SetDestination(targetPos);

            state = State.CHASE;
        }
    }

    protected bool isSameDir(Transform t)
    {
        bool playerXOrZ = Approximately(Mathf.Abs(t.eulerAngles.y) % 180);
        return playerXOrZ == monsterXOrZ;
    }

    protected void CalTargetPos()
    {
        targetPos = target.position;
        targetPos.y = transform.position.y;

        if (monsterXOrZ)
        {
            targetPos.z = transform.position.z;
        }
        else
        {
            targetPos.x = transform.position.x;
        }
    }

    protected bool Approximately(float num, float epsilon = 0.01f)
    {
        return Mathf.Abs(num) < epsilon;
    }

    protected IEnumerator ATTACK()
    {
        Debug.Log("°ø°ÝÁß");
        yield return null;
    }

    protected IEnumerator KILLED()
    {
        return null;
    }

    protected virtual void Update()
    {
        if(target == null)
        {
            return;
        }
        CalTargetPos();
        navMeshAgent.SetDestination(targetPos);
    }

    public virtual void OnDamaged(Vector3 attackerPos)
    {

        if (damaged)
        {
            return;
        }

        // X·Î Æ¨±æÁö Z·Î Æ¨±æÁö °áÁ¤
        bool monsterXOrZ = Mathf.Abs(transform.eulerAngles.y) % 180 == 0;

        int dir;
        rigidBody.linearVelocity = Vector3.zero;

        if (monsterXOrZ)
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
        Invoke("OffDamaged", 0.6f);
    }

    protected virtual void OffDamaged()
    {
        damaged = false;
    }
}
