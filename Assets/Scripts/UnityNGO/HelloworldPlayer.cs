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

        public float speed = 2f;
        public float jumpHeight = 5f;
        private float cameraRaySize = 30f;

        // flipX 상태를 네트워크로 동기화하기 위한 네트워크 변수.
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

        private void OnDestroy()
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

        private void Update()
        {
            // 오직 소유자에서만 입력과 애니메이션 처리
            if (!IsOwner) return;

            Jump();
            PlayerAnimation();
            PlayerLookCamera();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            Move();
            RaySide("Right");
            RaySide("Left");
            RayTop();
            RayDown();
        }

        private void PlayerLookCamera()
        {
            transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        }

        private void Move()
        {
            float h = Input.GetAxisRaw("Horizontal");
            Vector3 currentVelocity = rigidBody.linearVelocity;
            Vector3 moveVec = new Vector3(0, currentVelocity.y, 0);
            moveVec += h * speed * Camera.main.transform.right;
            rigidBody.linearVelocity = moveVec;
        }

        private void Jump()
        {
            if (Input.GetButtonDown("Jump"))
            {
                rigidBody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            }
        }

        private void PlayerAnimation()
        {
            float h = Input.GetAxisRaw("Horizontal");
            bool isWalking = Mathf.Abs(h) > 0.01f;
            animator.SetBool("isWalking", isWalking);

            // flip 상태를 네트워크 변수로 동기화
            if (IsOwner)
            {
                bool flip = (h < 0);
                flipState.Value = flip;   // 네트워크를 통해 동기화됨
                spriteRenderer.flipX = flip; // 로컬 업데이트
            }
        }

        private bool IsVisible()
        {
            RaycastHit rayHitPlayer;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);
            Physics.Raycast(rayStartDefault, Camera.main.transform.forward, out rayHitPlayer, cameraRaySize);

            if (rayHitPlayer.collider == null) return false;

            int playerLayer = LayerMask.NameToLayer("Player");
            if (rayHitPlayer.collider.gameObject.layer != playerLayer) return false;

            return true;
        }

        private void RayTop()
        {
            if (!IsVisible()) return;

            float offset = 0.5f;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);
            Vector3 rayTopOffset = Vector3.up * 0.65f;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = rayTopOffset.y - 0.5f;

            Debug.DrawRay(rayStartDefault + rayTopOffset, Camera.main.transform.forward * cameraRaySize, Color.green);

            if (rigidBody.linearVelocity.y > 0f)
            {
                RaycastHit rayHitUp;
                if (Physics.BoxCast(rayStartDefault + rayTopOffset, boxSize, Camera.main.transform.forward, out rayHitUp, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 targetPosition = rayStartDefault + Camera.main.transform.forward.normalized * rayHitUp.distance + rayHitUp.normal * offset;
                    if (!Physics.CheckBox(targetPosition, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                    {
                        transform.position = targetPosition;
                    }
                }
            }
        }

        private void RayDown()
        {
            if (!IsVisible()) return;

            float offset = 0.5f;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);
            Vector3 rayDownOffset = Vector3.up * 0.65f;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = rayDownOffset.y - 0.5f;

            Debug.DrawRay(rayStartDefault - rayDownOffset, Camera.main.transform.forward * cameraRaySize, Color.green);

            if (rigidBody.linearVelocity.y < -0.01f)
            {
                RaycastHit rayHitDown;
                if (Physics.BoxCast(rayStartDefault - rayDownOffset, boxSize, Camera.main.transform.forward, out rayHitDown, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 targetPosition = rayStartDefault + Camera.main.transform.forward.normalized * rayHitDown.distance - rayHitDown.normal * offset;
                    if (!Physics.CheckBox(targetPosition, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                    {
                        transform.position = targetPosition;
                    }
                }
            }
        }

        private void RaySide(string leftRight)
        {
            if (!IsVisible()) return;

            float raySize = 30f;
            int dir = (leftRight == "Right") ? 1 : -1;
            float offset = 0.5f;

            float h = Input.GetAxisRaw("Horizontal");
            if (h != dir) return;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.x = 0.15f;
            boxSize.y *= 0.98f;

            Vector3 sideOffset = Camera.main.transform.right * dir * 0.415f;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (raySize / 2);

            Debug.DrawRay(rayStartDefault + sideOffset, Camera.main.transform.forward * raySize, Color.red);

            RaycastHit rayHitSidePlatform;
            if (Physics.BoxCast(rayStartDefault + sideOffset, boxSize, Camera.main.transform.forward, out rayHitSidePlatform, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
            {
                Vector3 downSideOffset = Camera.main.transform.right * dir * 0.4f - Camera.main.transform.up * 0.51f;
                Debug.DrawRay(rayStartDefault + downSideOffset, Camera.main.transform.forward * raySize, Color.red);

                RaycastHit rayHitDownSidePlatform;
                Physics.Raycast(rayStartDefault + downSideOffset, Camera.main.transform.forward, out rayHitDownSidePlatform, raySize, LayerMask.GetMask("Platform"));

                Vector3 playerBox = playerCollider.bounds.extents;
                playerBox.y *= 0.98f;

                Vector3 targetPosition1 = rayStartDefault + Camera.main.transform.forward.normalized * rayHitSidePlatform.distance + rayHitSidePlatform.normal * offset;
                if (!Physics.CheckBox(targetPosition1, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    if ((IsGrounded(transform.position) && IsGrounded(targetPosition1)) || !IsGrounded(transform.position))
                    {
                        transform.position = targetPosition1;
                        return;
                    }
                }

                if (rayHitDownSidePlatform.collider != null)
                {
                    Vector3 targetPosition2 = rayHitDownSidePlatform.point - rayHitDownSidePlatform.normal * offset - downSideOffset;
                    if (!Physics.CheckBox(targetPosition2, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                    {
                        if (IsGrounded(targetPosition2))
                        {
                            transform.position = targetPosition2;
                        }
                    }
                }
            }
        }

        private bool IsGrounded(Vector3 position)
        {
            RaycastHit hit;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = 0.1f;
            float raySize = 0.51f;
            return Physics.BoxCast(position, boxSize, Vector3.down, out hit, Quaternion.identity, raySize, LayerMask.GetMask("Platform"));
        }
    }
}
