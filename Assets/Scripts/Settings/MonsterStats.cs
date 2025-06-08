using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterStats", menuName = "Monster/Stats")]
public class MonsterStats : ScriptableObject
{
    [Header("기본 스탯")]
    public int hp;
    public int power;

    [Header("전리품")]
    [Tooltip("이 몬스터를 처치했을 때 드롭할 골드 양")]
    public int goldDrop;
}