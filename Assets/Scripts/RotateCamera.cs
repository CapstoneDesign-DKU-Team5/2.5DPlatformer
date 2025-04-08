using UnityEngine;

public class RotateCamera : MonoBehaviour
{
    public Transform playerTransform;

    //ȸ�� ���� 0 ~ 270
    private float rotationAng = 0f;
    //�÷��̾�� ī�޶� �Ÿ�
    private float radius = 20f;

    private float rotationSpeed = 5f;
    private float moveSpeed = 8f;

    private bool cameraRoating = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            cameraRoating = true;
            rotationAng += 90f;
            if (rotationAng > 270f)
            {
                rotationAng = 0f;
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            cameraRoating = true;
            rotationAng -= 90f;
            if (rotationAng < 0)
            {
                rotationAng = 270f;
            }
        }

        Vector3 start = new Vector3(0, 0, -radius);

        Quaternion rotationY = Quaternion.Euler(0, rotationAng, 0);

        //�� ���� �������� ī�޶� ȸ��
        Vector3 movedPos = rotationY * start;

        //�÷��̾� ��ġ�� ��ġ �缳��
        Vector3 finalPos = Vector3.zero;
        if (playerTransform != null)
        {
            finalPos = movedPos + playerTransform.position;
        }


        //ī�޶� ���� ȸ��
        if (Quaternion.Angle(transform.rotation, rotationY) < 0.2f)
        {
            transform.rotation = rotationY;
            cameraRoating = false;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, rotationY, Time.deltaTime * rotationSpeed);
        }

        //�÷��̾ ������ ī�޶� ȸ��
        if (Vector3.Distance(transform.position, finalPos) < 0.02f)
        {
            transform.position = finalPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * moveSpeed);
        }
    }

    public bool GetCameraRotating()
    {
        return cameraRoating;
    }
}