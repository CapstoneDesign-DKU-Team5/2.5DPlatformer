using UnityEngine;

public enum ItemEffectType
{
    Heal,       // ü�� ȸ��
    SpeedBoost, // �ӵ� ����
    Shield,     // ���(����) �ο�
    DamageBuff, // ���ݷ� ����
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

    [Header("=== ȿ�� Ÿ�� ===")]
    public ItemEffectType effectType = ItemEffectType.None;

    [Header("=== ȿ�� �Ķ���� ���� ===")]
    public int healAmount;         // effectType == Heal �� �� ȸ����
    public float buffDuration;     // ���� ���� �ð� (��)
    public float speedMultiplier;  // effectType == SpeedBoost �� �� �ӵ� ����
    public int shieldAmount;       // effectType == Shield �� �� �ο��� ���� ��
    public float damageMultiplier; // effectType == DamageBuff �� �� ���� ����

    [Header("=== ����Ʈ ������ ���� ===")]
    [SerializeField, Tooltip("�� �������� ���� �� ������ ����Ʈ ������")]
    private GameObject effectPrefab;

    public GameObject EffectPrefab => effectPrefab;
}
