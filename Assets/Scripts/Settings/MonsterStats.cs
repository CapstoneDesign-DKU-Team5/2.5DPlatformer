using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterStats", menuName = "Monster/Stats")]
public class MonsterStats : ScriptableObject
{
    [Header("�⺻ ����")]
    public int hp;
    public int power;

    [Header("����ǰ")]
    [Tooltip("�� ���͸� óġ���� �� ����� ��� ��")]
    public int goldDrop;
}