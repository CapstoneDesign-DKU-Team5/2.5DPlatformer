using System.Collections;
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
        rigidBody = GetComponent<Rigidbody>();       
        spriteRenderer = GetComponent<SpriteRenderer>();

        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.enabled = false;
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
        Debug.Log("범위 내부로 들어옴");
        yield return null;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        navMeshAgent.enabled = true;
        state = State.CHASE;
    }

    protected IEnumerator ATTACK()
    {
        return null;
    }

    protected IEnumerator KILLED()
    {
        return null;
    }

    protected virtual void Update()
    {

    }

    public virtual void OnDamaged(Vector3 attackerPos)
    {

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
        Invoke("OffDamaged", 0.6f);
    }

    protected virtual void OffDamaged()
    {
        damaged = false;
    }
}
