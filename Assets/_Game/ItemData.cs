using UnityEngine;

public enum ItemType
{
    /// <summary>먹으면 사라진다 (물약).</summary>
    Consumable,
    Weapon,
    Armor
}

/// <summary>
/// 아이템 하나의 정의. 아이템마다 에셋을 하나씩 만든다 (Item_SmallPotion.asset ...).
/// 몇 개 들고 있는지·무엇을 착용했는지 같은 상태는 PlayerInventory가 들고 있는다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Item")]
public class ItemData : ScriptableObject
{
    [Header("공통")]
    public string displayName = "Item";
    public Sprite icon;
    public ItemType type = ItemType.Consumable;
    [Tooltip("상점 구매 가격(골드).")]
    [Min(0)] public int price = 10;

    [Header("소비 (Consumable)")]
    [Tooltip("먹으면 회복하는 HP.")]
    [Min(0)] public int healAmount = 0;

    [Header("장비 (Weapon / Armor)")]
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public int maxHPBonus = 0;

    public bool IsEquipment => type == ItemType.Weapon || type == ItemType.Armor;

    /// <summary>창에 한 줄로 보여줄 효과 설명.</summary>
    public string Describe()
    {
        if (type == ItemType.Consumable) return $"Heal {healAmount} HP";

        string text = "";
        if (attackBonus != 0) text += $"ATK +{attackBonus}  ";
        if (defenseBonus != 0) text += $"DEF +{defenseBonus}  ";
        if (maxHPBonus != 0) text += $"HP +{maxHPBonus}";
        return text.Trim();
    }
}
