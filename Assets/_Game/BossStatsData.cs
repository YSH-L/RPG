using UnityEngine;

/// <summary>
/// 보스 전용 기본값. EnemyStatsData를 확장해서 근접·원거리 패턴 관련 수치를 추가한다.
/// Fire Worm, Mecha-stone Golem처럼 보스마다 에셋을 하나씩 만든다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Boss Stats")]
public class BossStatsData : EnemyStatsData
{
    [Header("보스")]
    [Tooltip("처치 메시지 등에 쓰는 이름.")]
    public string displayName = "Boss";
    [Tooltip("켜면 이 보스를 잡았을 때 게임이 승리로 끝난다. 마지막 보스에만 켠다.")]
    public bool endsGame = false;

    [Header("근접 공격")]
    [Tooltip("이 거리 안이면 원거리 대신 근접 공격을 시도한다.")]
    public float meleeRange = 1.2f;
    public float meleeCooldown = 1.5f;
    [Tooltip("공격 애니메이션 시작 후 근접 판정이 들어가기까지의 시간(초).")]
    public float meleeHitDelay = 0.3f;

    [Header("원거리 공격 (투사체)")]
    public int projectileDamage = 15;
    public float projectileSpeed = 6f;
    public float rangedCooldown = 3f;
    public float rangedMinDistance = 1.5f;
    [Tooltip("공격 애니메이션 시작 후 투사체가 나가기까지의 시간(초).")]
    public float rangedFireDelay = 0.35f;
}
