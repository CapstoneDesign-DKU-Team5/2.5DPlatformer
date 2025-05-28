using HelloWorld;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;

public class Monster : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField, Tooltip("자식 BoxCollider (충돌 판정용)")]
    protected BoxCollider childBoxCollider;

    [SerializeField, Tooltip("물리 반응용 Rigidbody")]
    protected Rigidbody rigidBody;

    [SerializeField, Tooltip("애니메이션 제어용 Animator")]
    protected Animator animator;

    [SerializeField, Tooltip("Sprite 반전 및 시각 제어용 SpriteRenderer")]
    protected SpriteRenderer spriteRenderer;

    [Header("몬스터 상태")]
    [SerializeField, Tooltip("피격 중 여부")]
    protected bool damaged = false;

    [Header("추적 타겟 정보")]
    [SerializeField, Tooltip("NavMeshAgent 컴포넌트")]
    protected NavMeshAgent navMeshAgent;

    [Tooltip("플레이어 타겟")]
    public Transform target;

    [Tooltip("계산된 타겟 위치")]
    public Vector3 targetPos;

    [Header("기타 상태 값")]
    [SerializeField, Tooltip("몬스터가 X축을 기준으로 움직이는지 여부")]
    protected bool monsterXOrZ; // true면 x축 이동

    [SerializeField, Tooltip("현재 체력")]
    protected float HP = 3;

    protected enum State
    {
        IDLE,
        CHASE,
        ATTACK,
        KILLED
    }

    [SerializeField, Tooltip("현재 몬스터 상태")]
    protected State state;


    protected void Awake()
    {
        monsterXOrZ = Approximately(Mathf.Abs(transform.eulerAngles.y % 180f));

        childBoxCollider = transform.Find("ChaseRange").GetComponentInChildren<BoxCollider>();

        rigidBody = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        //navMeshAgent.enabled = false;
        navMeshAgent.updateRotation = false;

        state = State.IDLE;
        HP = 10;
        StartCoroutine(StateMachine());
    }

    protected IEnumerator StateMachine()
    {
        while (state != State.KILLED)
        {
            yield return StartCoroutine(state.ToString());
        }
        yield return StartCoroutine(State.KILLED.ToString());
    }

    protected virtual IEnumerator IDLE()
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
        else if (remainingDistance > (childBoxCollider.size.x / 2) + 0.5f || !isSameDir(target) || vDistance > 5f)
        {
            target = null;
            navMeshAgent.SetDestination(transform.position);
            yield return null;
            state = State.IDLE;
        }
        else
        {
            yield return StartCoroutine(Wait(curAnimStateInfo.length));
        }
    }

    public void TriggerSetTarget(Transform t)
    {
        if (state == State.KILLED)
        {
            return;
        }

        if (isSameDir(t) && state != State.CHASE/* && state != State.ATTACK*/)
        {
            target = t;
            //navMeshAgent.enabled = true;

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
            yield return StartCoroutine(Wait(curAnimStateInfo.length * 2f));
        }

    }

    //animation event Monster_A Attack 0:03에서 호출
    public void Hit()
    {
        if (target == null)
        {
            return;
        }
        Debug.Log("Hit 호출");
        float vDistance = Mathf.Abs(transform.position.y - target.position.y);
        float remainingDistance = monsterXOrZ ? target.position.x - transform.position.x : target.position.z - transform.position.z;
        remainingDistance = Mathf.Abs(remainingDistance);
        if (remainingDistance <= navMeshAgent.stoppingDistance && vDistance < 0.8f && isSameDir(target))
        {
            Debug.Log("Hit 성공");
            NetworkPlayer player = target.GetComponent<NetworkPlayer>();
            player.OnDamaged(transform.position);
        }
    }

    protected IEnumerator KILLED()
    {
        gameObject.layer = LayerMask.NameToLayer("Dead");
        animator.Play("Dead", 0, 0);

        float duration = 1.5f;
        float timer = 0f;

        Color color = spriteRenderer.material.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / duration);

            color.a = alpha;
            spriteRenderer.material.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }

    protected IEnumerator Wait(float t)
    {
        while (t > 0 && state != State.KILLED)
        {
            t -= Time.deltaTime;
            yield return null;
        }
    }

    protected void Update()
    {
        if (target == null || state == State.ATTACK)
        {
            return;
        }
        CalTargetPos();
        navMeshAgent.SetDestination(targetPos);
    }

    public void OnDamaged(Vector3 attackerPos, int damage)
    {
        if (damaged)
        {
            return;
        }

        AnimatorStateInfo curAnimStateInfo = animator.GetCurrentAnimatorStateInfo(0);

        //몬스터 넉백
        //rigidBody.isKinematic = false;
        //int dir;
        //rigidBody.linearVelocity = Vector3.zero;
        //if (monsterXOrZ)
        //{
        //    dir = transform.position.x - attackerPos.x > 0 ? 1 : -1;
        //    Vector3 dirVec = new Vector3(dir, 0, 0);
        //    rigidBody.AddForce(dirVec, ForceMode.Impulse);
        //}
        //else
        //{
        //    dir = transform.position.z - attackerPos.z > 0 ? 1 : -1;
        //    Vector3 dirVec = new Vector3(0, 0, dir);
        //    rigidBody.AddForce(dirVec, ForceMode.Impulse);
        //}

        HP -= damage;

        if (HP <= 0)
        {
            target = null;
            state = State.KILLED;
            return;
        }
        else
        {
            if (!curAnimStateInfo.IsName("Attack"))
            {
                animator.Play("Damaged", 0, 0);
            }
        }

        damaged = true;
        Invoke("OffDamaged", 0.6f);
    }

    protected void OffDamaged()
    {
        //rigidBody.isKinematic = true;
        damaged = false;
    }
}
