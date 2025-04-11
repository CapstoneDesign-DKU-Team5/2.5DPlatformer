using UnityEngine;
using Photon.Pun;

public class RotateCamera : MonoBehaviour
{
    public Transform playerTransform;

    private float rotationAng = 0f;
    private float radius = 20f;
    private float rotationSpeed = 5f;
    private float moveSpeed = 8f;

<<<<<<< Updated upstream
=======
    private bool cameraRotating = false;

>>>>>>> Stashed changes
    private void Update()
    {
        if (playerTransform == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
<<<<<<< Updated upstream
            rotationAng += 90f;
            if(rotationAng > 270f)
            {
                rotationAng = 0f;
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
=======
            cameraRotating = true;
            rotationAng += 90f;
            if (rotationAng > 270f) rotationAng = 0f;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            cameraRotating = true;
>>>>>>> Stashed changes
            rotationAng -= 90f;
            if (rotationAng < 0) rotationAng = 270f;
        }

        Vector3 offset = new Vector3(0, 0, -radius);
        Quaternion targetRot = Quaternion.Euler(0, rotationAng, 0);
        Vector3 finalPos = targetRot * offset + playerTransform.position;

        if (Quaternion.Angle(transform.rotation, targetRot) < 0.2f)
        {
<<<<<<< Updated upstream
            transform.rotation = rotationY;
=======
            transform.rotation = targetRot;
            cameraRotating = false;
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
}
=======

    public bool GetCameraRotating()
    {
        return cameraRotating;
    }
}
>>>>>>> Stashed changes
