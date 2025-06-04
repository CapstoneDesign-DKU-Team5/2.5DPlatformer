using TMPro;
using UnityEngine;

namespace HelloWorld
{
    public class LobbyPlayer : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody rigidBody;
        [SerializeField] private Collider playerCollider;

        [Header("Camera References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LobbyCamera cameraScript;

        [Header("Movement Settings")]
        [SerializeField] private float speed = 2f;
        [SerializeField] private float jumpHeight = 5f;
        [SerializeField] private float cameraRaySize = 30f;
        [SerializeField] private float interactDistance = 1.5f;

        [Header("UI")]
        [SerializeField] private TMP_Text usernameText;

        private Vector3 initialPosition;


        private BoxCollider boxColider;

        private float h = 0f;
        private float v = 0f;
        private bool jump = false;
        private bool isAir = false;

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerCollider = GetComponent<Collider>();
            boxColider = GetComponent<BoxCollider>();
        }

        private void Start()
        {
            initialPosition = transform.position;
            usernameText.text = PlayerPrefs.GetString("displayName", "Guest");
        }

        private void Update()
        {
            InputKey();
            IsAir();
            PlayerMoveAni();
            PlayerLookCamera();
            CheckDoorInteraction();
        }

        private void FixedUpdate()
        {
            Move();
            Jump();
            RayTop();
            RayDown();
            RayBack();
        }

        private void InputKey()
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
            if (Input.GetButtonDown("Jump"))
            {
                jump = true;
            }
        }

        private void Move()
        {
            Vector3 moveVec = new Vector3(0, rigidBody.linearVelocity.y, 0) + h * speed * GetForwardRight().right;
            rigidBody.linearVelocity = moveVec;
        }

        private void Jump()
        {
            if (!isAir && jump)
            {
                rigidBody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            }
            jump = false;
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

        private void PlayerMoveAni()
        {
            if (h > 0)
            {
                spriteRenderer.flipX = false;
                animator.SetBool("isWalking", true);
            }
            else if (h < 0)
            {
                spriteRenderer.flipX = true;
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

        private void PlayerLookCamera()
        {
            transform.rotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);
        }

        private void RayTop()
        {
            float offset = 0.3f;
            Vector3 forwardDir = GetForwardRight().forward;
            Vector3 rayStart = transform.position - forwardDir * (cameraRaySize / 2);
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = boxSize.y / 4f;
            Vector3 rayTopOffset = Vector3.up * (boxColider.size.y / 2f + boxSize.y);
            if (rigidBody.linearVelocity.y > 0f)
            {
                if (Physics.BoxCast(rayStart + rayTopOffset, boxSize, forwardDir, out RaycastHit hit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 target = rayStart + forwardDir * hit.distance + hit.normal * offset;
                    if (!Physics.CheckBox(target, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                        transform.position = target;
                }
            }
        }

        private void RayDown()
        {
            float offset = 0.3f;
            Vector3 forwardDir = GetForwardRight().forward;
            Vector3 rayStart = transform.position - forwardDir * (cameraRaySize / 2);
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = boxSize.y / 4f;
            Vector3 rayDownOffset = Vector3.up * (boxColider.size.y / 2f + boxSize.y);
            if (rigidBody.linearVelocity.y < -0.01f)
            {
                if (Physics.BoxCast(rayStart - rayDownOffset, boxSize, forwardDir, out RaycastHit hit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 target = rayStart + forwardDir * hit.distance - hit.normal * offset;
                    if (!Physics.CheckBox(target, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                        transform.position = target;
                }
            }
        }

        private void RayBack()
        {
            if (!isAir) return;

            float offset = 0.3f;
            Vector3 forwardDir = GetForwardRight().forward;
            Vector3 rayStart = transform.position;
            Vector3 boxSize = playerCollider.bounds.extents;

            if (Physics.BoxCast(rayStart, boxSize, -forwardDir, out RaycastHit hit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
            {
                Vector3 target = rayStart - forwardDir * hit.distance + hit.normal * offset;
                if (!Physics.CheckBox(target, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                    transform.position = target;
            }
        }

        private (Vector3 forward, Vector3 right) GetForwardRight()
        {
            float angle = mainCamera.transform.eulerAngles.y;
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            return (rot * Vector3.forward, rot * Vector3.right);
        }

        private void CheckDoorInteraction()
        {
            // 1) 카메라 기준 전방·후방·오른쪽·왼쪽 방향 벡터 계산
            Vector3 forwardDir = GetForwardRight().forward;
            Vector3 rightDir = GetForwardRight().right;
            Vector3 backDir = -forwardDir;
            Vector3 leftDir = -rightDir;

            // 2) 플레이어 몸체 중앙(허리 높이 정도)에서 Ray 시작
            Vector3 rayOrigin = transform.position + Vector3.up * (playerCollider.bounds.extents.y * 0.5f);

            // 3) 4개 방향을 배열에 담기
            Vector3[] checkDirs = new Vector3[] { forwardDir, backDir, rightDir, leftDir };

            foreach (Vector3 dir in checkDirs)
            {
                // 4) 각 방향으로 interactDistance만큼 Raycast
                if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, interactDistance))
                {
                    // 5) Ray가 닿은 오브젝트의 상위 중 DoorInteraction 컴포넌트가 있는지 검사
                    DoorInteraction doorScript = hit.collider.GetComponentInParent<DoorInteraction>();
                    if (doorScript != null)
                    {
                        // 6) 위쪽 방향키 입력 시 상호작용 호출
                        if (Input.GetKeyDown(KeyCode.UpArrow))
                        {
                            doorScript.OpenOrInteract();
                        }
                        // 한 번이라도 문을 발견했다면, 더 이상 다른 방향을 확인할 필요 없으므로 break
                        break;
                    }
                }

                // 디버그 시각화를 위해 네 방향 Ray를 시각적으로 표시 (Scene 뷰에만 보임)
                Debug.DrawRay(rayOrigin, dir * interactDistance, Color.green);
            }
        }
        public void RespawnToStart()
        {
            rigidBody.linearVelocity = Vector3.zero; // 낙하 중일 경우 멈추기
            transform.position = initialPosition;
        }


    }


}
