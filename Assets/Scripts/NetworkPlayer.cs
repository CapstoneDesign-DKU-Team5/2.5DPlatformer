using Photon.Pun;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using PlayFab;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Collections;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Photon.Pun.Demo.Asteroids;


namespace HelloWorld
{
    public class NetworkPlayer : MonoBehaviourPun
    {
        [Header("컴포넌트 참조")]
        [SerializeField, Tooltip("캐릭터 애니메이션을 제어하는 Animator")]
        private Animator animator;

        [SerializeField, Tooltip("스프라이트 반전 및 색상 변경에 사용되는 SpriteRenderer")]
        private SpriteRenderer spriteRenderer;

        [SerializeField, Tooltip("물리 연산에 사용되는 Rigidbody")]
        private Rigidbody rigidBody;

        [SerializeField, Tooltip("플레이어 몸체를 나타내는 주 Collider")]
        private Collider playerCollider;

        [Header("카메라 참조")]
        [SerializeField, Tooltip("플레이어를 따라다니는 메인 카메라")]
        private Camera mainCamera;

        [SerializeField, Tooltip("카메라 회전을 담당하는 스크립트")]
        private RotateCamera cameraScript;

        [Header("이동 설정")]
        [SerializeField, Tooltip("수평 이동 속도 (단위/초)")]
        private float speed = 2f;

        [SerializeField, Tooltip("점프 시 적용할 힘 (ForceMode.Impulse)")]
        private float jumpHeight = 20f;

        [SerializeField, Tooltip("전방 레이캐스트 길이의 절반")]
        private float cameraRaySize = 30f;

        [Header("UI")]
        [SerializeField, Tooltip("Playfab 유저네임 표시")]
        private TMP_Text usernameText;
        [SerializeField, Tooltip("PlayerStat ScriptableObject 에셋")]
        private PlayerStat playerStat;
        [SerializeField, Tooltip("HealthBar")]
        private Image healthBarFillImage;

        [Header("=== Item Effect Slots ===")]
        [SerializeField, Tooltip("아이템 이펙트가 생성될 위치들")]
        private Transform[] itemEffectSlots = new Transform[3];

        [Header("=== Healing Settings ===")]
        [SerializeField, Tooltip("힐 가능한 X축 거리 범위")]
        private float healRangeX = 2f;
        [SerializeField, Tooltip("힐 Amount")]
        private int healAmount = 10;
        [SerializeField, Tooltip("힐 시 내 체력 소모량")]
        private int healCost = 10;

        private float currentHealth;
        private int currentPower;
        private readonly Vector3 _effectRotateAxis = new Vector3(-1f, 0f, 1f).normalized;
        private const float _effectRotateSpeed = 90f; // 초당 90도

        [SerializeField, Tooltip("골드 줍기 범위")]
        private float goldPickupRange = 2f;

        [Header("Landing Damage Settings")]
        [Tooltip("낙하 데미지를 주기 시작하는 최소 거리")]
        [SerializeField]
        private float minFallRange = 1f;

        [Header("=== Bullet Settings ===")]
        [SerializeField, Tooltip("발사할 총알 프리팹 (Resources/Bullets 폴더)")]
        private GameObject bulletPrefab;

        

        [SerializeField, Tooltip("발사체 생성 시 플레이어로부터 떨어질 거리")]
        private float sideSpawnOffset = 1f;


        private bool isShootable = false;


        private BoxCollider boxColider;

        // ── 런타임 변수────────────────────────────
        private bool climbState = false;

        private float h = 0f;
        private float v = 0f;

        private bool jump = false;
        private bool attack = false;
        private bool climbJump = false;
        private bool isControlLocked = false;
        private bool isInvincible = false;
        private float _fallStartY;

        private Vector3 savedVelocity;
        private bool needRestoreVelocity = false;

        private bool isAttacking = false;
        private bool isAir = false;
        private bool isVisible = true;
        private bool isInvisibleFrontAndBack = false;
        private int isCameraFront = 1;
        private bool lastAirState = false;

        private bool flipState = false;
        private bool isDead = false;

