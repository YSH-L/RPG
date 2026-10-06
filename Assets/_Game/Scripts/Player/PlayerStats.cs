using System;
using System.Collections.Generic;
using UnityEngine;

public enum BuyResult
{
    Bought,
    NotEnoughGold,
    LevelTooLow,
    AlreadyBetter,
    AlreadyLearned,
    /// <summary>이미 산 원소 책을 다시 골라 그 원소로 바꿔 꼈다. 돈은 들지 않는다.</summary>
    Switched
}

/// <summary>
/// 플레이어의 지금 상태: 레벨·경험치·체력·골드·장비·포션.
/// 기본값과 성장 곡선은 <see cref="PlayerStatsData"/> 에셋에 있다.
/// </summary>
/// <remarks>
/// 몬스터를 잡은 보상은 몬스터를 참조하지 않고 <see cref="CombatEvents.OnDied"/>를 구독해서 받는다.
/// </remarks>
public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] private PlayerStatsData data;

    [Tooltip("1·2번 키로 쓰는 포션. 상점에서 산 포션은 같은 아이템 슬롯에 쌓인다.")]
    [SerializeField] private ItemData[] potionSlots = new ItemData[2];

    public PlayerStatsData Data => data;
    public int Level { get; private set; } = 1;
    public int Exp { get; private set; }
    public int ExpToNext => data.ExpToNext(Level);
    public bool IsMaxLevel => Level >= data.maxLevel;
    public int HP { get; private set; }
    public int MaxHP => data.MaxHPAt(Level) + (Armor != null ? Armor.hpBonus : 0);
    public int Attack => data.AttackAt(Level) + (Weapon != null ? Weapon.attackBonus : 0);
    public int Defense => data.DefenseAt(Level) + (Armor != null ? Armor.defenseBonus : 0);
    public int Gold { get; private set; }
    public ItemData Weapon { get; private set; }
    public ItemData Armor { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvincible => Time.time < invincibleUntil;
    /// <summary>방어 스킬 중. 이 동안은 어떤 데미지도 받지 않는다.</summary>
    public bool IsGuarding => Time.time < guardUntil;
    public int PotionSlotCount => potionSlots.Length;
    /// <summary>지금 무기에 붙은 원소. 원소 책을 사기 전에는 null.</summary>
    public ElementData Element { get; private set; }

    /// <summary>화면에 보이는 값이 하나라도 바뀌었을 때.</summary>
    public event Action OnChanged;
    public event Action<int> OnLevelUp;
    /// <summary>실제로 피해를 입었을 때 (깎인 양).</summary>
    public event Action<int> OnHurt;
    public event Action OnDeath;

    private int[] potionCounts;
    private float invincibleUntil;
    private float guardUntil;
    private readonly HashSet<SkillType> learned = new HashSet<SkillType>();
    private readonly HashSet<ElementData> ownedElements = new HashSet<ElementData>();

    public bool HasSkill(SkillType skill) => learned.Contains(skill);
    public bool OwnsElement(ElementData element) => element != null && ownedElements.Contains(element);

    /// <summary>체력을 채운다. 최대 체력을 넘지 않는다.</summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0 || HP >= MaxHP) return;
        HP = Mathf.Min(MaxHP, HP + amount);
        OnChanged?.Invoke();
    }

    /// <summary>지금부터 duration초 동안 모든 데미지를 막는다.</summary>
    public void Guard(float duration)
    {
        guardUntil = Time.time + duration;
    }

    private void Awake()
    {
        ResetToStart();
    }

    /// <summary>
    /// 캐릭터를 바꾼다. 수치 에셋을 갈아끼우고 처음 상태로 되돌린다.
    /// 시작 화면(Ready)에서 <see cref="CharacterSelect"/>가 부른다.
    /// </summary>
    public void SetData(PlayerStatsData newData)
    {
        if (newData == null) return;
        data = newData;
        ResetToStart();
        OnChanged?.Invoke();
    }

    private void ResetToStart()
    {
        potionCounts = new int[potionSlots.Length];
        if (potionCounts.Length > 0) potionCounts[0] = data.startPotions;
        Gold = data.startGold;
        HP = MaxHP;
    }

    private void OnEnable() { CombatEvents.OnDied += HandleDied; }
    private void OnDisable() { CombatEvents.OnDied -= HandleDied; }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0 || IsInvincible || IsGuarding) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        int damage = Mathf.Max(1, amount - Defense);
        HP = Mathf.Max(0, HP - damage);
        invincibleUntil = Time.time + data.invincibleTime;

        CombatEvents.RaiseDamaged(gameObject, damage);
        OnHurt?.Invoke(damage);

        if (HP <= 0)
        {
            IsDead = true;
            CombatEvents.RaiseDied(gameObject);
            OnDeath?.Invoke();
        }

        OnChanged?.Invoke();
    }

    /// <summary>한 번 휘두를 때의 데미지. 공격력에 편차를 준다.</summary>
    public int RollDamage()
    {
        float variance = data.damageVariance;
        return Mathf.Max(1, Mathf.RoundToInt(Attack * UnityEngine.Random.Range(1f - variance, 1f + variance)));
    }

    public void GainExp(int amount)
    {
        if (amount <= 0 || IsDead) return;

        Exp += amount;
        while (!IsMaxLevel && Exp >= ExpToNext)
        {
            Exp -= ExpToNext;
            Level++;
            HP = MaxHP;
            OnLevelUp?.Invoke(Level);
        }
        if (IsMaxLevel) Exp = 0;

        OnChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        OnChanged?.Invoke();
    }

    public ItemData GetPotion(int slot) => slot >= 0 && slot < potionSlots.Length ? potionSlots[slot] : null;
    public int GetPotionCount(int slot) => slot >= 0 && slot < potionCounts.Length ? potionCounts[slot] : 0;

    /// <summary>포션을 마신다. 체력이 가득 차 있거나 포션이 없으면 false.</summary>
    public bool UsePotion(int slot)
    {
        ItemData potion = GetPotion(slot);
        if (IsDead || potion == null || potionCounts[slot] <= 0 || HP >= MaxHP) return false;

        potionCounts[slot]--;
        int heal = potion.healAmount > 0 ? potion.healAmount : MaxHP;
        HP = Mathf.Min(MaxHP, HP + heal);
        OnChanged?.Invoke();
        return true;
    }

    public BuyResult Buy(ItemData item)
    {
        if (Level < item.requiredLevel) return BuyResult.LevelTooLow;
        if (item.kind == ItemKind.Weapon && Weapon != null && Weapon.attackBonus >= item.attackBonus) return BuyResult.AlreadyBetter;
        if (item.kind == ItemKind.Armor && Armor != null && Armor.defenseBonus >= item.defenseBonus) return BuyResult.AlreadyBetter;
        if (item.kind == ItemKind.SkillBook && item.element != null && ownedElements.Contains(item.element))
        {
            if (Element == item.element) return BuyResult.AlreadyLearned;
            Element = item.element;   // 이미 산 원소 책은 공짜로 바꿔 낀다
            OnChanged?.Invoke();
            return BuyResult.Switched;
        }
        if (item.kind == ItemKind.SkillBook && item.element == null && learned.Contains(item.skill)) return BuyResult.AlreadyLearned;
        if (Gold < item.price) return BuyResult.NotEnoughGold;

        Gold -= item.price;
        switch (item.kind)
        {
            case ItemKind.Weapon:
                Weapon = item;
                break;
            case ItemKind.Armor:
                int before = MaxHP;
                Armor = item;
                HP = Mathf.Min(MaxHP, HP + Mathf.Max(0, MaxHP - before));
                break;
            case ItemKind.Potion:
                int slot = Array.IndexOf(potionSlots, item);
                if (slot >= 0) potionCounts[slot]++;
                break;
            case ItemKind.SkillBook:
                learned.Add(item.skill);
                if (item.element != null)
                {
                    ownedElements.Add(item.element);
                    Element = item.element;
                }
                break;
        }

        OnChanged?.Invoke();
        return BuyResult.Bought;
    }

    private void HandleDied(GameObject target)
    {
        if (target == null || !target.TryGetComponent(out Enemy enemy)) return;

        EnemyStatsData reward = enemy.Data;
        GainExp(reward.expReward);
        AddGold(UnityEngine.Random.Range(reward.goldMin, reward.goldMax + 1));
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(reward.expReward);
    }
}
