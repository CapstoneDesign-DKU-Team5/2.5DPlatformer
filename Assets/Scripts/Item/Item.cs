
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemId;
    public string displayName;
    public string description;
    public Sprite icon;
    public bool consumable;

    [Tooltip("한 개 아이템당 사용 가능 횟수 (예: 충전형 물약 1개 = 3회)")]
    public int usesPerItem = 1;
}
