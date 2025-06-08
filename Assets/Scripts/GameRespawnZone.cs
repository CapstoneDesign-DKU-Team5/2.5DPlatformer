using HelloWorld;
using UnityEngine;

public class GameRespawnZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        NetworkPlayer player = other.GetComponent<NetworkPlayer>();
        if (player != null)
        {
            player.RespawnToStart();
        }
    }
}
