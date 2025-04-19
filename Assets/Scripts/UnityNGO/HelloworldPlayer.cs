using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldPlayer : NetworkBehaviour
    {
        // 위치 동기화는 NetworkTransform이 처리합니다.
        // 애니메이션 동기화는 플레이어 프리팹에 NetworkAnimator 컴포넌트를 추가하세요.

        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Rigidbody rigidBody;
        private Collider playerCollider;

        private Camera mainCamera;
        private RotateCamera cameraScript;

        public float speed = 2f;
        public float jumpHeight = 5f;

        private float cameraRaySize = 30f;

        private bool climbState = false;

        private float h = 0;
        private float v = 0;

        private bool jump = false;
        private bool attack = false;
        private bool climbJump = false;

        private Vector3 savedVelocity;
        private bool needRestoreVelocity = false;

        private NetworkVariable<bool> flipState = new NetworkVariable<bool>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            // 서버에서 모든 플레이어의 스폰 위치를 할당합니다.
            if (IsServer)
            {
                // OwnerClientId가 로컬 클라이언트면(즉, 호스트이면) 호스트 스폰, 아니면 클라이언트 스폰 위치
                Vector3 spawnPosition = (OwnerClientId == NetworkManager.Singleton.LocalClientId)
                    ? new Vector3(0f, 3f, -3f)
                    : new Vector3(-1.5f, 3f, -3f);
                transform.position = spawnPosition;
            }

            // 로컬 플레이어의 경우 카메라를 따라가도록 설정
            if (IsOwner)
            {
                Camera.main.GetComponent<RotateCamera>().playerTransform = this.transform;
            }

            // flipState의 변경을 모든 인스턴스에서 반영하도록 이벤트 등록
            flipState.OnValueChanged += OnFlipStateChanged;
        }

        public override void OnDestroy()
        {
            flipState.OnValueChanged -= OnFlipStateChanged;
        }

        private void OnFlipStateChanged(bool oldValue, bool newValue)
        {
            // flip 상태를 반영합니다.
            spriteRenderer.flipX = newValue;
            
        }

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            // 물리 기반 이동을 위해 Rigidbody가 kinematic이 아니도록 설정합니다.
            rigidBody.isKinematic = false;
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            mainCamera = Camera.main;
            cameraScript = mainCamera.GetComponent<RotateCamera>();
        }

        private void Update()
        {
            // 오직 소유자에서만 입력과 애니메이션 처리
            if (!IsOwner)
                return;

            InputKey();
            UpdateClimbState();
            PlayerAnimation();
            PlayerLookCamera();
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
                return;

            CanClimb();
            Move();
            Jump();
            RaySide("Right");
            RaySide("Left");
            RayTop();
            RayDown();
            Attack();
            OnDamaged();
        }

        private void PlayerLookCamera()
        {
            transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        }

        private bool IsVisible()
        {
            RaycastHit rayHitPlayer;

            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);

            Physics.Raycast(rayStartDefault, Camera.main.transform.forward, out rayHitPlayer, cameraRaySize);
            if (rayHitPlayer.collider == null)
                return false;

            int playerLayer = LayerMask.NameToLayer("Player");
            if (rayHitPlayer.collider.gameObject.layer != playerLayer)
                return false;

            return true;
        }

        private bool IsGrounded(Vector3 position)
        {
            RaycastHit hit;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = 0.1f;

            Vector3 direction = Vector3.down;
            float raySize = 0.51f;

            return Physics.BoxCast(position, boxSize, direction, out hit, Quaternion.identity, raySize, LayerMask.GetMask("Platform"));
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
            if (cameraScript.GetCameraRotating())
                return;

            Vector3 moveVec;

            if (climbState)
            {
                rigidBody.useGravity = false;

                moveVec = new Vector3(0, 0, 0);
                moveVec += h * speed * 0.5f * mainCamera.transform.right;

                moveVec += v * speed * 0.5f * Vector3.up;
            }
            else
            {
                rigidBody.useGravity = true;

                moveVec = new Vector3(0, rigidBody.linearVelocity.y, 0);
                moveVec += h * speed * mainCamera.transform.right;
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
            else
            {
                if (needRestoreVelocity)
                {
                    rigidBody.linearVelocity = savedVelocity;
                    needRestoreVelocity = false;
                    rigidBody.useGravity = true;
                }
            }

            if (IsGrounded(transform.position) || climbJump)
            {
                if (jump)
                {
                    rigidBody.AddForce(Vector2.up * jumpHeight, ForceMode.Impulse);
                }
            }
            jump = false;
            climbJump = false;
        }

        //위치를 옮겨야...// 나중에
        private void PlayerAnimation()
        {
            if (h > 0)
            {
                spriteRenderer.flipX = false;
                animator.SetBool("isWalking", true);

                if (flipState.Value != false) { flipState.Value = false; }
            }
            else if (h < 0)
            {
                spriteRenderer.flipX = true;
                animator.SetBool("isWalking", true);
                if (flipState.Value != true) { flipState.Value = true; }
            }
            else
            {
                animator.SetBool("isWalking", false);
            }

            //점프 애니메이션
            //if (Input.GetButtonDown("Jump")){
            //    animator.SetBool("isJumping", true);
            //}
        }

        private void RayTop()
        {
            if (!IsVisible())
                return;

            float offset = 0.5f;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);

            //콜라이더 y 값 하드 코딩 0.5f + 0.15f
            Vector3 rayTopOffset = Vector3.up * 0.65f;

            Vector3 boxSize = playerCollider.bounds.extents;
            //콜라이더 y 값 하드 코딩 0.5f
            boxSize.y = rayTopOffset.y - 0.5f;

            Debug.DrawRay(rayStartDefault + rayTopOffset, Camera.main.transform.forward * cameraRaySize, new Color(0, 1, 0));

            if (rigidBody.linearVelocity.y > 0f)
            {
                RaycastHit rayHitUp;
                //카메라에서 플레이어 살짝 위쪽으로 Box Ray를 쏨
                Physics.BoxCast(rayStartDefault + rayTopOffset, boxSize, Camera.main.transform.forward, out rayHitUp, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform"));

                if (rayHitUp.collider != null)
                {
                    //충돌한 벽 앞쪽으로 좌표 설정
                    Vector3 targetPosition = rayStartDefault + Camera.main.transform.forward.normalized * rayHitUp.distance + rayHitUp.normal * offset;

                    //지정한 위치에 충돌체가 하나라도 존재하면 Physics.CheckBox는 true를 반환
                    if (!Physics.CheckBox(targetPosition, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                    {
                        transform.position = targetPosition;
                    }
                }
            }
        }

        private void RayDown()
        {
            if (!IsVisible())
                return;

            float offset = 0.5f;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);

            //콜라이더 y 값 하드 코딩 0.5f + 0.15f
            Vector3 rayDownOffset = Vector3.up * 0.65f;

            Vector3 boxSize = playerCollider.bounds.extents;
            //콜라이더 y 값 하드 코딩 0.5f
            boxSize.y = rayDownOffset.y - 0.5f;

            Debug.DrawRay(rayStartDefault - rayDownOffset, Camera.main.transform.forward * cameraRaySize, new Color(0, 1, 0));

            //rigidbody Collision Detection 설정 필요 continuous
            //하강 속도 제한도 필요할 듯
            //-0.01f 하드 코딩
            if (rigidBody.linearVelocity.y < -0.01f)
            {
                RaycastHit rayHitDown;
                //카메라에서 플레이어 살짝 아래쪽으로 Box Ray를 쏨
                Physics.BoxCast(rayStartDefault - rayDownOffset, boxSize, Camera.main.transform.forward, out rayHitDown, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform"));

                if (rayHitDown.collider != null)
                {
                    //플랫폼 위쪽으로 좌표 조정
                    Vector3 targetPosition = rayStartDefault + Camera.main.transform.forward.normalized * rayHitDown.distance - rayHitDown.normal * offset;

                    //지정한 위치에 충돌체가 하나라도 존재하면 Physics.CheckBox는 true를 반환
                    if (!Physics.CheckBox(targetPosition, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                    {
                        transform.position = targetPosition;
                    }
                }
            }
        }

        private void RaySide(string leftRight)
        {
            if (!IsVisible() || cameraScript.GetCameraRotating())
                return;

            int dir = 0;
            float offset = 0.5f;

            switch (leftRight)
            {
                case "Right":
                    dir = 1;
                    break;
                case "Left":
                    dir = -1;
                    break;
            }

            float h = Input.GetAxisRaw("Horizontal");
            if (h != dir)
                return;

            Vector3 boxSize = playerCollider.bounds.extents;
            //하드 코딩
            boxSize.x = 0.15f;
            boxSize.y *= 0.98f;

            //콜라이더 하드코딩1
            Vector3 sideOffset = mainCamera.transform.right * dir * 0.415f;
            Vector3 rayStartDefault = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            Debug.DrawRay(rayStartDefault + sideOffset, mainCamera.transform.forward * cameraRaySize, new Color(1, 0, 0));

            RaycastHit rayHitSidePlatform;
            Physics.BoxCast(rayStartDefault + sideOffset, boxSize, mainCamera.transform.forward, out rayHitSidePlatform, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform"));

            //콜라이더 하드코딩2
            Vector3 downSideOffset = mainCamera.transform.right * dir * 0.4f - mainCamera.transform.up * 0.51f;

            Debug.DrawRay(rayStartDefault + downSideOffset, mainCamera.transform.forward * cameraRaySize, new Color(1, 0, 0));

            RaycastHit rayHitDownSidePlatform;
            Physics.Raycast(rayStartDefault + downSideOffset, mainCamera.transform.forward, out rayHitDownSidePlatform, cameraRaySize, LayerMask.GetMask("Platform"));



            //1 좌우 좌표
            Vector3 playerBox = playerCollider.bounds.extents;
            playerBox.y *= 0.98f;

            if (rayHitSidePlatform.collider != null)
            {
                Vector3 targetPosition1 = rayStartDefault + mainCamera.transform.forward.normalized * rayHitSidePlatform.distance + rayHitSidePlatform.normal * offset;

                if (!Physics.CheckBox(targetPosition1, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    if (IsGrounded(transform.position) && IsGrounded(targetPosition1) || !IsGrounded(transform.position))
                    {
                        transform.position = targetPosition1;
                        return;
                    }
                }
            }

            //2 발판이 있어야 이동되는...
            if (rayHitDownSidePlatform.collider == null)
                return;
            Vector3 targetPosition2 = rayHitDownSidePlatform.point - rayHitDownSidePlatform.normal * offset - downSideOffset;

            if (!Physics.CheckBox(targetPosition2, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
            {
                if (IsGrounded(targetPosition2))
                {
                    transform.position = targetPosition2;
                }
            }

        }

        private bool CanClimb()
        {
            if (IsGrounded(transform.position))
            {
                return false;
            }

            //콜라이더 하드코딩 0.4 0.5
            Vector3 rightOffset = mainCamera.transform.right * 0.4f;
            Vector3 topOffset = Vector3.up * 0.5f;
            Vector3 rayStartDefault = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

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

            RaycastHit ivyHit;
            int ivy = LayerMask.NameToLayer("Ivy");
            int mask = ~(1 << LayerMask.NameToLayer("Player"));

            foreach (var offset in offsets)
            {
                //null이 아니고
                if (Physics.Raycast(rayStartDefault + offset, mainCamera.transform.forward, out ivyHit, cameraRaySize, mask))
                {
                    if (ivyHit.collider.gameObject.layer == ivy)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateClimbState()
        {
            bool canClimb = CanClimb();

            if (climbState && Input.GetButtonDown("Jump"))
            {
                climbState = false;
                climbJump = true;
                return;
            }

            if (Input.GetButtonDown("Jump") && canClimb)
            {
                climbState = true;
            }

            if (climbState && !canClimb)
            {
                climbState = false;
            }
        }

        private void Attack()
        {
            if (!attack)
                return;
        }

        public void OnDamaged()
        {
            //콜라이더 하드코딩 0.4 0.5
            Vector3 rightOffset = mainCamera.transform.right * 0.4f;
            Vector3 topOffset = Vector3.up * 0.5f;
            Vector3 rayStartDefault = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

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
            int mask = ~(1 << LayerMask.NameToLayer("Player"));

            foreach (var offset in offsets)
            {
                //null이 아니고
                if (Physics.Raycast(rayStartDefault + offset, mainCamera.transform.forward, out enemyHit, cameraRaySize, mask))
                {
                    if (enemyHit.collider.gameObject.layer == enemy)
                    {
                        Debug.Log("Damaged");
                        spriteRenderer.color = new Color(1, 1, 1, 0.4f);
                        bool isX = Mathf.Abs(transform.rotation.y) == 90 ? false : true;
                        int dirc;
                        if (isX)
                        {
                            dirc = transform.position.x - enemyHit.transform.position.x > 0 ? 1 : -1;
                            rigidBody.AddForce(new Vector3(dirc, 1, 0) * 7, ForceMode.Impulse);
                        }
                        else
                        {
                            dirc = transform.position.z - enemyHit.transform.position.z > 0 ? 1 : -1;
                            rigidBody.AddForce(new Vector3(0, 1, dirc) * 7, ForceMode.Impulse);
                        }
                    }
                }
            }
        }
    }
}
