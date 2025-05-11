using Photon.Pun;
using TMPro;
using UnityEngine;

namespace HelloWorld
{
    public class NetworkPlayer : MonoBehaviourPun
    {
        [Header("������Ʈ ����")]
        [SerializeField, Tooltip("ĳ���� �ִϸ��̼��� �����ϴ� Animator")]
        private Animator animator;

        [SerializeField, Tooltip("��������Ʈ ���� �� ���� ���濡 ���Ǵ� SpriteRenderer")]
        private SpriteRenderer spriteRenderer;

        [SerializeField, Tooltip("���� ���꿡 ���Ǵ� Rigidbody")]
        private Rigidbody rigidBody;

        [SerializeField, Tooltip("�÷��̾� ��ü�� ��Ÿ���� �� Collider")]
        private Collider playerCollider;

        [Header("ī�޶� ����")]
        [SerializeField, Tooltip("�÷��̾ ����ٴϴ� ���� ī�޶�")]
        private Camera mainCamera;

        [SerializeField, Tooltip("ī�޶� ȸ���� ����ϴ� ��ũ��Ʈ")]
        private RotateCamera cameraScript;

        [Header("�̵� ����")]
        [SerializeField, Tooltip("���� �̵� �ӵ� (����/��)")]
        private float speed = 2f;

        [SerializeField, Tooltip("���� �� ������ �� (ForceMode.Impulse)")]
        private float jumpHeight = 5f;

        [SerializeField, Tooltip("���� ����ĳ��Ʈ ������ ����")]
        private float cameraRaySize = 30f;

        private BoxCollider boxColider;

        // ���� ��Ÿ�� ������������������������������������������������������������
        private bool climbState = false;

        private float h = 0f;
        private float v = 0f;

        private bool jump = false;
        private bool attack = false;
        private bool climbJump = false;
        private bool damaged = false;

        private Vector3 savedVelocity;
        private bool needRestoreVelocity = false;

        private bool isAttacking = false;
        private bool isAir = false;
        private bool isVisible = true;

        private bool flipState = false;

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
            IsAir();

            Move();
            Jump();
            RaySide("Right");
            RaySide("Left");
            RayTop();
            RayDown();
            StartAttack();
            OnDamaged();
        }

