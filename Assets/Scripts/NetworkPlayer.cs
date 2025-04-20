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
        private bool attack = false;
        private bool climbJump = false;
        private bool damged = false;

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
            PlayerAnimation();
            PlayerLookCamera();
        }

        private void FixedUpdate()
        {
            if (!photonView.IsMine || mainCamera == null || cameraScript == null)
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
            transform.rotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);
        }

        private bool IsVisible()
        {
            RaycastHit rayHitPlayer;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            if (Physics.Raycast(rayStart, mainCamera.transform.forward, out rayHitPlayer, cameraRaySize))
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                return rayHitPlayer.collider.gameObject.layer == playerLayer;
            }

            return false;
        }

        private bool IsGrounded(Vector3 position)
        {
            RaycastHit hit;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = 0.1f;
            return Physics.BoxCast(position, boxSize, Vector3.down, out hit, Quaternion.identity, 0.51f, LayerMask.GetMask("Platform"));
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
            if (cameraScript.GetCameraRotating() || damged)
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

            if ((IsGrounded(transform.position) || climbJump) && jump)
            {
                rigidBody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            }

            jump = false;
            climbJump = false;
        }

        private void PlayerAnimation()
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
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);
            Vector3 rayTopOffset = Vector3.up * 0.65f;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = rayTopOffset.y - 0.5f;

            Debug.DrawRay(rayStart + rayTopOffset, mainCamera.transform.forward * cameraRaySize, Color.green);

            if (rigidBody.linearVelocity.y > 0f)
            {
                if (Physics.BoxCast(rayStart + rayTopOffset, boxSize, mainCamera.transform.forward, out RaycastHit hit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 target = rayStart + mainCamera.transform.forward.normalized * hit.distance + hit.normal * offset;
                    if (!Physics.CheckBox(target, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                        transform.position = target;
                }
            }
        }

        private void RayDown()
        {
            if (!photonView.IsMine || !IsVisible())
                return;

            float offset = 0.5f;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);
            Vector3 rayDownOffset = Vector3.up * 0.65f;
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = rayDownOffset.y - 0.5f;

            Debug.DrawRay(rayStart - rayDownOffset, mainCamera.transform.forward * cameraRaySize, Color.green);

            if (rigidBody.linearVelocity.y < -0.01f)
            {
                if (Physics.BoxCast(rayStart - rayDownOffset, boxSize, mainCamera.transform.forward, out RaycastHit hit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform")))
                {
                    Vector3 target = rayStart + mainCamera.transform.forward.normalized * hit.distance - hit.normal * offset;
                    if (!Physics.CheckBox(target, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                        transform.position = target;
                }
            }
        }

        private void RaySide(string leftRight)
        {
            if (!photonView.IsMine || !IsVisible() || cameraScript.GetCameraRotating())
                return;

            int dir = (leftRight == "Right") ? 1 : -1;

            //없어도 될듯. test
            //if (h != dir)
            //    return;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.x = 0.15f;
            boxSize.y *= 0.98f;

            Vector3 sideOffset = mainCamera.transform.right * dir * 0.415f;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            Debug.DrawRay(rayStart + sideOffset, mainCamera.transform.forward * cameraRaySize, Color.red);
            Physics.BoxCast(rayStart + sideOffset, boxSize, mainCamera.transform.forward, out RaycastHit sideHit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 downOffset = mainCamera.transform.right * dir * 0.4f - mainCamera.transform.up * 0.51f;
            Debug.DrawRay(rayStart + downOffset, mainCamera.transform.forward * cameraRaySize, Color.red);
            Physics.Raycast(rayStart + downOffset, mainCamera.transform.forward, out RaycastHit downHit, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 playerBox = playerCollider.bounds.extents;
            playerBox.y *= 0.98f;

            if (sideHit.collider != null)
            {
                Vector3 target = rayStart + mainCamera.transform.forward.normalized * sideHit.distance + sideHit.normal * 0.5f;
                if (!Physics.CheckBox(target, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    if (IsGrounded(transform.position) && IsGrounded(target) || !IsGrounded(transform.position))
                    {
                        transform.position = target;
                        return;
                    }
                }
            }

            if (downHit.collider != null)
            {
                Vector3 target = downHit.point - downHit.normal * 0.5f - downOffset;
                if (!Physics.CheckBox(target, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    if (IsGrounded(target))
                        transform.position = target;
                }
            }
        }

        private bool CanClimb()
        {
            if (IsGrounded(transform.position))
                return false;

            Vector3 rightOffset = mainCamera.transform.right * 0.4f;
            Vector3 topOffset = Vector3.up * 0.5f;
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
            //콜라이더 하드코딩 0.25 0.45
            Vector3 rightOffset = mainCamera.transform.right * 0.25f;
            Vector3 topOffset = Vector3.up * 0.45f;
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
                        if (damged)
                        {
                            return;
                        }

                        float CorrectDir = Mathf.Abs(enemyHit.transform.eulerAngles.y) - Mathf.Abs(transform.eulerAngles.y);
                        if (CorrectDir % 180f == 0 ? true : false)
                        {
                            //매달리기 상태 문제 해결
                            climbState = false;
                            rigidBody.useGravity = true;

                            spriteRenderer.color = new Color(1, 1, 1, 0.4f);

                            bool isXOrZ = Mathf.Abs(transform.eulerAngles.y) % 180 == 0 ? true : false;

                            int dir;
                            if (isXOrZ)
                            {
                                rigidBody.linearVelocity = Vector3.zero;
                                dir = transform.position.x - enemyHit.transform.position.x > 0 ? 1 : -1;                                
                                Vector3 dirVec = new Vector3(dir, 4f, 0);
                                rigidBody.AddForce(dirVec, ForceMode.Impulse);

                            }
                            else
                            {
                                rigidBody.linearVelocity = Vector3.zero;
                                dir = transform.position.z - enemyHit.transform.position.z > 0 ? 1 : -1;
                                Vector3 dirVec =  new Vector3(0, 4f, dir);
                                rigidBody.AddForce(dirVec, ForceMode.Impulse);
                            }
                            damged = true;
                            Invoke("OffDamaged", 0.9f);
                        }
                    }
                }
            }
        }

        void OffDamaged()
        {
            spriteRenderer.color = new Color(1, 1, 1, 1);
            damged = false;
        }
    }
}
