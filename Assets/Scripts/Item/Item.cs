using UnityEngine;

public enum ItemEffectType
{
    Heal,       // 체력 회복
    SpeedBoost, // 속도 증가
    Shield,     // 방어(쉴드) 부여
    DamageBuff, // 공격력 버프
    None
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemId;
    public string displayName;
    public string description;
    public Sprite icon;
    public bool consumable;
    public int usesPerItem = 1;

    [Header("=== 효과 타입 ===")]
    public ItemEffectType effectType = ItemEffectType.None;

    [Header("=== 효과 파라미터 예시 ===")]
    public int healAmount;         // effectType == Heal 일 때 회복량
    public float buffDuration;     // 버프 지속 시간 (초)
    public float speedMultiplier;  // effectType == SpeedBoost 일 때 속도 배율
    public int shieldAmount;       // effectType == Shield 일 때 부여될 쉴드 양
    public float damageMultiplier; // effectType == DamageBuff 일 때 공격 배율

    [Header("=== 이펙트 프리팹 참조 ===")]
    [SerializeField, Tooltip("이 아이템이 사용될 때 생성할 이펙트 프리팹")]
    private GameObject effectPrefab;

    public GameObject EffectPrefab => effectPrefab;
}
