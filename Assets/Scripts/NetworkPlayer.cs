using Photon.Pun;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using PlayFab;


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

        private Vector3 savedVelocity;
        private bool needRestoreVelocity = false;

        private bool isAttacking = false;
        private bool isAir = false;
        private bool isVisible = true;
        private bool isInvisibleFrontAndBack = false;
        private int isCameraFront = 1;

        private bool flipState = false;

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerCollider = GetComponent<Collider>();
            boxColider = GetComponent<BoxCollider>();

            jumpHeight = 7f;
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
            if (photonView.IsMine)
            {
                GameObject camObj = Instantiate(Resources.Load<GameObject>("CameraPrefab"));
                mainCamera = camObj.GetComponent<Camera>();
                cameraScript = camObj.GetComponent<RotateCamera>();
                cameraScript.playerTransform = transform;
            }
        }

        private void Update()
        {
            if (!photonView.IsMine || mainCamera == null || cameraScript == null)
                return;

            InputKey();
            UpdateClimbState();
            PlayerMoveAni();
            PlayerLookCamera();
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
            if (Physics.BoxCast(transform.position, boxSize, Vector3.down, out hit, Quaternion.identity, 0.51f, LayerMask.GetMask("Platform")))
            {
                isAir = false;
                animator.SetBool("isAir", false);
            }
            else
            {
                isAir = true;
                animator.SetBool("isAir", true);
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
                return;
            }

            if (Input.GetButtonDown("Jump") && canClimb)
            {
                climbState = true;
                animator.SetBool("isClimbIdle", true);
            }

            if (climbState && !canClimb)
            {
                climbState = false;
                animator.SetBool("isClimbIdle", false);
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
                                monster.OnDamaged(transform.position, 1);
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
                            OnDamaged(enemyHit.transform.position);
                            break;
                        }
                    }
                }
            }
        }

        public void OnDamaged(Vector3 monsterPos)
        {
            if (isInvincible)
            {
                return;
            }

            //매달리기 상태 문제 해결
            if (climbState)
            {
                climbState = false;
                animator.SetBool("isClimbIdle", false);
                rigidBody.useGravity = true;
            }

            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
            animator.SetTrigger("isDamaged");

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
    }
}
