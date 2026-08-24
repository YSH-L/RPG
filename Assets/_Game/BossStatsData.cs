using UnityEngine;

/// <summary>
/// 보스 전용 기본값. EnemyStatsData를 확장해서 원거리 패턴(파이어볼) 관련 수치를 추가한다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Boss Stats")]
public class BossStatsData : EnemyStatsData
{
    [Header("근접 공격")]
    [Tooltip("이 거리 안이면 원거리 대신 근접 공격을 시도한다.")]
    public float meleeRange = 1.2f;
    public float meleeCooldown = 1.5f;

    [Header("원거리 공격 (Fire Ball)")]
    public int projectileDamage = 15;
    public float projectileSpeed = 6f;
    public float rangedCooldown = 3f;
    public float rangedMinDistance = 1.5f;
}
