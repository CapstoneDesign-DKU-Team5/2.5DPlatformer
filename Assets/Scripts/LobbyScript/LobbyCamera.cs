using UnityEngine;

public class LobbyCamera : MonoBehaviour
{
    [Tooltip("카메라가 따라갈 플레이어 Transform")]
    public Transform playerTransform;

    [Header("카메라 회전 설정")]
    [SerializeField, Tooltip("현재 카메라의 회전 각도 (0, 90, 180, 270)")]
    private float rotationAng = 0f;

    [SerializeField, Tooltip("플레이어로부터의 카메라 거리")]
    private float radius = 20f;

    [SerializeField, Tooltip("카메라 회전 속도")]
    private float rotationSpeed = 5f;

    [SerializeField, Tooltip("카메라 위치 이동 속도")]
    private float moveSpeed = 8f;

    [Header("카메라 오프셋 설정")]
    [SerializeField, Tooltip("플레이어 기준 카메라의 추가 오프셋 (예: 높이 조절 등)")]
    private Vector3 cameraOffset = new Vector3(0, 5f, 0);

    [Header("카메라 상태")]
    [SerializeField, Tooltip("카메라가 회전 중인지 여부")]
    private bool cameraRotating = false;

    private void Update()
    {
        if (playerTransform == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            cameraRotating = true;
            rotationAng += 90f;
            if (rotationAng > 270f) rotationAng = 0f;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            cameraRotating = true;
            rotationAng -= 90f;
            if (rotationAng < 0) rotationAng = 270f;
        }

        Vector3 offset = Quaternion.Euler(0, rotationAng, 0) * new Vector3(0, 0, -radius);
        Vector3 finalPos = playerTransform.position + offset + cameraOffset;
        Quaternion targetRot = Quaternion.Euler(0, rotationAng, 0);

        if (Quaternion.Angle(transform.rotation, targetRot) < 0.2f)
        {
            transform.rotation = targetRot;
            cameraRotating = false;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

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
        return cameraRotating;
    }

    public (Vector3 forward, Vector3 right) GetForwardRight()
    {
        Quaternion rot = Quaternion.Euler(0, rotationAng, 0);
        return (rot * Vector3.forward, rot * Vector3.right);
    }

    public float GetRotationAngle()
    {
        return rotationAng;
    }
}