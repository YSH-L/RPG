using UnityEngine;

public enum ItemKind
{
    Weapon,
    Armor,
    Potion
}

/// <summary>상점에서 파는 물건 하나. 무기·방어구는 사는 즉시 장착되고, 포션은 쌓인다.</summary>
[CreateAssetMenu(menuName = "RPG/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "Item";
    public Sprite icon;
    public ItemKind kind;
    [Min(0)] public int price = 10;
    [Min(1)] public int requiredLevel = 1;

    [Header("장비")]
    [Min(0)] public int attackBonus;
    [Min(0)] public int defenseBonus;
    [Min(0)] public int hpBonus;

    [Header("포션")]
    [Tooltip("회복량. 0이면 최대 체력까지 전부 회복한다.")]
    [Min(0)] public int healAmount = 50;

    public string Describe()
    {
        switch (kind)
        {
            case ItemKind.Weapon: return $"ATK +{attackBonus}";
            case ItemKind.Armor: return hpBonus > 0 ? $"DEF +{defenseBonus}  HP +{hpBonus}" : $"DEF +{defenseBonus}";
            default: return healAmount > 0 ? $"Heal {healAmount} HP" : "Full heal";
        }
    }
}