        private Vector3 networkPos;
        private Quaternion networkRot;
        private Vector3 networkVel;
        private double networkTimeStamp;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(rigidBody.linearVelocity);
                stream.SendNext(PhotonNetwork.Time);
            }
            else
            {
                networkPos = (Vector3)stream.ReceiveNext();
                networkRot = (Quaternion)stream.ReceiveNext();
                networkVel = (Vector3)stream.ReceiveNext();
                networkTimeStamp = (double)stream.ReceiveNext();
            }
        }

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerCollider = GetComponent<Collider>();
            boxColider = GetComponent<BoxCollider>();

            jumpHeight = 7f;
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }


        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                string displayName = PlayerPrefs.GetString("displayName", "Guest");
                photonView.RPC(nameof(SetUsernameText), RpcTarget.AllBuffered, displayName);
            }
        }


        [PunRPC]
        private void SetUsernameText(string name)
        {
            if (usernameText != null)
            {
                usernameText.text = name;
            }
        }

        private void Start()
        {
            if (!photonView.IsMine)
            {
                rigidBody.isKinematic = true;
            }
            if (photonView.IsMine)
            {
                GameObject camObj = Instantiate(Resources.Load<GameObject>("CameraPrefab"));
                mainCamera = camObj.GetComponent<Camera>();
                cameraScript = camObj.GetComponent<RotateCamera>();
                cameraScript.playerTransform = transform;
                currentHealth = playerStat.hp;
                currentPower = playerStat.power;
                UpdateHealthBar();
            }
        }

        private void Update()
        {
           
           
            if (!photonView.IsMine || mainCamera == null || cameraScript == null)
                return;

            if (!photonView.IsMine)
            {
                double lag = PhotonNetwork.Time - networkTimeStamp;
                Vector3 extrapolated = networkPos + networkVel * (float)lag;

                transform.position = Vector3.Lerp(
                transform.position,
                extrapolated,
                Time.deltaTime * 10f  // 보간 속도: 필요시 조절
            );
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    networkRot,
                    Time.deltaTime * 10f
                );
            }

            // 3) 한 번만 죽음 처리
            if (!isDead && currentHealth <= 0)
            {
                Die();
            }
            if (isDead) return;

            InputKey();
            UpdateClimbState();
            PlayerMoveAni();
            PlayerLookCamera();
            HandleShooting();

            if (Input.GetKeyDown(KeyCode.F))
            {
                TryHeal();
            }

            if (Input.GetKeyDown(KeyCode.Z))
                TryPickupGold();
        }

        private void FixedUpdate()
        {
            if (!photonView.IsMine || mainCamera == null || cameraScript == null)
                return;

            IsVisible();
            IsVisibleFromBack();
            IsAir();

            Move();
            Jump();
            RaySide("Right");
            RaySide("Left");
            RayTop();
            RayDown();
            StartAttack();
            CheckCollisionDamage();
        }

        public void TakeDamage(float damageAmount)
        {
            if (!photonView.IsMine) return;


            currentHealth = Mathf.Max(0, currentHealth - damageAmount);
            UpdateHealthBar();


            photonView.RPC(nameof(RPC_UpdateHealth), RpcTarget.OthersBuffered, currentHealth);
            if (currentHealth <= 0)
                Die();
        }


        [PunRPC]
        private void RPC_UpdateHealth(float newHealth)
        {
            currentHealth = newHealth;
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            if (healthBarFillImage == null || playerStat == null)
                return;

            float fillRatio = (float)currentHealth / playerStat.hp;
            healthBarFillImage.fillAmount = Mathf.Clamp01(fillRatio);
        }

        public void ApplyHeal(float amount)
        {
            if (!photonView.IsMine) return;
            currentHealth = Mathf.Min(playerStat.hp, currentHealth + amount);
            UpdateHealthBar();
            photonView.RPC(nameof(RPC_UpdateHealth), RpcTarget.OthersBuffered, currentHealth);
        }

        public int GetPower()
        {
            return currentPower;
        }

        public void SetPower(int newPower)
        {
            if (!photonView.IsMine) return;
            currentPower = newPower;
            // 필요하다면 공격 애니메이션/효과 갱신 로직 추가
            photonView.RPC(nameof(RPC_UpdatePower), RpcTarget.OthersBuffered, currentPower);
        }

        [PunRPC]
        private void RPC_UpdatePower(int syncedPower)
        {
            currentPower = syncedPower;
        }


        private void PlayerLookCamera()
        {
            transform.rotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);
        }

        private void IsVisible()
        {
            RaycastHit rayHitPlayer;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);
            int mask = ~((1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("ChaseRange")) | (1 << LayerMask.NameToLayer("Dead")));

            if (Physics.Raycast(rayStart, mainCamera.transform.forward, out rayHitPlayer, cameraRaySize, mask))
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                if (rayHitPlayer.collider.gameObject.layer == playerLayer)
                {
                    isInvisibleFrontAndBack = false;
                    isVisible = true;
                    isCameraFront = 1;
                    return;
                }
            }
            isCameraFront = -1;
            isVisible = false;
        }

        private void IsVisibleFromBack()
        {
            if (!isVisible)
            {
                RaycastHit rayHitPlayer;
                Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2) * -1;
                int mask = ~((1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("ChaseRange")) | (1 << LayerMask.NameToLayer("Dead")));

                if (Physics.Raycast(rayStart, -mainCamera.transform.forward, out rayHitPlayer, cameraRaySize, mask))
                {
                    Debug.DrawRay(rayStart, -mainCamera.transform.forward * cameraRaySize, Color.black);

                    int playerLayer = LayerMask.NameToLayer("Player");
                    if (rayHitPlayer.collider.gameObject.layer == playerLayer)
                    {
                        isInvisibleFrontAndBack = false;
                        return;
                    }
                    isInvisibleFrontAndBack = true;
                }
            }
        }

        private bool IsGrounded(Vector3 position)
        {
            RaycastHit hit;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = 0.1f;
            return Physics.BoxCast(position, boxSize, Vector3.down, out hit, Quaternion.identity, 0.51f, LayerMask.GetMask("Platform"));
        }

        private void IsAir()
        {
            RaycastHit hit;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = 0.1f;

            // 땅에 닿아 있으면 grounded=true
            bool grounded = Physics.BoxCast(
                transform.position,
                boxSize,
                Vector3.down,
                out hit,
                Quaternion.identity,
                0.51f,
                LayerMask.GetMask("Platform")
            );

            bool newIsAir = !grounded;

            // 상태가 바뀔 때에만 처리
            if (newIsAir != lastAirState)
            {
                // ① 공중으로 떠오를 때 Y 저장
                if (newIsAir)
                {
                    _fallStartY = transform.position.y;
                }
                // ② 착지할 때 데미지 계산
                else
                {
                    float fallDistance = _fallStartY - transform.position.y;
                    if (fallDistance > 1f && photonView.IsMine)
                    {
                        int damage = Mathf.Clamp((int)fallDistance, 1, 50);
                        Debug.Log(damage + "낙하 데미지");
                        TakeDamage(damage);
                    }
                }
                isAir = newIsAir;
                animator.SetBool("isAir", newIsAir);

                // 내 캐릭터에서만 RPC 호출
                if (photonView.IsMine)
                {
                    photonView.RPC(
                        nameof(RPC_SetIsAir),
                        RpcTarget.Others,
                        newIsAir
                    );
                }

                lastAirState = newIsAir;
            }
        }


        private void InputKey()
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
            if (Input.GetButtonDown("Jump"))
            {
                jump = true;
            }

            if (Input.GetButtonDown("Fire1"))
            {
                if(!isControlLocked)
                attack = true;
            }
        }

        private void Move()
        {
            if (cameraScript.GetCameraRotating() || isControlLocked)
                return;

            Vector3 moveVec;

            if (climbState)
            {
                rigidBody.useGravity = false;
                moveVec = h * speed * 0.5f * mainCamera.transform.right + v * speed * 0.5f * Vector3.up;
            }
            else
            {
                rigidBody.useGravity = true;
                moveVec = new Vector3(0, rigidBody.linearVelocity.y, 0) + h * speed * mainCamera.transform.right;
            }

            rigidBody.linearVelocity = moveVec;
        }

        private void Jump()
        {
            if (cameraScript.GetCameraRotating())
            {
                if (!needRestoreVelocity)
                {
                    savedVelocity = rigidBody.linearVelocity;
                    needRestoreVelocity = true;
                }

                rigidBody.linearVelocity = Vector3.zero;
                rigidBody.useGravity = false;
                jump = false;
                climbJump = false;
                return;
            }
            else if (needRestoreVelocity)
            {
                rigidBody.linearVelocity = savedVelocity;
                needRestoreVelocity = false;
                rigidBody.useGravity = true;
            }

            if ((!isAir || climbJump) && jump)
            {
                rigidBody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            }

            jump = false;
            climbJump = false;
        }

        private void PlayerMoveAni()
        {
            if (h > 0)
            {
                if (spriteRenderer.flipX == true) // 원래 이 조건이었음
                {
                    photonView.RPC(nameof(SetFlipState), RpcTarget.AllBuffered, false);
                }
                animator.SetBool("isWalking", true);
            }
            else if (h < 0)
            {
                if (spriteRenderer.flipX == false)
                {
                    photonView.RPC(nameof(SetFlipState), RpcTarget.AllBuffered, true);
                }
                animator.SetBool("isWalking", true);
            }
            else if (v != 0)
            {
                animator.SetBool("isWalking", true);
            }
            else
            {
                animator.SetBool("isWalking", false);
            }
        }



        [PunRPC]
        private void SetFlipState(bool newFlip)
        {
            flipState = newFlip;
            spriteRenderer.flipX = newFlip;
        }

        private void RayTop()
        {
            if (!photonView.IsMine || isInvisibleFrontAndBack)
                return;

            float offset = 0.5f;

            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2) * isCameraFront;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = boxSize.y / 4f;

            Vector3 rayTopOffset = Vector3.up * (boxColider.size.y / 2f + boxSize.y);

            Debug.DrawRay(rayStart + rayTopOffset, mainCamera.transform.forward * isCameraFront * cameraRaySize, Color.yellow);

            if (rigidBody.linearVelocity.y > 0f)
            {
                if (Physics.BoxCast(rayStart + rayTopOffset, boxSize, mainCamera.transform.forward * isCameraFront, out RaycastHit hit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 target = rayStart + mainCamera.transform.forward.normalized * isCameraFront * hit.distance + hit.normal * offset;
                    if (!Physics.CheckBox(target, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                        transform.position = target;
                }
            }
        }

        private void RayDown()
        {
            if (!photonView.IsMine || isInvisibleFrontAndBack)
                return;

            float offset = 0.5f;

            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2) * isCameraFront;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = boxSize.y / 4f;
            boxSize.x = boxSize.x * 0.99f;

            Vector3 rayDownOffset = Vector3.up * (boxColider.size.y / 2f + boxSize.y);

            Debug.DrawRay(rayStart - rayDownOffset, mainCamera.transform.forward * isCameraFront * cameraRaySize, Color.yellow);

            if (rigidBody.linearVelocity.y < -0.01f)
            {
                if (Physics.BoxCast(rayStart - rayDownOffset, boxSize, mainCamera.transform.forward * isCameraFront, out RaycastHit hit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 target = rayStart + mainCamera.transform.forward.normalized * isCameraFront * hit.distance - hit.normal * offset;
                    if (!Physics.CheckBox(target, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                        transform.position = target;
                }
            }
        }

        private void RaySide(string leftRight)
        {
            if (!photonView.IsMine || isInvisibleFrontAndBack || cameraScript.GetCameraRotating())
                return;

            int dir = (leftRight == "Right") ? 1 : -1;

            if (h != dir && !isControlLocked)
                return;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.x = 0.15f;
            boxSize.y *= 0.98f;

            Vector3 sideOffset = mainCamera.transform.right * dir * (boxColider.size.x / 2f + boxSize.x);
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2) * isCameraFront;

            Debug.DrawRay(rayStart + sideOffset, mainCamera.transform.forward * isCameraFront * cameraRaySize, Color.red);
            Physics.BoxCast(rayStart + sideOffset, boxSize, mainCamera.transform.forward * isCameraFront, out RaycastHit sideHit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 downOffset = mainCamera.transform.right * dir * boxColider.size.x / 2f - mainCamera.transform.up * (boxColider.size.y / 2 + 0.01f);

            Debug.DrawRay(rayStart + downOffset, mainCamera.transform.forward * isCameraFront * cameraRaySize, Color.red);
            Physics.Raycast(rayStart + downOffset, mainCamera.transform.forward * isCameraFront, out RaycastHit downHit, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 playerBox = playerCollider.bounds.extents;
            playerBox.y *= 0.98f;
            float offset = 0.5f;

            if (sideHit.collider != null)
            {
                Vector3 target = rayStart + mainCamera.transform.forward.normalized * isCameraFront * sideHit.distance + sideHit.normal * offset;
                if (!Physics.CheckBox(target, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    if (!isAir && IsGrounded(target) || isAir)
                    {
                        transform.position = target;
                        return;
                    }
                }
            }

            if (downHit.collider != null)
            {
                Vector3 target = downHit.point - downHit.normal * offset - downOffset;
                if (!Physics.CheckBox(target, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    if (IsGrounded(target))
                        transform.position = target;
                }
            }
        }

        private bool CanClimb()
        {
            if (!isAir)
                return false;

            Vector3 rightOffset = mainCamera.transform.right * boxColider.size.x / 2f;
            Vector3 topOffset = Vector3.up * boxColider.size.y / 2f;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            Vector3[] offsets = new Vector3[]
            {
                topOffset + rightOffset,
                topOffset - rightOffset,
                -topOffset + rightOffset,
                -topOffset - rightOffset
            };

            int ivy = LayerMask.NameToLayer("Ivy");
            int mask = ~(1 << LayerMask.NameToLayer("Player"));

            foreach (var offset in offsets)
            {
                if (Physics.Raycast(rayStart + offset, mainCamera.transform.forward, out RaycastHit ivyHit, cameraRaySize, mask))
                {
                    if (ivyHit.collider.gameObject.layer == ivy)
                        return true;
                }
            }

            return false;
        }

        private void UpdateClimbState()
        {
            if (!photonView.IsMine) return;

            bool canClimb = CanClimb();

            if (climbState && Input.GetButtonDown("Jump"))
            {
                climbState = false;
                climbJump = true;
                animator.SetBool("isClimbIdle", false);
                photonView.RPC(nameof(RPC_SetClimbIdle), RpcTarget.Others, false);
                return;
            }

            if (Input.GetButtonDown("Jump") && canClimb)
            {
                climbState = true;
                animator.SetBool("isClimbIdle", true);
                photonView.RPC(nameof(RPC_SetClimbIdle), RpcTarget.Others, true);
            }

            if (climbState && !canClimb)
            {
                climbState = false;
                animator.SetBool("isClimbIdle", false);
                photonView.RPC(nameof(RPC_SetClimbIdle), RpcTarget.Others, false);
            }
        }

        private void StartAttack()
        {
            if (!attack || isControlLocked || climbState || isAttacking)
                return;
            animator.SetTrigger("doAttack");
            isAttacking = true;
        }

        //Animation Event Attack 0:03에서 호출
        private void AnimEvent_Hit()
        {
            // 서버 경유로(Room 안 모든 클라 + 내 클라) Hit() 실행
            photonView.RPC(nameof(Hit), RpcTarget.AllViaServer);
        }

        [PunRPC]
        private void Hit()
        {
            float attackDir = spriteRenderer.flipX ? -1f : 1f;

            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2) * isCameraFront;

            //player 중심으로부터 범위
            Vector3 attackRange = attackDir == 1 ? mainCamera.transform.right * 0.45f : -mainCamera.transform.right * 0.45f;
            //player 공격 상 하 범위
            Vector3 attackHalfHeight = Vector3.up * 0.1f;
            //공격 중심 시작 높이
            Vector3 attackHeight = Vector3.up * 0.1f;

            RaycastHit enemyHit;
            int enemy = LayerMask.NameToLayer("Enemy");
            int mask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("ChaseRange")));

            Vector3[] offsets = new Vector3[]
{
                attackHeight + attackRange + attackHalfHeight,
                attackHeight + attackRange - attackHalfHeight
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                //Debug.DrawRay(rayStart + offsets[i], mainCamera.transform.forward * isCameraFront * cameraRaySize, Color.blue);

                if (Physics.Raycast(rayStart + offsets[i], mainCamera.transform.forward * isCameraFront, out enemyHit, cameraRaySize, mask))
                {
                    if (enemyHit.collider.gameObject.layer == enemy)
                    {
                        float CorrectDir = Mathf.Abs(enemyHit.transform.eulerAngles.y) - Mathf.Abs(transform.eulerAngles.y);
                        if (CorrectDir % 180f == 0 ? true : false)
                        {
                            Monster monster = enemyHit.collider.GetComponent<Monster>();
                            if (monster != null)
                            {
                                monster.OnDamaged(transform.position, currentPower);
                            }
                            break;
                        }

                    }
                }
            }
        }

        //Animation Event Attack 0:06에서 호출
        private void EndAttack()
        {
            attack = false;
            isAttacking = false;
        }

        public void CheckCollisionDamage()
        {
            //판정 이상하면 조금 늘려야
            Vector3 rightOffset = mainCamera.transform.right * boxColider.size.x / 2f;
            //절반보다 조금 더 줄임
            Vector3 topOffset = Vector3.up * boxColider.size.y / 2.2f;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2) * isCameraFront;

            //player 꼭짓점에 ray
            Vector3[] offsets = new Vector3[]
            {
                //topRight
                topOffset + rightOffset,
                //topLeft
                topOffset - rightOffset,
                //downRight
                -topOffset + rightOffset,
                //downLeft
                -topOffset - rightOffset
            };

            RaycastHit enemyHit;
            int enemy = LayerMask.NameToLayer("Enemy");
            int mask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("ChaseRange")));

            foreach (var offset in offsets)
            {
                //null이 아니고
                if (Physics.Raycast(rayStart + offset, mainCamera.transform.forward * isCameraFront, out enemyHit, cameraRaySize, mask))
                {
                    if (enemyHit.collider.gameObject.layer == enemy)
                    {
                        if (isInvincible)
                        {
                            return;
                        }

                        //카메라에서 보여야만 -> 180도 차이나야
                        float CorrectDir = Mathf.Abs(enemyHit.transform.eulerAngles.y) - Mathf.Abs(transform.eulerAngles.y);
                        if (CorrectDir % 180f == 0 ? true : false)
                        {
                            Monster monster = enemyHit.collider.GetComponent<Monster>();
                            if (monster != null)
                            {
                                OnDamaged(enemyHit.transform.position, monster.stats.power);

                            }

                            break;
                        }
                    }
                }
            }
        }

        [PunRPC]
        public void OnDamaged(Vector3 monsterPos, int damageAmount)
        {
            if (!photonView.IsMine || isInvincible) return;
            currentHealth = Mathf.Max(0, currentHealth - damageAmount);
            UpdateHealthBar();

            //매달리기 상태 문제 해결
            if (climbState)
            {
                climbState = false;
                animator.SetBool("isClimbIdle", false);
                rigidBody.useGravity = true;
            }

            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
            animator.SetTrigger("isDamaged");
            photonView.RPC(nameof(RPC_StartDamageFlash), RpcTarget.Others);
            //플레이어 x로 튕겨야 할지 z로 튕겨야 할지 결정
            bool isXOrZ = Mathf.Abs(transform.eulerAngles.y) % 180 == 0 ? true : false;

            int dir;
            if (isXOrZ)
            {
                rigidBody.linearVelocity = Vector3.zero;
                dir = transform.position.x - monsterPos.x > 0 ? 1 : -1;
                Vector3 dirVec = new Vector3(dir, 4f, 0);
                rigidBody.AddForce(dirVec, ForceMode.Impulse);

            }
            else
            {
                rigidBody.linearVelocity = Vector3.zero;
                dir = transform.position.z - monsterPos.z > 0 ? 1 : -1;
                Vector3 dirVec = new Vector3(0, 4f, dir);
                rigidBody.AddForce(dirVec, ForceMode.Impulse);
            }
            isControlLocked = true;
            isInvincible = true;

            Invoke("UnlockControl", 0.5f);
            Invoke("OffDamaged", 1.5f);
            Invoke(nameof(SendEndFlashRPC), 1.5f);
            if (currentHealth <= 0)
                Die();
        }

        private void SendEndFlashRPC()
        {
            if (photonView.IsMine)
                photonView.RPC(nameof(RPC_EndDamageFlash), RpcTarget.Others);
        }

        void UnlockControl()
        {
            isControlLocked = false;
        }

        void OffDamaged()
        {
            spriteRenderer.color = new Color(1, 1, 1, 1);
            isInvincible = false;

            //attck = true하고 damaged = true 같은 프레임에 실행되면
            //StartAttack이 무시되거나? EndAttack ani 출력안되서 무시 됨.
            //일단 해결 위해서
            attack = false;
            isAttacking = false;
        }

        [PunRPC]
        public void RPC_SpawnItemEffect(int slotIndex, string prefabName, float duration)
        {
            // ① Resources 폴더 내에 effectPrefab을 넣어두고, 경로는 "Effects/"+prefabName 라고 가정
            var effectPrefab = Resources.Load<GameObject>($"Effects/{prefabName}");
            if (effectPrefab == null) return;

            // ② Instantiate + 회전 코루틴 + 파괴
            Transform spawnPoint = itemEffectSlots[slotIndex];
            GameObject go = Instantiate(effectPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            StartCoroutine(RotateEffect(go, duration));
            if (duration > 0f)
                Destroy(go, duration);
        }

        [PunRPC]
        public void RPC_SetIsAir(bool newIsAir)
        {
            animator.SetBool("isAir", newIsAir);
        }

        private IEnumerator RotateEffect(GameObject go, float duration)
        {
            float elapsed = 0f;
            while (go != null && (duration <= 0f || elapsed < duration))
            {
                go.transform.Rotate(_effectRotateAxis, _effectRotateSpeed * Time.deltaTime, Space.World);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        [PunRPC]
        private void RPC_StartDamageFlash()
        {
            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
            animator.SetTrigger("isDamaged");
        }

        // 2) 깜박거림 끝나고 원래 상태로 복구
        [PunRPC]
        private void RPC_EndDamageFlash()
        {
            spriteRenderer.color = new Color(1, 1, 1, 1f);
        }

        [PunRPC]
        private void RPC_SetClimbIdle(bool climbIdle)
        {
            animator.SetBool("isClimbIdle", climbIdle);
        }

        private void TryHeal()
        {
            // 내 것만 체크
            if (!photonView.IsMine) return;

            // 1) 씬에 있는 모든 NetworkPlayer를 찾는다
            var all = Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);

            // 2) 나를 제외하고, X축 거리 범위 안에 든 플레이어만 모은다
            List<NetworkPlayer> targets = new List<NetworkPlayer>();
            foreach (var other in all)
            {
                if (other == this) continue;
                if (Mathf.Abs(transform.position.x - other.transform.position.x) <= healRangeX)
                    targets.Add(other);
            }

            if (targets.Count == 0)
                return;

            // 4) 힐 시작 애니메이션 & RPC 전송
            animator.SetBool("isHealing", true);
            photonView.RPC(nameof(RPC_SetHealing), RpcTarget.OthersBuffered, true);

            // 5) 대상들에 RPC로 실제 힐
            foreach (var victim in targets)
            {
                victim.photonView.RPC(nameof(RPC_ReceiveHeal), RpcTarget.AllBuffered, healAmount);
            }

            // 6) 내 체력 소모
            currentHealth = Mathf.Max(0, currentHealth - healCost);
            UpdateHealthBar();
            photonView.RPC(nameof(RPC_UpdateHealth), RpcTarget.OthersBuffered, currentHealth);

            if (!isDead && currentHealth <= 0)
            {
                Die();
            }


            // 7) 애니 종료
            StartCoroutine(StopHealingAnim());
            
        }


        private IEnumerator StopHealingAnim()
        {
            yield return new WaitForSeconds(1f);
            animator.SetBool("isHealing", false);
            photonView.RPC(nameof(RPC_SetHealing), RpcTarget.OthersBuffered, false);
        }

        [PunRPC]
        private void RPC_ReceiveHeal(int amount)
        {
            currentHealth = Mathf.Min(playerStat.hp, currentHealth + amount);
            UpdateHealthBar();
        }

        [PunRPC]
        private void RPC_SetHealing(bool isHealing)
        {
            animator.SetBool("isHealing", isHealing);
        }

        private void TryPickupGold()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, goldPickupRange);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("GoldCoin"))
                {
                    var gold = hit.GetComponent<Gold>();
                    if (gold != null)
                        gold.Pickup();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;                            // 원 색상
            Gizmos.DrawWireSphere(transform.position, goldPickupRange);  // 반지름만큼 선으로 그리기

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, minFallRange);
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            
            animator.SetTrigger("isDead");   
            DisableControlAndCollisions();
            GameManager.instance.photonView.RPC("RPC_PlayerDied", RpcTarget.All);
            photonView.RPC(nameof(RPC_AllTriggerDeath), RpcTarget.AllBuffered);
        }

        [PunRPC]
        
        private void RPC_AllTriggerDeath()
        {
            animator.SetTrigger("isDead");
            DisableControlAndCollisions();
        }

        private void DisableControlAndCollisions()
        {
            // 이동·점프·공격 키 입력 무시
            isControlLocked = true;
            // 물리/충돌 끄기
            var cols = GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;
            rigidBody.isKinematic = true;
            // Raycast 같은 로직이 들어있는 메서드들도 isDead 체크로 빠져나가게
        }

        public void RespawnToStart()
        {
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;

            // 2) 위치 및 회전 복구
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            TakeDamage(50);

        }

        // 1) RPC로 총알 발사 가능 상태 활성화
        [PunRPC]
        public void RPC_EnableShooting(float duration)
        {
            StartCoroutine(ShootingDurationCoroutine(duration));
        }

        // 2) 지정된 시간만큼 isShootable 유지
        private IEnumerator ShootingDurationCoroutine(float duration)
        {
            isShootable = true;
            Debug.Log("isShootable = true");
            yield return new WaitForSeconds(duration);
            isShootable = false;
            Debug.Log("isShootable = false");
        }

        private void HandleShooting()
        {
            if (!photonView.IsMine || !isShootable)
                return;

            // 우클릭(마우스 오른쪽 버튼) 체크
            if (Input.GetMouseButtonDown(1))
            {
                // 1) 화면 클릭 지점으로 Ray 쏴서 월드 좌표 얻기
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Vector3 clickPos = hit.point;

                    // 2) 플레이어와 클릭 지점의 벡터 계산
                    Vector3 dirToClick = (clickPos - transform.position).normalized;

                    // 3) dot 연산으로 오른쪽(>0) / 왼쪽(<0) 판별
                    float sideDot = Vector3.Dot(dirToClick, transform.right);
                    float sideSign = sideDot >= 0f ? 1f : -1f;

                    // 4) 스폰 위치 계산 (옆 + 높이)
                    Vector3 spawnPos = transform.position
                                       + transform.right * sideSign * sideSpawnOffset;


                    // 5) 발사 방향은 클릭 지점을 향하도록 설정
                    Quaternion spawnRot = Quaternion.LookRotation(dirToClick);

                    // 6) PhotonNetwork로 동기화하여 생성
                    GameObject bullet = PhotonNetwork.Instantiate(
                        $"Bullets/{bulletPrefab.name}",
                        spawnPos,
                        spawnRot);

                    // 7) Bullet 컴포넌트 초기화 (방향, 파워 전달)
                    var b = bullet.GetComponent<Bullet>();
                    if (b != null)
                        b.Initialize(dirToClick);
                }
            }
        }

    }



}