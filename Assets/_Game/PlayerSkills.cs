using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 스킬 3칸 (Z / X / C). 어떤 스킬인지·수치는 SkillData 에셋이 정하고,
/// 여기서는 쿨타임 상태와 실행만 맡는다. 레벨이 unlockLevel에 닿아야 쓸 수 있다.
/// Player 오브젝트에 PlayerController와 같이 붙인다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerSkills : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public SkillData skill;
        [Tooltip("스킬 이펙트 풀. 비워도 된다.")]
        public ObjectPool effectPool;
        [Tooltip("Projectile 스킬의 투사체 풀.")]
        public ObjectPool projectilePool;
    }

    [SerializeField] private PlayerController player;
    [SerializeField] private SpriteAnimator animator;
    [SerializeField] private LayerMask enemyMask;
    [Tooltip("순서대로 Z, X, C 키.")]
    [SerializeField] private Slot[] slots = new Slot[3];

    private static readonly string[] KeyLabels = { "Z", "X", "C" };

    private Rigidbody2D rb;
    private float[] cooldowns;
    private int lastLevel;

    public int SlotCount => slots.Length;
    public SkillData GetSkill(int index) => slots[index] != null ? slots[index].skill : null;
    public float GetCooldownRemaining(int index) => cooldowns[index];
    public bool IsUnlocked(int index) => GetSkill(index) != null && player.CurrentLevel >= GetSkill(index).unlockLevel;
    public static string KeyLabel(int index) => index < KeyLabels.Length ? KeyLabels[index] : "?";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cooldowns = new float[slots.Length];
    }

    private void OnEnable()
    {
        lastLevel = player.CurrentLevel;
        player.OnLevelChanged += HandleLevelChanged;
    }

    private void OnDisable()
    {
        player.OnLevelChanged -= HandleLevelChanged;
    }

    private void HandleLevelChanged(int level)
    {
        // 저장을 불러오며 레벨이 바뀐 경우(Ready 상태)는 알리지 않는다.
        bool playing = GameManager.Instance != null && GameManager.Instance.IsPlaying;

        for (int i = 0; i < slots.Length; i++)
        {
            SkillData skill = GetSkill(i);
            if (playing && skill != null && lastLevel < skill.unlockLevel && skill.unlockLevel <= level)
            {
                StartCoroutine(ShowUnlockMessage(skill, i));
            }
        }
        lastLevel = level;
    }

    private IEnumerator ShowUnlockMessage(SkillData skill, int index)
    {
        // 같은 프레임에 뜨는 "Level Up!" 메시지를 덮어쓰지 않도록 조금 늦게 띄운다.
        yield return new WaitForSeconds(1.6f);
        ShowMessage($"New skill: {skill.displayName} [{KeyLabel(index)}]");
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || player.IsDead) return;

        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0f) cooldowns[i] = Mathf.Max(0f, cooldowns[i] - Time.deltaTime);
        }

        // 돌진·회전 베기 중에는 다른 스킬을 겹쳐 쓰지 않는다.
        if (player.ControlLocked) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.zKey.wasPressedThisFrame) TryCast(0);
        else if (kb.xKey.wasPressedThisFrame) TryCast(1);
        else if (kb.cKey.wasPressedThisFrame) TryCast(2);
    }

    private void TryCast(int index)
    {
        if (index >= slots.Length) return;
        SkillData skill = GetSkill(index);
        if (skill == null || cooldowns[index] > 0f) return;

        if (!IsUnlocked(index))
        {
            ShowMessage($"{skill.displayName} unlocks at Lv.{skill.unlockLevel}");
            return;
        }

        cooldowns[index] = skill.cooldown;
        Slot slot = slots[index];

        switch (skill.type)
        {
            case SkillType.Dash: StartCoroutine(DashRoutine(slot)); break;
            case SkillType.Area: StartCoroutine(AreaRoutine(slot)); break;
            case SkillType.Projectile: StartCoroutine(ProjectileRoutine(slot)); break;
        }
    }

    // ── 스킬별 동작 ────────────────────────────────────────────────

    private IEnumerator DashRoutine(Slot slot)
    {
        SkillData skill = slot.skill;
        float dir = player.FacingSign;
        float speed = skill.dashDistance / skill.dashDuration;

        player.ControlLocked = true;
        player.GrantInvulnerability(skill.dashDuration + 0.2f);
        animator.Play(AnimState.Attack);

        // 벽에 막히면 거기서 멈추도록 Rigidbody로 민다. 실제로 지나간 구간만 판정한다.
        Vector2 start = rb.position;
        float elapsed = 0f;
        while (elapsed < skill.dashDuration && !player.IsDead)
        {
            rb.linearVelocity = new Vector2(dir * speed, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        Vector2 end = rb.position;

        SpawnEffect(slot, end + new Vector2(0f, skill.hitHeight * 0.5f), dir);

        Vector2 center = (start + end) * 0.5f + new Vector2(0f, skill.hitHeight * 0.5f);
        Vector2 size = new Vector2(Mathf.Abs(end.x - start.x) + 1f, skill.hitHeight);
        for (int i = 0; i < skill.hitCount && !player.IsDead; i++)
        {
            DamageAll(Physics2D.OverlapBoxAll(center, size, 0f, enemyMask), skill);
            if (i < skill.hitCount - 1) yield return new WaitForSeconds(skill.hitInterval);
        }

        player.ControlLocked = false;
    }

    private IEnumerator AreaRoutine(Slot slot)
    {
        SkillData skill = slot.skill;

        player.ControlLocked = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.Play(AnimState.Attack);

        Vector2 center = rb.position + new Vector2(0f, 0.5f);
        SpawnEffect(slot, center, player.FacingSign);

        yield return new WaitForSeconds(skill.castDelay);
        for (int i = 0; i < skill.hitCount && !player.IsDead; i++)
        {
            center = rb.position + new Vector2(0f, 0.5f);
            DamageAll(Physics2D.OverlapCircleAll(center, skill.radius, enemyMask), skill);
            if (i < skill.hitCount - 1) yield return new WaitForSeconds(skill.hitInterval);
        }

        player.ControlLocked = false;
    }

    private IEnumerator ProjectileRoutine(Slot slot)
    {
        SkillData skill = slot.skill;
        float dir = player.FacingSign;
        animator.Play(AnimState.Attack);

        yield return new WaitForSeconds(skill.castDelay);
        if (player.IsDead || slot.projectilePool == null) yield break;

        Vector3 spawnPos = transform.position + new Vector3(dir * 0.6f, 0.55f, 0f);
        GameObject go = slot.projectilePool.Get(spawnPos, Quaternion.identity);
        if (go != null && go.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Launch(new Vector2(dir, 0f), skill.GetDamage(player.AttackPower),
                skill.projectileSpeed, slot.projectilePool, skill.projectileRange);
        }
    }

    // ── 공용 ──────────────────────────────────────────────────────

    /// <summary>한 번의 판정에서 같은 적은 한 번만 때린다 (콜라이더가 여러 개여도).</summary>
    private void DamageAll(Collider2D[] hits, SkillData skill)
    {
        int damage = skill.GetDamage(player.AttackPower);
        var done = new HashSet<GameObject>();

        foreach (Collider2D hit in hits)
        {
            if (!done.Add(hit.gameObject)) continue;
            if (hit.TryGetComponent<IDamageable>(out var target)) target.TakeDamage(damage);
        }
    }

    private static void SpawnEffect(Slot slot, Vector2 position, float facing)
    {
        if (slot.effectPool == null) return;

        GameObject fx = slot.effectPool.Get(position, Quaternion.identity);
        if (fx != null && fx.TryGetComponent<SkillEffect>(out var effect)) effect.Init(slot.effectPool, facing);
    }

    // UI 폰트(LiberationSans)에 한글 글리프가 없어서 화면 문구는 영어로 쓴다.
    private static void ShowMessage(string text)
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(text);
    }
}