        private void PlayerLookCamera()
        {
            transform.rotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);
        }

        private void IsVisible()
        {
            RaycastHit rayHitPlayer;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            if (Physics.Raycast(rayStart, mainCamera.transform.forward, out rayHitPlayer, cameraRaySize))
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                if(rayHitPlayer.collider.gameObject.layer == playerLayer)
                {
                    isVisible = true;
                    return;
                }
                //return rayHitPlayer.collider.gameObject.layer == playerLayer;
            }
            isVisible = false;
            //return false;
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
            if (cameraScript.GetCameraRotating() || damaged)
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

            //bool noSameFrame = false;
            if ((!isAir || climbJump) && jump)
            {
                rigidBody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
                //noSameFrame = true;
            }

            //if (!isAir && rigidBody.linearVelocity.y < 0 && !noSameFrame) 
            //{
            //}

            jump = false;
            climbJump = false;
        }

        private void PlayerMoveAni()
        {
            if (h > 0)
            {
                if (spriteRenderer.flipX == true) // ���� �� �����̾���
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
            if (!photonView.IsMine || !isVisible)
                return;

            float offset = 0.5f;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = boxSize.y / 4f;
            //boxSize.x = boxSize.x / 2f;
            Vector3 rayTopOffset = Vector3.up * (boxColider.size.y / 2f + boxSize.y);


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
            if (!photonView.IsMine || !isVisible)
                return;

            float offset = 0.5f;
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);            
            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.y = boxSize.y / 4f;
            boxSize.x = boxSize.x * 0.99f;
            Vector3 rayDownOffset = Vector3.up * (boxColider.size.y / 2f + boxSize.y);

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
            if (!photonView.IsMine || !isVisible || cameraScript.GetCameraRotating())
                return;

            int dir = (leftRight == "Right") ? 1 : -1;

            if (h != dir && !damaged)
                return;

            Vector3 boxSize = playerCollider.bounds.extents;
            boxSize.x = 0.15f;
            boxSize.y *= 0.98f;

            Vector3 sideOffset = mainCamera.transform.right * dir * (boxColider.size.x / 2f + boxSize.x);
            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            Debug.DrawRay(rayStart + sideOffset, mainCamera.transform.forward * cameraRaySize, Color.red);
            Physics.BoxCast(rayStart + sideOffset, boxSize, mainCamera.transform.forward, out RaycastHit sideHit, Quaternion.identity, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 downOffset = mainCamera.transform.right * dir * boxColider.size.x / 2f - mainCamera.transform.up * (boxColider.size.y / 2 + 0.01f);
            Debug.DrawRay(rayStart + downOffset, mainCamera.transform.forward * cameraRaySize, Color.red);
            Physics.Raycast(rayStart + downOffset, mainCamera.transform.forward, out RaycastHit downHit, cameraRaySize, LayerMask.GetMask("Platform"));

            Vector3 playerBox = playerCollider.bounds.extents;
            playerBox.y *= 0.98f;
            float offset = 0.5f;

            if (sideHit.collider != null)
            {
                Vector3 target = rayStart + mainCamera.transform.forward.normalized * sideHit.distance + sideHit.normal * offset;
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
            if (!attack || damaged || climbState || isAttacking)
                return;
            animator.SetTrigger("doAttack");
            isAttacking = true;
        }

        //Animation Event Attack 0:03���� ȣ��
        private void AnimEvent_Hit()
        {
            // ���� ������(Room �� ��� Ŭ�� + �� Ŭ��) Hit() ����
            photonView.RPC(nameof(Hit), RpcTarget.AllViaServer);
        }

        [PunRPC]
        private void Hit()
        {
            float attackDir = spriteRenderer.flipX ? -1f : 1f;

            Vector3 rayStart = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            //player �߽����κ��� ����
            Vector3 attackRange = attackDir == 1 ? mainCamera.transform.right * 0.45f : -mainCamera.transform.right * 0.45f;
            //player ���� �� �� ����
            Vector3 attackHalfHeight = Vector3.up * 0.1f;
            //���� �߽� ���� ����
            Vector3 attackHeight = Vector3.up * 0.1f;

            RaycastHit[] enemyHits = new RaycastHit[2];
            int enemy = LayerMask.NameToLayer("Enemy");
            int mask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("ChaseRange")));

            Vector3[] offsets = new Vector3[]
{
                attackHeight + attackRange + attackHalfHeight,
                attackHeight + attackRange - attackHalfHeight
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Debug.DrawRay(rayStart + offsets[i], mainCamera.transform.forward * cameraRaySize, Color.blue);

                if (Physics.Raycast(rayStart + offsets[i], mainCamera.transform.forward, out enemyHits[i], cameraRaySize, mask))
                {
                    if (enemyHits[i].collider.gameObject.layer == enemy)
                    {
                        Monster monster = enemyHits[i].collider.GetComponent<Monster>();
                        if (monster != null)
                        {
                            monster.OnDamaged(transform.position);
                        }

                        break;
                    }
                }
            }
        }

        //Animation Event Attack 0:06���� ȣ��
        private void EndAttack()
        {
            attack = false;
            isAttacking = false;
        }

        public void OnDamaged()
        {
            //���� �̻��ϸ� ���� �÷���
            Vector3 rightOffset = mainCamera.transform.right * boxColider.size.x / 2f;
            //���ݺ��� ���� �� ����
            Vector3 topOffset = Vector3.up * boxColider.size.y / 2.2f;
            Vector3 rayStartDefault = transform.position - mainCamera.transform.forward * (cameraRaySize / 2);

            //player �������� ray
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
                //null�� �ƴϰ�
                if (Physics.Raycast(rayStartDefault + offset, mainCamera.transform.forward, out enemyHit, cameraRaySize, mask))
                {
                    if (enemyHit.collider.gameObject.layer == enemy)
                    {
                        if (damaged)
                        {
                            return;
                        }

                        //ī�޶󿡼� �����߸� -> 180�� ���̳���
                        float CorrectDir = Mathf.Abs(enemyHit.transform.eulerAngles.y) - Mathf.Abs(transform.eulerAngles.y);
                        if (CorrectDir % 180f == 0 ? true : false)
                        {
                            //�Ŵ޸��� ���� ���� �ذ�
                            if (climbState)
                            {
                                climbState = false;
                                animator.SetBool("isClimbIdle", false);
                                rigidBody.useGravity = true;
                            }
                            
                            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
                            animator.SetTrigger("isDamaged");

                            //�÷��̾� x�� ƨ�ܾ� ���� z�� ƨ�ܾ� ���� ����
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
                            damaged = true;
                            Invoke("OffDamaged", 0.9f);
                        }
                    }
                }
            }
        }

        void OffDamaged()
        {
            spriteRenderer.color = new Color(1, 1, 1, 1);
            damaged = false;

            //attck = true�ϰ� damaged = true ���� �����ӿ� ����Ǹ�
            //StartAttack�� ���õǰų�? EndAttack ani ��¾ȵǼ� ���� ��.
            //�ϴ� �ذ� ���ؼ�
            attack = false;
            isAttacking = false;
        }
    }
}
