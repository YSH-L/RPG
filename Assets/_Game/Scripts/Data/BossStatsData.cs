using UnityEngine;

/// <summary>보스 정의. 일반 몬스터 정의에 원거리 공격을 더한다.</summary>
[CreateAssetMenu(menuName = "RPG/Boss Stats")]
public class BossStatsData : EnemyStatsData
{
    [Header("원거리 공격")]
    [Min(0)] public int projectileDamage = 40;
    [Min(0f)] public float projectileSpeed = 6f;
    [Tooltip("이 거리 안에 있으면 투사체를 쏜다.")]
    [Min(0f)] public float rangedRange = 9f;
    [Min(0.1f)] public float rangedCooldown = 3f;
    [Tooltip("투사체가 나가는 높이(발 기준).")]
    [Min(0f)] public float muzzleHeight = 1f;
}
