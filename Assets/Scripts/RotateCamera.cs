using UnityEngine;
using Photon.Pun;

public class RotateCamera : MonoBehaviour
{
    public Transform playerTransform;

    private float rotationAng = 0f;
    private float radius = 20f;
    private float rotationSpeed = 5f;
    private float moveSpeed = 8f;

    private bool angleRotating = false;
    private bool distanceRotating = false;

    private void Update()
    {
        if (playerTransform == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            angleRotating = true;
            distanceRotating = true;
            rotationAng += 90f;
            if (rotationAng > 270f) rotationAng = 0f;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            angleRotating = true;
            distanceRotating = true;
            rotationAng -= 90f;
            if (rotationAng < 0) rotationAng = 270f;
        }

        Vector3 offset = new Vector3(0, 0, -radius);
        Quaternion targetRot = Quaternion.Euler(0, rotationAng, 0);
        Vector3 finalPos = targetRot * offset + playerTransform.position;


        if (Quaternion.Angle(transform.rotation, targetRot) < 0.2f)
        {
            transform.rotation = targetRot;
            angleRotating = false;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        if (Vector3.Distance(transform.position, finalPos) < 0.02f)
        {
            transform.position = finalPos;
            distanceRotating = false;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * moveSpeed);
        }
    }

    public bool GetCameraRotating()
    {
        return angleRotating || distanceRotating;
    }
}
