using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rigidBody;
    private Collider playerCollider;  //collider 변수명 겹침?

    public float speed;
    public float jumpHeight;

    private float cameraRaySize = 30f;  //카메라에서 쏘는 ray 길이

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider>();

        speed = 2;
        jumpHeight = 5;
    }

    private void Update()
    {
        Jump();
        PlayerAnimation();
        PlayerLookCamera();
    }

    private void FixedUpdate()
    {
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

    private void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");

        Vector3 moveVec = new Vector3(0, rigidBody.linearVelocity.y, 0);
        moveVec += h * speed * Camera.main.transform.right;

        rigidBody.linearVelocity = moveVec;
    }

    private void Jump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            rigidBody.AddForce(Vector2.up * jumpHeight, ForceMode.Impulse);
        }
    }

    //위치를 옮겨야...// 나중에
    private void PlayerAnimation()
    {
        //좌우이동 애니메이션
        float h = Input.GetAxisRaw("Horizontal");
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
        else
        {
            animator.SetBool("isWalking", false);
        }

        //점프 애니메이션
        //if (Input.GetButtonDown("Jump")){
        //    animator.SetBool("isJumping", true);
        //}
    }

    //카메라 회전 중 게임 멈춰야

    private void RayTop()
    {
        if (!IsVisible())
            return;

        float offset = 0.5f;
        Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (cameraRaySize / 2);

        //콜라이더 y 값 하드 코딩 0.5f + 0.1f
        Vector3 rayTopOffset = Vector3.up * 0.6f;

        Vector3 boxSize = playerCollider.bounds.extents;
        //콜라이더 y 값 하드 코딩 0.5f
        boxSize.y = rayTopOffset.y - 0.5f;

        //Debug.DrawRay(rayStartDefault + rayTopOffset, Camera.main.transform.forward * cameraRaySize, new Color(0, 1, 0));

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

        //콜라이더 y 값 하드 코딩 0.5f + 0.1f
        Vector3 rayDownOffset = Vector3.up * 0.6f;

        Vector3 boxSize = playerCollider.bounds.extents;
        //콜라이더 y 값 하드 코딩 0.5f
        boxSize.y = rayDownOffset.y - 0.5f;

        //Debug.DrawRay(rayStartDefault - rayDownOffset, Camera.main.transform.forward * cameraRaySize, new Color(0, 1, 0));

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
        if (!IsVisible())
            return;

        float raySize = 30f;
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

        //콜라이더 하드코딩1
        Vector3 sideOffset = Camera.main.transform.right * dir * 0.41f;
        Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (raySize / 2);

        Debug.DrawRay(rayStartDefault + sideOffset, Camera.main.transform.forward * raySize, new Color(1, 0, 0));
        RaycastHit rayHitSidePlatform;
        Physics.Raycast(rayStartDefault + sideOffset, Camera.main.transform.forward, out rayHitSidePlatform, raySize, LayerMask.GetMask("Platform"));


        //콜라이더 하드코딩2
        Vector3 downSideOffset = Camera.main.transform.right * dir * 0.4f - Camera.main.transform.up * 0.51f;

        Debug.DrawRay(rayStartDefault + downSideOffset, Camera.main.transform.forward * raySize, new Color(1, 0, 0));
        RaycastHit rayHitDownSidePlatform;
        Physics.Raycast(rayStartDefault + downSideOffset, Camera.main.transform.forward, out rayHitDownSidePlatform, raySize, LayerMask.GetMask("Platform"));



        //1 좌우 좌표
        Vector3 playerBox = playerCollider.bounds.extents;
        playerBox.y *= 0.98f;

        if (rayHitSidePlatform.collider != null)
        {
            Vector3 targetPosition1 = rayHitSidePlatform.point + rayHitSidePlatform.normal * offset - sideOffset;

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

    private bool IsGrounded(Vector3 position)
    {
        RaycastHit hit;

        Vector3 boxSize = playerCollider.bounds.extents;
        boxSize.y = 0.1f;
        Vector3 direction = Vector3.down;
        float raySize = 0.51f;

        return Physics.BoxCast(position, boxSize, direction, out hit, Quaternion.identity, raySize, LayerMask.GetMask("Platform"));
    }
}
