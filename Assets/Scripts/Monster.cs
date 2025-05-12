    using System.Collections;
    using UnityEngine;
    using UnityEngine.AI;
    using UnityEngine.SocialPlatforms;

    public class Monster : MonoBehaviour
    {
    [Header("컴포넌트 참조")]
    [SerializeField, Tooltip("자식 BoxCollider (충돌 판정용)")]
    protected BoxCollider childBox;

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
    private bool monsterXOrZ; // true면 x축 이동

    [SerializeField, Tooltip("현재 체력")]
    private float HP = 3;

    private enum State
    {
        IDLE,
        CHASE,
        ATTACK,
        KILLED
    }

    [SerializeField, Tooltip("현재 몬스터 상태")]
    private State state;


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

            //Debug.Log("범위 내부로 들어옴");

            float vDistance = Mathf.Abs(transform.position.y - target.position.y);

            float remainingDistance = monsterXOrZ ? target.position.x - transform.position.x : target.position.z - transform.position.z;

            spriteRenderer.flipX = remainingDistance < 0;

            remainingDistance = Mathf.Abs(remainingDistance);

            if (remainingDistance <= navMeshAgent.stoppingDistance && vDistance < 1f && isSameDir(target)) 
            {
                Debug.Log("Attack");
                //state = State.ATTACK;

            }
            else if (navMeshAgent.remainingDistance > childBox.size.x * 5f || !isSameDir(target) || vDistance > 5f)
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
            //Debug.Log(playerXOrZ == monsterXOrZ);
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
            Debug.Log("공격중");
            yield return new WaitForSeconds(2f);
            state = State.CHASE;
        }

        protected IEnumerator KILLED()
        {
            return null;
        }

        protected virtual void Update()
        {
            if (target == null)
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
