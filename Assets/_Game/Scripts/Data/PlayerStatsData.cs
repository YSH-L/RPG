using UnityEngine;

/// <summary>
/// 플레이어 성장 곡선과 조작감. 밸런스는 이 에셋의 숫자만 고친다.
/// 상태(현재 HP·레벨·골드)는 <see cref="PlayerStats"/>가 들고 있다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Player Stats")]
public class PlayerStatsData : ScriptableObject
{
    [Header("레벨")]
    [Min(1)] public int maxLevel = 30;
    [Tooltip("1 → 2레벨에 필요한 경험치.")]
    [Min(1)] public int expToLevel2 = 15;
    [Tooltip("레벨마다 필요 경험치가 몇 배씩 늘어나는지.")]
    [Min(1f)] public float expGrowth = 1.25f;

    [Header("능력치 (레벨 1 기준 + 레벨당 증가)")]
    [Min(1)] public int baseMaxHP = 100;
    [Min(0)] public int hpPerLevel = 20;
    [Min(1)] public int baseAttack = 10;
    [Min(0)] public int attackPerLevel = 3;
    [Min(0)] public int baseDefense = 0;
    [Min(0)] public int defensePerLevel = 1;

    [Header("시작 소지품")]
    [Min(0)] public int startGold = 50;
    [Tooltip("시작할 때 1번 슬롯 포션 개수.")]
    [Min(0)] public int startPotions = 3;

    [Header("이동")]
    [Min(0f)] public float moveSpeed = 4f;
    [Min(0f)] public float jumpVelocity = 9f;

    [Header("공격")]
    [Tooltip("공격 버튼을 누른 시점부터 다음 공격까지 최소 간격(초). 공격 모션이 이보다 길면 모션이 끝날 때까지 기다린다.")]
    [Min(0f)] public float attackCooldown = 0.5f;
    [Tooltip("버튼을 누른 뒤 판정이 들어가기까지(초). 칼이 앞으로 나가는 프레임에 맞춘다. Swordsman Atk1은 20fps의 4번째 프레임 = 0.2초.")]
    [Min(0f)] public float attackHitDelay = 0.2f;
    [Tooltip("캐릭터 발 기준 공격 판정 상자 중심. x는 바라보는 방향으로 뒤집힌다.")]
    public Vector2 attackBoxOffset = new Vector2(0.65f, 0.5f);
    public Vector2 attackBoxSize = new Vector2(1.4f, 1.1f);
    [Tooltip("한 번에 맞힐 수 있는 최대 몬스터 수.")]
    [Min(1)] public int maxTargets = 3;
    [Tooltip("데미지 편차. 0.1이면 공격력의 ±10%.")]
    [Range(0f, 0.5f)] public float damageVariance = 0.1f;

    [Header("스킬 — 범위베기 [A] (스킬북으로 배운다)")]
    [Tooltip("누른 시점부터 다시 쓸 수 있을 때까지(초).")]
    [Min(0f)] public float slashCooldown = 4f;
    [Tooltip("버튼을 누른 뒤 판정이 들어가기까지(초). Swordsman Atk2는 16fps의 5번째 프레임 = 0.25초.")]
    [Min(0f)] public float slashHitDelay = 0.25f;
    [Tooltip("기본 공격 데미지의 몇 배인지.")]
    [Min(0f)] public float slashDamageMultiplier = 1.8f;
    [Tooltip("캐릭터 발 기준 판정 상자 중심. x 0이면 앞뒤로 같이 벤다.")]
    public Vector2 slashBoxOffset = new Vector2(0f, 0.6f);
    public Vector2 slashBoxSize = new Vector2(4f, 1.6f);
    [Min(1)] public int slashMaxTargets = 10;

    [Header("스킬 — 방어 [S] (스킬북으로 배운다)")]
    [Tooltip("방어 자세를 유지하는 시간(초). 그동안 모든 데미지를 받지 않는다.")]
    [Min(0f)] public float guardDuration = 1f;
    [Tooltip("방어가 끝난 뒤 다시 쓸 수 있을 때까지(초).")]
    [Min(0f)] public float guardCooldown = 5f;

    [Header("피격")]
    [Tooltip("맞은 뒤 무적 시간(초).")]
    [Min(0f)] public float invincibleTime = 1f;
    public Vector2 knockback = new Vector2(3f, 4f);

    public int ExpToNext(int level)
    {
        return Mathf.Max(1, Mathf.RoundToInt(expToLevel2 * Mathf.Pow(expGrowth, level - 1)));
    }

    public int MaxHPAt(int level) => baseMaxHP + hpPerLevel * (level - 1);
    public int AttackAt(int level) => baseAttack + attackPerLevel * (level - 1);
    public int DefenseAt(int level) => baseDefense + defensePerLevel * (level - 1);
}
