using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 조작: ←→ 이동, Space 점프, Z 공격, ↑ 포탈·상인, 1·2 포션.
/// 수치는 전부 <see cref="PlayerStatsData"/>에서 읽는다.
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

    public PlayerStats Stats { get; private set; }
    public Area CurrentArea => loopBody != null ? loopBody.Area : null;

    private Rigidbody2D body;
    private PlayerStatsData data;
    private float facing = 1f;
    private float nextAttackTime;
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
            if (keyboard.spaceKey.wasPressedThisFrame && grounded && Time.time >= knockbackUntil)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, data.jumpVelocity);
                grounded = false;
            }
            if (keyboard.upArrowKey.wasPressedThisFrame) TryInteract();
            if (keyboard.digit1Key.wasPressedThisFrame) Stats.UsePotion(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Stats.UsePotion(1);
        }

        bool attacking = animator.Current == AnimState.Attack && animator.IsBusy;

        if (Time.time >= knockbackUntil)
        {
            // 땅에서 공격하는 동안은 제자리에 선다. 공중 공격은 관성을 유지한다.
            float speed = attacking && grounded ? 0f : move * data.moveSpeed;
            body.linearVelocity = new Vector2(speed, body.linearVelocity.y);

            if (!attacking && move != 0f)
            {
                facing = Mathf.Sign(move);
                animator.SetFacing(move);
            }
        }

        if (!grounded) animator.Play(AnimState.Jump);
        else animator.Play(move != 0f && !attacking ? AnimState.Walk : AnimState.Idle);
    }

    private bool IsGrounded()
    {
        if (body.linearVelocity.y > 0.05f) return false;
        Vector2 feet = (Vector2)transform.position + Vector2.down * 0.02f;
        return Physics2D.OverlapBox(feet, groundCheckSize, 0f, groundMask) != null;
    }

    private void StartAttack()
    {
        nextAttackTime = Time.time + data.attackCooldown;
        animator.Play(AnimState.Attack);
        StartCoroutine(HitAfterDelay(facing));
    }

    private IEnumerator HitAfterDelay(float direction)
    {
        yield return new WaitForSeconds(data.attackHitDelay);
        if (dying || CurrentArea == null) yield break;

        Vector2 offset = new Vector2(data.attackBoxOffset.x * direction, data.attackBoxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;
        CurrentArea.OverlapBox(center, data.attackBoxSize, enemyMask, hits);

        // 가까운 순서로 maxTargets 마리까지.
        hits.Sort((a, b) =>
            Mathf.Abs(CurrentArea.DeltaX(transform.position.x, a.transform.position.x))
                .CompareTo(Mathf.Abs(CurrentArea.DeltaX(transform.position.x, b.transform.position.x))));

        int struck = 0;
        foreach (Collider2D hit in hits)
        {
            if (struck >= data.maxTargets) break;
            if (!hit.TryGetComponent(out IDamageable target)) continue;
            if (hit.TryGetComponent(out Enemy enemy) && enemy.IsDead) continue;

            target.TakeDamage(Stats.RollDamage());
            struck++;

            if (hitEffectPool != null)
            {
                GameObject effect = hitEffectPool.Get(hit.bounds.center, Quaternion.identity);
                if (effect != null && effect.TryGetComponent(out OneShotEffect oneShot)) oneShot.Play(hitEffectPool, CurrentArea);
            }
        }
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

    /// <summary>맞은 뒤 무적 시간 동안 깜빡인다.</summary>
    private void UpdateBlink()
    {
        Color color = spriteRenderer.color;
        color.a = Stats.IsInvincible && Mathf.FloorToInt(Time.time * 16f) % 2 == 0 ? 0.35f : 1f;
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
