using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 조작: ←→ 이동, Space 점프, Z 공격, A 범위베기(Archer: 화살 난사), S 방어(Archer: 회피), ↑ 포탈·상인, 1·2 포션.
/// A·S는 상점에서 스킬북을 사야 쓸 수 있다.
/// 수치는 전부 <see cref="PlayerStatsData"/>에서 읽는다. 캐릭터는 시작 화면에서 <see cref="SetCharacter"/>로 바뀐다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    /// <summary>몬스터가 쫓아갈 대상을 찾을 때 쓴다.</summary>
    public static PlayerController Instance { get; private set; }

    [SerializeField] private SpriteAnimator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private LoopBody loopBody;

    [Header("판정")]
    [SerializeField] private LayerMask groundMask = 1 << 8;
    [SerializeField] private LayerMask enemyMask = 1 << 10;
    [Tooltip("발밑 판정 상자 크기.")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.12f);
    [Tooltip("↑를 눌렀을 때 포탈·상인을 찾는 범위(몸 중심 기준).")]
    [SerializeField] private Vector2 interactSize = new Vector2(1.2f, 1.6f);

    [Header("효과")]
    [Tooltip("몬스터를 때렸을 때 나오는 이펙트 풀. 비워 두면 생략.")]
    [SerializeField] private ObjectPool hitEffectPool;
    [Tooltip("범위베기의 원소 칼·베기 이펙트. 비워 두면 생략.")]
    [SerializeField] private SlashVisual slashVisual;
    [Tooltip("Archer의 화살 풀. 원거리 캐릭터만 쓴다.")]
    [SerializeField] private ObjectPool arrowPool;

    public PlayerStats Stats { get; private set; }
    public Area CurrentArea => loopBody != null ? loopBody.Area : null;

    private Rigidbody2D body;
    private PlayerStatsData data;
    private float facing = 1f;
    private float nextAttackTime;
    private float nextSlashTime;
    private float nextGuardTime;
    private float knockbackUntil;
    private int ignoreInputFrame = -1;
    private bool dying;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    private void Awake()
    {
        Instance = this;
        body = GetComponent<Rigidbody2D>();
        Stats = GetComponent<PlayerStats>();
        data = Stats.Data;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
        Stats.OnHurt += HandleHurt;
        Stats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
        Stats.OnHurt -= HandleHurt;
        Stats.OnDeath -= HandleDeath;
    }

    /// <summary>
    /// Ready에서 Space로 시작하는 그 프레임에 점프까지 같이 되지 않도록,
    /// 상태가 바뀐 프레임의 입력은 버린다.
    /// </summary>
    private void HandleStateChanged(GameState state)
    {
        ignoreInputFrame = Time.frameCount;
    }

    /// <summary>
    /// 캐릭터를 바꾼다. 수치 에셋과 애니메이터를 갈아끼운다. 시작 화면(Ready)에서만 부른다.
    /// 애니메이터는 캐릭터마다 하나씩 플레이어에 붙어 있고, 쓰는 것만 켠다 — 둘 다 켜지면 서로 스프라이트를 덮어쓴다.
    /// </summary>
    public void SetCharacter(PlayerStatsData newData, SpriteAnimator newAnimator)
    {
        if (newAnimator != null && newAnimator != animator)
        {
            if (animator != null) animator.enabled = false;
            animator = newAnimator;
            animator.enabled = true;
            animator.Play(AnimState.Idle, force: true);
            animator.SetFacing(facing);
        }
        Stats.SetData(newData);
        data = Stats.Data;
    }

    /// <summary>HUD에 보일 스킬 이름. Archer는 같은 책으로 다른 기술을 쓴다.</summary>
    public string SkillName(SkillType skill)
    {
        if (data.ranged && skill == SkillType.Slash) return "Volley";
        if (data.guardDashSpeed > 0f && skill == SkillType.Guard) return "Dodge";
        return skill.ToString();
    }

    /// <summary>다른 공간으로 순간이동. <see cref="AreaManager"/>가 부른다.</summary>
    public void TeleportTo(Area area, Vector2 position)
    {
        loopBody.Area = area;
        body.position = position;
        transform.position = position;
        body.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (dying) return;

        UpdateBlink();

        Keyboard keyboard = Keyboard.current;
        bool canControl = GameManager.Instance != null && GameManager.Instance.IsPlaying
                          && keyboard != null && !InputLock.Locked && Time.frameCount != ignoreInputFrame;

        bool grounded = IsGrounded();
        float move = 0f;

        if (canControl)
        {
            move = (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f);

            if (keyboard.zKey.wasPressedThisFrame && Time.time >= nextAttackTime) StartAttack();
            if (keyboard.aKey.wasPressedThisFrame) TrySlash();
            if (keyboard.sKey.wasPressedThisFrame) TryGuard();
            if (keyboard.spaceKey.wasPressedThisFrame && grounded && Time.time >= knockbackUntil && !Stats.IsGuarding)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, data.jumpVelocity);
                grounded = false;
            }
            if (keyboard.upArrowKey.wasPressedThisFrame) TryInteract();
            if (keyboard.digit1Key.wasPressedThisFrame) Stats.UsePotion(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Stats.UsePotion(1);
        }

        bool acting = IsActing();
        bool guarding = Stats.IsGuarding;

        if (Time.time >= knockbackUntil)
        {
            // 땅에서 공격하는 동안과 방어하는 동안은 제자리에 선다. 공중 공격은 관성을 유지한다.
            // 회피(guardDashSpeed > 0)는 바라보는 반대쪽으로 물러난다.
            float speed = (acting && grounded) || guarding ? 0f : move * data.moveSpeed;
            if (guarding && data.guardDashSpeed > 0f) speed = -facing * data.guardDashSpeed;
            body.linearVelocity = new Vector2(speed, body.linearVelocity.y);

            if (!acting && move != 0f)
            {
                facing = Mathf.Sign(move);
                animator.SetFacing(move);
            }
        }

        // Block은 반복 동작이라 방어 시간 동안 여기서 계속 틀어 준다. 끝나면 아래 줄이 Idle로 되돌린다.
        if (guarding) animator.Play(AnimState.Block);
        else if (!grounded) animator.Play(AnimState.Jump);
        else animator.Play(move != 0f && !acting ? AnimState.Walk : AnimState.Idle);
    }

    /// <summary>스킬을 다시 쓸 수 있을 때까지 남은 시간(초). HUD가 읽는다.</summary>
    public float SkillCooldownLeft(SkillType skill)
    {
        float readyAt = skill == SkillType.Slash ? nextSlashTime : nextGuardTime;
        return Mathf.Max(0f, readyAt - Time.time);
    }

    private bool IsGrounded()
    {
        if (body.linearVelocity.y > 0.05f) return false;
        Vector2 feet = (Vector2)transform.position + Vector2.down * 0.02f;
        return Physics2D.OverlapBox(feet, groundCheckSize, 0f, groundMask) != null;
    }

    private void StartAttack()
    {
        if (!TryPlayStrike(AnimState.Attack)) return;

        // 누른 시점부터 잰다. 모션이 쿨타임보다 길면 IsActing 검사로 모션이 끝날 때까지 막힌다.
        nextAttackTime = Time.time + data.attackCooldown;
        if (data.ranged)
        {
            StartCoroutine(Shoot(AnimState.Attack, data.attackHitDelay, 1, 1f, facing, null));
            return;
        }
        StartCoroutine(Strike(AnimState.Attack, data.attackHitDelay, data.attackBoxOffset, data.attackBoxSize,
            data.maxTargets, 1f, facing, null));
    }

    /// <summary>범위베기. 무기에 붙은 원소가 있으면 범위·데미지·상태이상과 연출이 그 원소를 따른다.</summary>
    private void TrySlash()
    {
        if (!Stats.HasSkill(SkillType.Slash) || Time.time < nextSlashTime) return;
        if (!TryPlayStrike(AnimState.Skill)) return;

        ElementData element = Stats.Element;
        float areaScale = element != null ? element.areaMultiplier : 1f;
        float damage = data.slashDamageMultiplier * (element != null ? element.damageMultiplier : 1f);

        nextSlashTime = Time.time + data.slashCooldown;
        if (data.ranged)
        {
            // Archer: 화살 난사. 원소의 범위 배율은 화살이 날아가는 거리에 곱해진다.
            StartCoroutine(Shoot(AnimState.Skill, data.slashHitDelay, data.volleyCount, damage, facing, element));
            return;
        }
        if (slashVisual != null) slashVisual.PlayBlade(element, facing);
        StartCoroutine(Strike(AnimState.Skill, data.slashHitDelay, data.slashBoxOffset, data.slashBoxSize * areaScale,
            data.slashMaxTargets, damage, facing, element));
    }

    private void TryGuard()
    {
        if (!Stats.HasSkill(SkillType.Guard) || Time.time < nextGuardTime || IsActing()) return;

        Stats.Guard(data.guardDuration);
        nextGuardTime = Time.time + data.guardDuration + data.guardCooldown;
        animator.Play(AnimState.Block);
    }

    /// <summary>
    /// 공격 모션을 튼다. Hit 모션 중이면 거절되는데, 모션이 안 나왔으면 판정도 넣지 않도록 false를 돌려준다.
    /// </summary>
    private bool TryPlayStrike(AnimState motion)
    {
        if (IsActing()) return false;
        animator.Play(motion);
        return IsPlaying(motion);
    }

    private bool IsPlaying(AnimState motion) => animator.Current == motion && animator.IsBusy;

    /// <summary>공격·범위베기 모션 중이거나 방어 중. 이 동안은 다른 공격·스킬을 시작하지 않는다.</summary>
    private bool IsActing() => IsPlaying(AnimState.Attack) || IsPlaying(AnimState.Skill) || Stats.IsGuarding;

    private IEnumerator Strike(AnimState motion, float delay, Vector2 boxOffset, Vector2 boxSize,
        int maxTargets, float damageMultiplier, float direction, ElementData element)
    {
        yield return new WaitForSeconds(delay);
        if (dying || CurrentArea == null) yield break;
        if (!IsPlaying(motion)) yield break;   // 판정 전에 맞아서 모션이 끊겼으면 데미지도 없다

        if (element != null && slashVisual != null) slashVisual.PlayEffect(element, direction);

        Vector2 offset = new Vector2(boxOffset.x * direction, boxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;
        CurrentArea.OverlapBox(center, boxSize, enemyMask, hits);

        // 가까운 순서로 maxTargets 마리까지.
        hits.Sort((a, b) =>
            Mathf.Abs(CurrentArea.DeltaX(transform.position.x, a.transform.position.x))
                .CompareTo(Mathf.Abs(CurrentArea.DeltaX(transform.position.x, b.transform.position.x))));

        int struck = 0;
        int totalDealt = 0;
        foreach (Collider2D hit in hits)
        {
            if (struck >= maxTargets) break;
            if (!hit.TryGetComponent(out IDamageable target)) continue;
            bool isEnemy = hit.TryGetComponent(out Enemy enemy);
            if (isEnemy && enemy.IsDead) continue;

            int dealt = Mathf.Max(1, Mathf.RoundToInt(Stats.RollDamage() * damageMultiplier));
            target.TakeDamage(dealt);
            if (isEnemy && element != null) enemy.ApplyElement(element, dealt);
            totalDealt += dealt;
            struck++;

            if (hitEffectPool != null)
            {
                GameObject effect = hitEffectPool.Get(hit.bounds.center, Quaternion.identity);
                if (effect != null && effect.TryGetComponent(out OneShotEffect oneShot)) oneShot.Play(hitEffectPool, CurrentArea);
            }
        }

        if (element != null && element.lifesteal > 0f) Stats.Heal(Mathf.RoundToInt(totalDealt * element.lifesteal));
    }

    /// <summary>
    /// Archer의 화살. 모션이 시위를 놓는 순간(delay 뒤)에 count발을 위아래로 고르게 나눠 쏜다.
    /// 데미지는 쏘는 순간 정하고, 맞은 뒤의 원소 효과는 <see cref="OnArrowHit"/>가 건다.
    /// </summary>
    private IEnumerator Shoot(AnimState motion, float delay, int count, float damageMultiplier, float direction, ElementData element)
    {
        yield return new WaitForSeconds(delay);
        if (dying || CurrentArea == null || arrowPool == null) yield break;
        if (!IsPlaying(motion)) yield break;   // 시위를 놓기 전에 맞아서 모션이 끊겼으면 쏘지 않는다

        float range = data.arrowRange * (element != null ? element.areaMultiplier : 1f);
        Color tint = element != null ? Color.Lerp(Color.white, element.color, 0.7f) : Color.white;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float height = Mathf.Lerp(data.arrowHeightMin, data.arrowHeightMax, t);
            float angle = (t - 0.5f) * data.volleySpread;   // 아래쪽 화살은 살짝 아래로, 위쪽은 살짝 위로
            Vector2 aim = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            aim.x *= direction;

            Vector3 muzzle = transform.position + new Vector3(0.35f * direction, height, 0f);
            GameObject go = arrowPool.Get(muzzle, Quaternion.identity);
            if (go == null || !go.TryGetComponent(out PlayerArrow arrow)) continue;

            int dealt = Mathf.Max(1, Mathf.RoundToInt(Stats.RollDamage() * damageMultiplier));
            arrow.Launch(arrowPool, CurrentArea, aim, data.arrowSpeed, range / data.arrowSpeed, dealt, enemyMask, this, element, tint);
        }
    }

    /// <summary>화살이 몬스터를 맞혔을 때 <see cref="PlayerArrow"/>가 부른다.</summary>
    public void OnArrowHit(Collider2D hit, int dealt, ElementData element)
    {
        if (hitEffectPool != null)
        {
            GameObject effect = hitEffectPool.Get(hit.bounds.center, Quaternion.identity);
            if (effect != null && effect.TryGetComponent(out OneShotEffect oneShot)) oneShot.Play(hitEffectPool, CurrentArea);
        }
        if (element == null) return;

        if (hit.TryGetComponent(out Enemy enemy)) enemy.ApplyElement(element, dealt);
        if (element.lifesteal > 0f) Stats.Heal(Mathf.RoundToInt(dealt * element.lifesteal));
        if (slashVisual != null) slashVisual.PlayEffectAt(element, hit.bounds.center);
    }

    private void TryInteract()
    {
        Vector2 center = (Vector2)transform.position + Vector2.up * (interactSize.y * 0.5f);
        Collider2D[] found = Physics2D.OverlapBoxAll(center, interactSize, 0f);
        foreach (Collider2D candidate in found)
        {
            if (candidate.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact(this);
                return;
            }
        }
    }

    private void HandleHurt(int amount)
    {
        if (Stats.IsDead) return;

        knockbackUntil = Time.time + 0.25f;
        body.linearVelocity = new Vector2(-facing * data.knockback.x, data.knockback.y);
        animator.Play(AnimState.Hit);
    }

    /// <summary>맞은 뒤 무적 시간 동안 깜빡인다. 회피하는 동안은 반투명.</summary>
    private void UpdateBlink()
    {
        Color color = spriteRenderer.color;
        bool dodging = Stats.IsGuarding && data.guardDashSpeed > 0f;
        color.a = dodging ? 0.5f : Stats.IsInvincible && Mathf.FloorToInt(Time.time * 16f) % 2 == 0 ? 0.35f : 1f;
        spriteRenderer.color = color;
    }

    private void HandleDeath()
    {
        StartCoroutine(DeathRoutine());
    }

    /// <summary>Swordsman에는 death 프레임이 없다. Hit에서 멈춘 채 뒤로 쓰러지며 흐려진다.</summary>
    private IEnumerator DeathRoutine()
    {
        dying = true;   // 진행 중인 공격 판정 코루틴도 이 값을 보고 멈춘다
        animator.Play(AnimState.Hit, force: true);
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        yield return new WaitForSeconds(0.35f);

        animator.enabled = false;   // Hit 마지막 프레임에서 멈춘다
        body.linearVelocity = Vector2.zero;
        body.simulated = false;     // 눕는 동안 콜라이더가 바닥에 걸려 튀지 않게
        float duration = 0.8f;
        float targetAngle = 90f * facing;   // 바라보는 반대쪽(뒤)으로 넘어간다
        Color color = spriteRenderer.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, targetAngle, k * k));
            color.a = Mathf.Lerp(1f, 0.35f, k);
            spriteRenderer.color = color;
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        GameManager.Instance.TriggerGameOver();
    }

    private void OnDrawGizmosSelected()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null || stats.Data == null) return;
        Gizmos.color = Color.red;
        Vector2 offset = new Vector2(stats.Data.attackBoxOffset.x * facing, stats.Data.attackBoxOffset.y);
        Gizmos.DrawWireCube((Vector2)transform.position + offset, stats.Data.attackBoxSize);
    }
}
