using HelloWorld;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;
using Photon.Pun;
using UnityEngine.UI;

public class Monster : MonoBehaviourPunCallbacks,IPunObservable
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

    [SerializeField, Tooltip("몬스터 스탯 (HP, Power)")]
    public MonsterStats stats;
    protected float HP;
    protected float Power;
    private float maxHP;

    [Header("UI")]
    [SerializeField, Tooltip("체력바 Fill 이미지 (Fill Type)")]
    private Image healthBarFillImage;

    [Header("리워드")]
    [SerializeField, Tooltip("죽었을 때 소환할 골드 프리팹 (Inspector에 연결)")]
    private GameObject goldPrefab;

    protected enum State
    {
        IDLE,
        CHASE,
        ATTACK,
        KILLED
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 쪽 HP 상태 내보내기
            stream.SendNext(HP);
            // (선택) 위치/회전 동기화도 함께 하고 싶다면 여기서 보내세요:
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 상대편에서 보낸 HP 갱신
            HP = (float)stream.ReceiveNext();
            UpdateHealthBarUI();

            // 위치/회전 동기화
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
        }
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
        HP = stats.hp;
        Power = stats.power;
        maxHP = stats.hp;
        StartCoroutine(StateMachine());
    }

    private void Start()
    {
        UpdateHealthBarUI();
        if (PhotonNetwork.IsMasterClient)
        {
            
            PhotonNetwork.SendRate = 15;           
            PhotonNetwork.SerializationRate = 10;  
        }

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
            yield return StartCoroutine(Wait(curAnimStateInfo.length/2));
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
        //Debug.Log("Hit 호출");
        float vDistance = Mathf.Abs(transform.position.y - target.position.y);
        float remainingDistance = monsterXOrZ ? target.position.x - transform.position.x : target.position.z - transform.position.z;
        remainingDistance = Mathf.Abs(remainingDistance);
        if (remainingDistance <= navMeshAgent.stoppingDistance && vDistance < 0.8f && isSameDir(target))
        {
            //Debug.Log("Hit 성공");
            NetworkPlayer player = target.GetComponent<NetworkPlayer>();
            player.OnDamaged(transform.position, stats.power);
            player.photonView.RPC(
    nameof(NetworkPlayer.OnDamaged),
    RpcTarget.AllBuffered,
    transform.position,
    stats.power);
        }
    }

    protected IEnumerator KILLED()
    {

        navMeshAgent.isStopped = true;    // 더 이상 이동 금지
        navMeshAgent.ResetPath();         // 남아 있던 경로 클리어
        childBoxCollider.enabled = false; // 충돌 판정도 비활성화

        gameObject.layer = LayerMask.NameToLayer("Dead");
        animator.Play("Dead", 0, 0);

        float duration = 1f;
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

        if (goldPrefab != null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                var go = PhotonNetwork.Instantiate(
    goldPrefab.name,
    transform.position,
    goldPrefab.transform.rotation
);
                var goldComp = go.GetComponent<Gold>();
                if (goldComp != null)
                {
                    goldComp.Initialize(stats.goldDrop);

                }
            }
               
                
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
        HP -= damage;
        UpdateHealthBarUI();

        photonView.RPC(nameof(RPC_UpdateHP), RpcTarget.OthersBuffered, HP);

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

       
        Debug.Log(HP);
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

    private void UpdateHealthBarUI()
    {
        if (healthBarFillImage != null && maxHP > 0)
            healthBarFillImage.fillAmount = HP / maxHP;
    }

    [PunRPC]
    public void RPC_UpdateHP(float newHP)
    {
        HP = newHP;
        UpdateHealthBarUI();
    }
}
