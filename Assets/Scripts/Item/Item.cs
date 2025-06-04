
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemId;
    public string displayName;
    public string description;
    public Sprite icon;
    public bool consumable;

    [Tooltip("�� �� �����۴� ��� ���� Ƚ�� (��: ������ ���� 1�� = 3ȸ)")]
    public int usesPerItem = 1;
}
