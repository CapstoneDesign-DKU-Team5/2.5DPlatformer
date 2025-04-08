using UnityEngine;

public class RotateCamera : MonoBehaviour
{
    public Transform playerTransform;

    //회전 각도 0 ~ 270
    private float rotationAng = 0f;
    //플레이어와 카메라 거리
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

        //원 점을 기준으로 카메라 회전
        Vector3 movedPos = rotationY * start;

        //플레이어 위치로 위치 재설정
        Vector3 finalPos = movedPos + playerTransform.position;

        //카메라 각도 회전
        if (Quaternion.Angle(transform.rotation, rotationY) < 0.2f)
        {
            transform.rotation = rotationY;
            cameraRoating = false;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, rotationY, Time.deltaTime * rotationSpeed);
        }

        //플레이어를 축으로 카메라 회전
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