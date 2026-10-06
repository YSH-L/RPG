using UnityEngine;

public enum ItemKind
{
    Weapon,
    Armor,
    Potion,
    SkillBook
}

/// <summary>스킬북으로 배우는 스킬. 수치는 <see cref="PlayerStatsData"/>에 있다.</summary>
public enum SkillType
{
    Slash,
    Guard
}

/// <summary>상점에서 파는 물건 하나. 무기·방어구는 사는 즉시 장착되고, 포션은 쌓이고, 스킬북은 사는 즉시 스킬을 배운다.</summary>
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

    [Header("스킬북")]
    public SkillType skill;

    public string Describe()
    {
        switch (kind)
        {
            case ItemKind.Weapon: return $"ATK +{attackBonus}";
            case ItemKind.Armor: return hpBonus > 0 ? $"DEF +{defenseBonus}  HP +{hpBonus}" : $"DEF +{defenseBonus}";
            case ItemKind.SkillBook: return $"Learn [{SkillKey(skill)}] {skill}";
            default: return healAmount > 0 ? $"Heal {healAmount} HP" : "Full heal";
        }
    }

    /// <summary>스킬을 쓰는 키. 입력은 <see cref="PlayerController"/>가 읽는다.</summary>
    public static string SkillKey(SkillType skill) => skill == SkillType.Slash ? "A" : "S";
}
