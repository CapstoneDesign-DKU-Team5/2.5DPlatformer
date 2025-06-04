using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStat", menuName = "Stats/PlayerStat")]
public class PlayerStat : ScriptableObject
{
    public float hp = 500f;
    public int power = 10;
}
