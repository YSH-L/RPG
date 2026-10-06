using UnityEngine;

/// <summary>
/// 무기에 부여하는 원소 하나. 원소 책을 사면 이 원소가 범위베기 [A]에 붙는다.
/// 겉모습(칼·베기 이펙트)과 능력 수치를 함께 들고 있다. 능력은 쓰지 않는 칸을 기본값으로 두면 꺼진다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Element")]
public class ElementData : ScriptableObject
{
    public string displayName = "Element";
    [Tooltip("HUD·상점 글자 색.")]
    public Color color = Color.white;

    [Header("겉모습")]
    [Tooltip("범위베기 때 플레이어 주위를 한 바퀴 도는 원소 칼 (Elemental Weapons Effect).")]
    public Sprite blade;
    [Tooltip("범위베기 때 앞뒤로 터지는 베기 이펙트. SpriteAnimator의 Attack 묶음에 프레임이 들어 있는 프리팹.")]
    public GameObject slashEffect;

    [Header("능력 — 범위베기에만 적용")]
    [Tooltip("데미지 배율. 1이면 그대로.")]
    [Min(0f)] public float damageMultiplier = 1f;
    [Tooltip("범위베기 판정 상자 배율. 1이면 그대로.")]
    [Min(0.1f)] public float areaMultiplier = 1f;
    [Tooltip("준 데미지 중 체력으로 돌려받는 비율. 0.15면 15%.")]
    [Range(0f, 1f)] public float lifesteal;

    [Tooltip("지속 피해(화상 등): 맞은 데미지의 이 비율만큼 burnInterval마다 다시 들어간다. 0이면 없음.")]
    [Range(0f, 1f)] public float burnRatio;
    [Min(0f)] public float burnDuration = 3f;
    [Min(0.1f)] public float burnInterval = 1f;

    [Tooltip("둔화: 이동속도 배율. 1이면 둔화 없음.")]
    [Range(0f, 1f)] public float slowFactor = 1f;
    [Min(0f)] public float slowDuration = 2f;

    [Tooltip("속박: 이 시간(초) 동안 움직이지도 공격하지도 못한다. 0이면 없음.")]
    [Min(0f)] public float rootDuration;

    /// <summary>상점에 보여줄 한 줄 설명. 능력이 여러 개면 모두 적는다.</summary>
    public string Describe()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (areaMultiplier > 1f) parts.Add($"Area x{areaMultiplier:0.#}");
        if (damageMultiplier > 1f) parts.Add($"Damage x{damageMultiplier:0.#}");
        if (burnRatio > 0f) parts.Add($"DoT {burnRatio * 100f:0}%/{burnInterval:0.#}s {burnDuration:0.#}s");
        if (slowFactor < 1f) parts.Add($"Slow {(1f - slowFactor) * 100f:0}% {slowDuration:0.#}s");
        if (lifesteal > 0f) parts.Add($"Lifesteal {lifesteal * 100f:0}%");
        if (rootDuration > 0f) parts.Add($"Root {rootDuration:0.0}s");
        return string.Join(", ", parts);
    }
}
