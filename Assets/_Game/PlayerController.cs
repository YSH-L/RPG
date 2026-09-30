using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동·공격·레벨업·피격을 담당한다. Swordsman/Archer 공용 —
/// 종류별 수치는 PlayerStatsData 에셋으로 갈아끼운다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour, IDamageable
{
    public static PlayerController Instance { get; private set; }

    [Header("정의")]
    [SerializeField] private PlayerStatsData data;

    [Header("참조")]
    [SerializeField] private SpriteAnimator animator;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.12f;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxHP => data.GetMaxHP(CurrentLevel);
    public int ExpToNextLevel => CurrentLevel >= data.maxLevel ? 0 : data.GetExpToNextLevel(CurrentLevel);
    public bool IsDead { get; private set; }

    /// <summary>레벨이 바뀔 때. (새 레벨)</summary>
    public event Action<int> OnLevelChanged;
    /// <summary>경험치가 바뀔 때. (현재 경험치, 다음 레벨까지 필요한 경험치)</summary>
    public event Action<int, int> OnExpChanged;
    /// <summary>체력이 바뀔 때. (현재 체력, 최대 체력)</summary>
    public event Action<int, int> OnHPChanged;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float facingSign = 1f;
    private float attackCooldownTimer;
    private float invulnerableTimer;
    private bool ignoreJumpThisFrame;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        CurrentLevel = 1;
        CurrentExp = 0;
        CurrentHP = MaxHP;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleStateChanged(GameState state)
    {
        // Ready에서 Space로 시작하는 프레임에 같은 입력으로 같이 점프하는 것을 막는다.
        if (state == GameState.Playing) ignoreJumpThisFrame = true;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || IsDead)
        {
            ignoreJumpThisFrame = false;
            return;
        }

        if (invulnerableTimer > 0f) invulnerableTimer -= Time.deltaTime;
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float moveX = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1f;

        rb.linearVelocity = new Vector2(moveX * data.moveSpeed, rb.linearVelocity.y);
        if (moveX != 0f) facingSign = Mathf.Sign(moveX);
        animator.SetFacing(moveX);

        bool grounded = IsGrounded();

        if (kb.spaceKey.wasPressedThisFrame && grounded && !ignoreJumpThisFrame)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, data.jumpForce);
        }

        if (kb.leftCtrlKey.wasPressedThisFrame && attackCooldownTimer <= 0f)
        {
            attackCooldownTimer = data.attackCooldown;
            StartCoroutine(AttackRoutine());
        }

        animator.Play(grounded ? (moveX != 0f ? AnimState.Walk : AnimState.Idle) : AnimState.Jump);

        ignoreJumpThisFrame = false;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return true;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

    private IEnumerator AttackRoutine()
    {
        animator.Play(AnimState.Attack);
        yield return new WaitForSeconds(data.attackHitDelay);
        if (IsDead) yield break;

        Vector2 origin = (Vector2)transform.position + new Vector2(data.attackRange * 0.5f * facingSign, 0f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, data.attackRange * 0.5f, enemyMask);
        int damage = data.GetAttackPower(CurrentLevel);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target)) target.TakeDamage(damage);
        }
    }

    public void GainExp(int amount)
    {
        if (IsDead || amount <= 0) return;

        bool leveledUp = false;
        CurrentExp += amount;

        while (CurrentLevel < data.maxLevel && CurrentExp >= data.GetExpToNextLevel(CurrentLevel))
        {
            CurrentExp -= data.GetExpToNextLevel(CurrentLevel);
            CurrentLevel++;
            leveledUp = true;
        }

        if (CurrentLevel >= data.maxLevel) CurrentExp = 0;

        if (leveledUp)
        {
            CurrentHP = MaxHP;
            OnLevelChanged?.Invoke(CurrentLevel);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Level Up! Lv.{CurrentLevel}");
        }

        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || invulnerableTimer > 0f || amount <= 0) return;

        int defense = data.GetDefense(CurrentLevel);
        int actual = Mathf.Max(1, amount - defense);
        CurrentHP = Mathf.Max(0, CurrentHP - actual);

        CombatEvents.RaiseDamaged(gameObject, actual);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        invulnerableTimer = data.invulnerabilityDuration;

        if (CurrentHP <= 0)
        {
            Die();
        }
        else
        {
            animator.Play(AnimState.Hit);
        }
    }

    private void Die()
    {
        IsDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        animator.Play(AnimState.Hit, force: true);
        CombatEvents.RaiseDied(gameObject);
        StartCoroutine(DeathFallRoutine());
    }

    /// <summary>Swordsman/Archer에는 death 프레임이 없어서, 각도와 색을 바꿔 쓰러지는 것으로 대신한다.</summary>
    private IEnumerator DeathFallRoutine()
    {
        const float duration = 0.6f;
        float elapsed = 0f;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, facingSign >= 0f ? -80f : 80f);
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r * 0.4f, startColor.g * 0.4f, startColor.b * 0.4f, startColor.a);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        transform.rotation = endRot;
        spriteRenderer.color = endColor;
    }
}
