using Photon.Pun;
using UnityEngine;

namespace HelloWorld
{
    public class NetworkPlayer : MonoBehaviourPun
    {
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
        private bool climbJump = false;
        private Vector3 savedVelocity;
        private bool needRestoreVelocity = false;

        private bool flipState = false;

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            mainCamera = Camera.main;
            cameraScript = mainCamera.GetComponent<RotateCamera>();

            if (photonView.IsMine)
            {
                Camera.main.GetComponent<RotateCamera>().playerTransform = this.transform;

                
                transform.position = new Vector3(Random.Range(-2f, 2f), 3f, -3f);
            }
        }

        private void Update()
        {
            if (!photonView.IsMine)
                return;

            InputMove();
            InputJump();
            UpdateClimbState();
            PlayerAnimation();
            PlayerLookCamera();
        }

        private void FixedUpdate()
        {
            if (!photonView.IsMine)
                return;

            CanClimb();
            Move();
            Jump();
            RaySide("Right");
            RaySide("Left");
            RayTop();
            RayDown();
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
            return Physics.BoxCast(position, boxSize, Vector3.down, out hit, Quaternion.identity, 0.51f, LayerMask.GetMask("Platform"));
        }

        private void InputMove()
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        private void InputJump()
        {
            if (Input.GetButtonDown("Jump"))
            {
                jump = true;
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

        private void PlayerAnimation()
        {
            if (h > 0)
            {
                if (spriteRenderer.flipX != false)
                {
                    photonView.RPC(nameof(SetFlipState), RpcTarget.AllBuffered, false);
                }
                animator.SetBool("isWalking", true);
            }
            else if (h < 0)
            {
                if (spriteRenderer.flipX != true)
                {
                    photonView.RPC(nameof(SetFlipState), RpcTarget.AllBuffered, true);
                }
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
            if (!photonView.IsMine || !IsVisible())
                return;

            float offset = 0.5f;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);
            Vector3 rayTopOffset = Vector3.up * 0.65f;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = rayTopOffset.y - 0.5f;

            Debug.DrawRay(rayStartDefault + rayTopOffset, Camera.main.transform.forward * cameraRaySize, new Color(0, 1, 0));

            if (rigidBody.linearVelocity.y > 0f)
            {
                if (Physics.BoxCast(rayStartDefault + rayTopOffset, boxSize, Camera.main.transform.forward, out RaycastHit rayHitUp, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
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
            if (!photonView.IsMine || !IsVisible())
                return;

            float offset = 0.5f;
            Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);
            Vector3 rayDownOffset = Vector3.up * 0.65f;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = rayDownOffset.y - 0.5f;

            Debug.DrawRay(rayStartDefault - rayDownOffset, Camera.main.transform.forward * cameraRaySize, new Color(0, 1, 0));

            if (rigidBody.linearVelocity.y < -0.01f)
            {
                if (Physics.BoxCast(rayStartDefault - rayDownOffset, boxSize, Camera.main.transform.forward, out RaycastHit rayHitDown, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
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
            if (!photonView.IsMine || !IsVisible() || cameraScript.GetCameraRotating())
                return;

            int dir = (leftRight == "Right") ? 1 : -1;

            float h = Input.GetAxisRaw("Horizontal");
            if (h != dir)
                return;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.x = 0.15f;
            boxSize.y *= 0.98f;

            Vector3 sideOffset = mainCamera.transform.right * dir * 0.415f;
            Vector3 rayStartDefault = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            Debug.DrawRay(rayStartDefault + sideOffset, mainCamera.transform.forward * cameraRaySize, new Color(1, 0, 0));
            Physics.BoxCast(rayStartDefault + sideOffset, boxSize, mainCamera.transform.forward, out RaycastHit rayHitSidePlatform, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 downSideOffset = mainCamera.transform.right * dir * 0.4f - mainCamera.transform.up * 0.51f;
            Debug.DrawRay(rayStartDefault + downSideOffset, mainCamera.transform.forward * cameraRaySize, new Color(1, 0, 0));
            Physics.Raycast(rayStartDefault + downSideOffset, mainCamera.transform.forward, out RaycastHit rayHitDownSidePlatform, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 playerBox = playerCollider.bounds.extents;
            playerBox.y *= 0.98f;

            if (rayHitSidePlatform.collider != null)
            {
                Vector3 targetPosition1 = rayStartDefault + mainCamera.transform.forward.normalized * rayHitSidePlatform.distance + rayHitSidePlatform.normal * 0.5f;
                if (!Physics.CheckBox(targetPosition1, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    if (IsGrounded(transform.position) && IsGrounded(targetPosition1) || !IsGrounded(transform.position))
                    {
                        transform.position = targetPosition1;
                        return;
                    }
                }
            }

            if (rayHitDownSidePlatform.collider == null)
                return;

            Vector3 targetPosition2 = rayHitDownSidePlatform.point - rayHitDownSidePlatform.normal * 0.5f - downSideOffset;
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
                return false;

            Vector3 rightOffset = mainCamera.transform.right * 0.4f;
            Vector3 topOffset = Vector3.up * 0.5f;
            Vector3 rayStartDefault = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

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
                if (Physics.Raycast(rayStartDefault + offset, mainCamera.transform.forward, out RaycastHit ivyHit, cameraRaySize, mask))
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
            if (!photonView.IsMine)
                return;

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

    }
}
