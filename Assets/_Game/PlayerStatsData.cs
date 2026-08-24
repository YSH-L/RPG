using UnityEngine;

/// <summary>
/// 플레이어 종류별 기본 스탯과 레벨 성장 곡선. 캐릭터마다(Swordsman, Archer) 에셋을 하나씩 만든다.
/// 상태(CurrentHP, CurrentLevel 등)는 PlayerController가 들고 있는다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Player Stats")]
public class PlayerStatsData : ScriptableObject
{
    [Header("레벨")]
    [Min(1)] public int maxLevel = 15;

    [Header("체력 (레벨당 선형 성장)")]
    public int baseMaxHP = 50;
    public int hpPerLevel = 10;

    [Header("공격력 (레벨당 선형 성장)")]
    public int baseAttackPower = 10;
    public int attackPerLevel = 2;

    [Header("방어력 — 받는 피해에서 그대로 차감된다 (레벨당 선형 성장)")]
    public int baseDefense = 0;
    public int defensePerLevel = 1;

    [Header("경험치 (레벨당 지수 성장)")]
    public int baseExpToLevel = 20;
    public float expGrowthRate = 1.15f;

    [Header("이동 — 레벨과 무관하게 고정")]
    public float moveSpeed = 4f;
    public float jumpForce = 9f;

    [Header("공격 판정")]
    public float attackRange = 0.9f;
    public float attackCooldown = 0.4f;
    [Tooltip("공격 애니메이션 시작 후 실제로 판정이 들어가기까지의 시간(초).")]
    public float attackHitDelay = 0.15f;

    [Header("피격 무적 시간")]
    public float invulnerabilityDuration = 0.8f;

    public int GetMaxHP(int level) => baseMaxHP + hpPerLevel * (level - 1);
    public int GetAttackPower(int level) => baseAttackPower + attackPerLevel * (level - 1);
    public int GetDefense(int level) => baseDefense + defensePerLevel * (level - 1);

    public int GetExpToNextLevel(int level)
    {
        float exp = baseExpToLevel * Mathf.Pow(expGrowthRate, level - 1);
        return Mathf.RoundToInt(exp);
    }
}
