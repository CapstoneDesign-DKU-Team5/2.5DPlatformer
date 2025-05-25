using HelloWorld;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;

public class Monster : MonoBehaviour
{
    [Header("������Ʈ ����")]
    [SerializeField, Tooltip("�ڽ� BoxCollider (�浹 ������)")]
    protected BoxCollider childBoxCollider;

    [SerializeField, Tooltip("���� ������ Rigidbody")]
    protected Rigidbody rigidBody;

    [SerializeField, Tooltip("�ִϸ��̼� ����� Animator")]
    protected Animator animator;

    [SerializeField, Tooltip("Sprite ���� �� �ð� ����� SpriteRenderer")]
    protected SpriteRenderer spriteRenderer;

    [Header("���� ����")]
    [SerializeField, Tooltip("�ǰ� �� ����")]
    protected bool damaged = false;

    [Header("���� Ÿ�� ����")]
    [SerializeField, Tooltip("NavMeshAgent ������Ʈ")]
    protected NavMeshAgent navMeshAgent;

    [Tooltip("�÷��̾� Ÿ��")]
    public Transform target;

    [Tooltip("���� Ÿ�� ��ġ")]
    public Vector3 targetPos;

    [Header("��Ÿ ���� ��")]
    [SerializeField, Tooltip("���Ͱ� X���� �������� �����̴��� ����")]
    private bool monsterXOrZ; // true�� x�� �̵�

    [SerializeField, Tooltip("���� ü��")]
    private float HP = 3;

    private enum State
    {
        IDLE,
        CHASE,
        ATTACK,
        KILLED
    }

    [SerializeField, Tooltip("���� ���� ����")]
    private State state;


    protected virtual void Awake()
    {
        monsterXOrZ = Approximately(Mathf.Abs(transform.eulerAngles.y % 180f));

        childBoxCollider = transform.Find("ChaseRange").GetComponentInChildren<BoxCollider>();

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

        float vDistance = Mathf.Abs(transform.position.y - target.position.y);

        float remainingDistance = monsterXOrZ ? target.position.x - transform.position.x : target.position.z - transform.position.z;

        spriteRenderer.flipX = remainingDistance < 0;

        remainingDistance = Mathf.Abs(remainingDistance);

        if (remainingDistance <= navMeshAgent.stoppingDistance && vDistance < 1f && isSameDir(target))
        {
            state = State.ATTACK;
        }
        else if (remainingDistance > (childBoxCollider.size.x / 2) || !isSameDir(target) || vDistance > 5f)
        {
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

    public void TriggerSetTarget(Transform t)
    {
        if (isSameDir(t) && state != State.CHASE/* && state != State.ATTACK*/)
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
        AnimatorStateInfo curAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        animator.Play("Attack", 0, 0);

        float vDistance = Mathf.Abs(transform.position.y - target.position.y);
        float remainingDistance = monsterXOrZ ? target.position.x - transform.position.x : target.position.z - transform.position.z;
        remainingDistance = Mathf.Abs(remainingDistance);

        if (remainingDistance > navMeshAgent.stoppingDistance || vDistance > 1f || !isSameDir(target))
        {
            state = State.CHASE;
        }
        else
        {
            yield return new WaitForSeconds(curAnimStateInfo.length * 2f);
        }

    }

    //animation event Monster_A Attack 0:03���� ȣ��
    public virtual void Hit()
    {
        Debug.Log("Hit ȣ��");
        float vDistance = Mathf.Abs(transform.position.y - target.position.y);
        float remainingDistance = monsterXOrZ ? target.position.x - transform.position.x : target.position.z - transform.position.z;
        remainingDistance = Mathf.Abs(remainingDistance);
        if (remainingDistance <= navMeshAgent.stoppingDistance && vDistance < 0.8f && isSameDir(target))
        {
            Debug.Log("Hit ����");
            NetworkPlayer player = target.GetComponent<NetworkPlayer>();
            player.OnDamaged(transform.position);
        }
    }

    protected IEnumerator KILLED()
    {
        return null;
    }

    protected virtual void Update()
    {
        if (target == null || state == State.ATTACK)
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

        Debug.Log("attacked A");

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
