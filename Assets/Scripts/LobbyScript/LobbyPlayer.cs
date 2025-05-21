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

        [Header("UI")]
        [SerializeField] private TMP_Text usernameText;

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
            usernameText.text = PlayerPrefs.GetString("displayName", "Guest");
        }

        private void Update()
        {
            InputKey();
            IsAir();
            PlayerMoveAni();
            PlayerLookCamera();
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
            float offset = 0.5f;
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
            float offset = 0.5f;
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

            float offset = 0.5f;
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
    }
}
