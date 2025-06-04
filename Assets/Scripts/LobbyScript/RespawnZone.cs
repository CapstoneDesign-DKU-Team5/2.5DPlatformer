using HelloWorld;
using UnityEngine;

public class RespawnZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        LobbyPlayer player = other.GetComponent<LobbyPlayer>();
        if (player != null)
        {
            player.RespawnToStart();
        }
    }
}
