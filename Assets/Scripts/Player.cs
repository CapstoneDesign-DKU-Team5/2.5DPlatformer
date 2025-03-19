using UnityEngine;

public class Player : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody rigidBody;
    private Collider playerCollider;  //collider ������ ��ħ?

    public float speed;
    public float jumpHeight;

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
        RayTopDown();
    }

    private void PlayerLookCamera()
    {
        transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
    }

    private bool isPlayerVisible()
    {
        RaycastHit rayHitPlayer;
        float raySize = 20f;

        Physics.Raycast(transform.position - Camera.main.transform.forward * (raySize / 2), Camera.main.transform.forward, out rayHitPlayer, raySize);
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

    //��ġ�� �Űܾ�...
    private void PlayerAnimation()
    {
        //�¿��̵� �ִϸ��̼�
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

        //���� �ִϸ��̼�
        //if (Input.GetButtonDown("Jump")){
        //    animator.SetBool("isJumping", true);
        //}
    }

    private void RayTopDown()
    {
        if (!isPlayerVisible())
            return;

        float offset = 0.5f;
        float raySize = 30f;
        Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (raySize / 2);
        //�ݶ��̴� y �� �ϵ� �ڵ� 0.65f
        Vector3 rayTopDownOffset = Vector3.up * 0.65f;

        Debug.DrawRay(rayStartDefault + rayTopDownOffset, Camera.main.transform.forward * raySize, new Color(0, 1, 0));
        Debug.DrawRay(rayStartDefault - rayTopDownOffset, Camera.main.transform.forward * raySize, new Color(0, 1, 0));       

        if (rigidBody.linearVelocity.y > 0f)
        {
            RaycastHit rayHitUp;
            Physics.Raycast(rayStartDefault + rayTopDownOffset, Camera.main.transform.forward, out rayHitUp, raySize, LayerMask.GetMask("Platform"));
            if (rayHitUp.collider != null)
            {
                //Debug.Log(rayHitUp.collider.gameObject.name);
                
                Vector3 targetPosition = rayHitUp.point + rayHitUp.normal * offset - rayTopDownOffset;
                //targetPosition.y = transform.position.y;

                //������ ��ġ�� �浹ü�� �ϳ��� �����ϸ� Physics.CheckBox�� true�� ��ȯ
                if (!Physics.CheckBox(targetPosition, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    transform.position = targetPosition;
                }
            }
        }

        Debug.Log("///////////////////////////////////");

        //rigidbody Collision Detection ���� �ʿ� continuous
        //�ϰ� �ӵ� ���ѵ� �ʿ��� ��
        //-0.01f �ϵ� �ڵ�
        if (rigidBody.linearVelocity.y < -0.01f)
        {
            RaycastHit rayHitDown;
            Physics.Raycast(rayStartDefault - rayTopDownOffset, Camera.main.transform.forward, out rayHitDown, raySize, LayerMask.GetMask("Platform"));
            if (rayHitDown.collider != null)
            {
                Debug.Log(rayHitDown.collider.gameObject.name);
                Debug.Log(rigidBody.linearVelocity.y);

                Vector3 targetPosition = rayHitDown.point - rayHitDown.normal * offset + rayTopDownOffset;
                //targetPosition.y = transform.position.y;

                Debug.Log(targetPosition);

                //������ ��ġ�� �浹ü�� �ϳ��� �����ϸ� Physics.CheckBox�� true�� ��ȯ
                if (!Physics.CheckBox(targetPosition, playerCollider.bounds.extents, Quaternion.identity, LayerMask.GetMask("Platform")))
                {
                    Debug.Log("Success");
                    transform.position = targetPosition;
                }
            }
        }
    }

    //ī�޶� ȸ�� �߿� �����̸� ����� �۵� x
    private void RaySide(string leftRight)
    {
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
        if (!isPlayerVisible())
            return;

        //�ݶ��̴� �ϵ��ڵ�1
        Vector3 sideOffset = Camera.main.transform.right * dir * 0.41f;
        Vector3 rayStartDefault = transform.position - Camera.main.transform.forward * (raySize / 2);

        Debug.DrawRay(rayStartDefault + sideOffset, Camera.main.transform.forward * raySize, new Color(1, 0, 0));
        RaycastHit rayHitSidePlatform;
        Physics.Raycast(rayStartDefault + sideOffset, Camera.main.transform.forward, out rayHitSidePlatform, raySize, LayerMask.GetMask("Platform"));


        //�ݶ��̴� �ϵ��ڵ�2
        Vector3 downSideOffset = Camera.main.transform.right * dir * 0.4f - Camera.main.transform.up * 0.51f;

        Debug.DrawRay(rayStartDefault + downSideOffset, Camera.main.transform.forward * raySize, new Color(1, 0, 0));
        RaycastHit rayHitDownSidePlatform;
        Physics.Raycast(rayStartDefault + downSideOffset, Camera.main.transform.forward, out rayHitDownSidePlatform, raySize, LayerMask.GetMask("Platform"));



        //1
        Vector3 playerBox = playerCollider.bounds.extents;
        playerBox.y *= 0.98f;

        if (rayHitSidePlatform.collider != null)
        {
            Vector3 targetPosition1 = rayHitSidePlatform.point + rayHitSidePlatform.normal * offset - sideOffset;

            if (!Physics.CheckBox(targetPosition1, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
            {
                if (isGrounded(transform.position) && isGrounded(targetPosition1) || !isGrounded(transform.position))
                {
                    transform.position = targetPosition1;
                    return;
                }
            }
        }



        //2
        if (rayHitDownSidePlatform.collider == null)
            return;
        Vector3 targetPosition2 = rayHitDownSidePlatform.point - rayHitDownSidePlatform.normal * offset - downSideOffset;

        if (!Physics.CheckBox(targetPosition2, playerBox, Quaternion.identity, LayerMask.GetMask("Platform")))
        {
            if (isGrounded(targetPosition2))
            {
                transform.position = targetPosition2;
            }
        }

    }

    private bool isGrounded(Vector3 position)
    {
        RaycastHit hit;

        Vector3 boxSize = playerCollider.bounds.extents;
        boxSize.y = 0.1f;
        Vector3 direction = Vector3.down;
        float raySize = 0.51f;

        return Physics.BoxCast(position, boxSize, direction, out hit, Quaternion.identity, raySize, LayerMask.GetMask("Platform"));
    }
}
