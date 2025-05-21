using UnityEngine;

public class LobbyCamera : MonoBehaviour
{
    [Tooltip("ī�޶� ���� �÷��̾� Transform")]
    public Transform playerTransform;

    [Header("ī�޶� ȸ�� ����")]
    [SerializeField, Tooltip("���� ī�޶��� ȸ�� ���� (0, 90, 180, 270)")]
    private float rotationAng = 0f;

    [SerializeField, Tooltip("�÷��̾�κ����� ī�޶� �Ÿ�")]
    private float radius = 20f;

    [SerializeField, Tooltip("ī�޶� ȸ�� �ӵ�")]
    private float rotationSpeed = 5f;

    [SerializeField, Tooltip("ī�޶� ��ġ �̵� �ӵ�")]
    private float moveSpeed = 8f;

    [Header("ī�޶� ������ ����")]
    [SerializeField, Tooltip("�÷��̾� ���� ī�޶��� �߰� ������ (��: ���� ���� ��)")]
    private Vector3 cameraOffset = new Vector3(0, 5f, 0);

    [Header("ī�޶� ����")]
    [SerializeField, Tooltip("ī�޶� ȸ�� ������ ����")]
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