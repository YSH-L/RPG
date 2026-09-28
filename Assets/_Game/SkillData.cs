using UnityEngine;

public enum SkillType
{
    /// <summary>앞으로 돌진하며 지나간 자리의 적을 벤다.</summary>
    Dash,
    /// <summary>제자리에서 주변 원형 범위를 여러 번 벤다.</summary>
    Area,
    /// <summary>앞으로 날아가는 투사체를 쏜다.</summary>
    Projectile
}

/// <summary>
/// 스킬 하나의 정의. 스킬마다 에셋을 하나씩 만든다 (Skill_DashSlash.asset ...).
/// 쿨타임이 얼마나 남았는지 같은 상태는 PlayerSkills가 들고 있는다.
/// 피해량은 고정값이 아니라 플레이어 공격력 × damageMultiplier라서 레벨이 오르면 같이 세진다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    [Header("공통")]
    public string displayName = "Skill";
    public SkillType type = SkillType.Area;
    [Tooltip("이 레벨이 되면 쓸 수 있다.")]
    [Min(1)] public int unlockLevel = 1;
    [Min(0f)] public float cooldown = 3f;
    [Tooltip("플레이어 공격력에 곱하는 배율. 한 번 맞을 때의 피해량이다.")]
    [Min(0f)] public float damageMultiplier = 1.5f;
    [Tooltip("같은 적을 몇 번 때리는지. 투사체는 무시한다.")]
    [Min(1)] public int hitCount = 1;
    [Tooltip("여러 번 때릴 때 사이 간격(초).")]
    [Min(0f)] public float hitInterval = 0.15f;
    [Tooltip("스킬 시작 후 첫 판정(투사체는 발사)까지의 시간(초).")]
    [Min(0f)] public float castDelay = 0.1f;

    [Header("돌진 (Dash)")]
    public float dashDistance = 3.5f;
    [Min(0.01f)] public float dashDuration = 0.18f;
    [Tooltip("돌진 경로 판정의 높이. 발부터 위로 잰다.")]
    public float hitHeight = 1.2f;

    [Header("범위 (Area)")]
    public float radius = 1.8f;

    [Header("투사체 (Projectile)")]
    public float projectileSpeed = 9f;
    public float projectileRange = 7f;

    [Header("소리")]
    public AudioClip castSound;

    public int GetDamage(int attackPower) => Mathf.Max(1, Mathf.RoundToInt(attackPower * damageMultiplier));
}
