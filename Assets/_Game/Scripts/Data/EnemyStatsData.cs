using UnityEngine;

/// <summary>
/// 몬스터 종류 하나의 정의. Slime.asset, Goblin.asset처럼 종류마다 에셋 하나.
/// 지금 남은 체력은 <see cref="Enemy"/>가 인스턴스마다 따로 들고 있다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Enemy Stats")]
public class EnemyStatsData : ScriptableObject
{
    public string displayName = "Monster";

    [Header("전투")]
    [Min(1)] public int maxHP = 30;
    [Tooltip("몸에 닿았을 때 주는 피해.")]
    [Min(0)] public int contactDamage = 8;
    [Tooltip("공격 동작이 끝났을 때 주는 피해.")]
    [Min(0)] public int attackDamage = 10;
    [Tooltip("이 거리 안에 들어오면 공격 동작을 시작한다.")]
    [Min(0f)] public float attackRange = 0.9f;
    [Min(0f)] public float attackCooldown = 1.5f;
    [Tooltip("켜면 맞아도 경직되지 않는다. 보스용.")]
    public bool superArmor;

    [Header("이동")]
    [Min(0f)] public float moveSpeed = 1.2f;
    [Tooltip("이 거리 안에 플레이어가 있으면 쫓아온다.")]
    [Min(0f)] public float detectRange = 5f;
    [Tooltip("스폰 지점에서 이 거리까지만 배회한다.")]
    [Min(0f)] public float wanderRange = 6f;

    [Header("비행")]
    public bool flying;
    [Tooltip("비행 몬스터가 떠 있는 높이(지면 기준).")]
    [Min(0f)] public float hoverHeight = 1.2f;

    [Header("보상")]
    [Min(0)] public int expReward = 5;
    [Min(0)] public int goldMin = 3;
    [Min(0)] public int goldMax = 6;
}
