using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일반 몬스터. 배회하다가 플레이어가 가까우면 쫓아가고, 사거리 안이면 공격 동작 끝에 피해를 준다.
/// 몸에 닿아도 피해를 준다. 정의는 <see cref="EnemyStatsData"/>, 지금 체력은 여기.
/// </summary>
/// <remarks>
/// <see cref="ObjectPool"/>로 재사용되므로 초기화는 <c>OnEnable</c>에서 한다.
/// 땅이 평평해서 물리 대신 위치를 직접 옮긴다(몸은 Kinematic 트리거).
/// </remarks>
public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] protected EnemyStatsData data;
    [SerializeField] protected SpriteAnimator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected BoxCollider2D hitbox;
    [SerializeField] protected LoopBody loopBody;
    [SerializeField] protected LayerMask playerMask = 1 << 9;

    [Tooltip("죽는 동작이 끝난 뒤 사라지기까지(초).")]
    [SerializeField, Min(0f)] private float corpseTime = 0.6f;

    public EnemyStatsData Data => data;
    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    protected Area area;
    private EnemySpawner spawner;
    private float homeX;
    private float wanderDirection;
    private float nextDecisionTime;
    private float nextAttackTime;
    private float stunnedUntil;
    private bool meleePending;
    private bool started;
    private float bobPhase;
    private readonly List<Collider2D> contacts = new List<Collider2D>();

    // 원소 상태이상 (범위베기로 걸린다)
    private int burnPerTick;
    private int burnTicksLeft;
    private Color burnTint;
    private float burnInterval;
    private float nextBurnTick;
    private float slowFactor = 1f;
    private float slowUntil;
    private float rootedUntil;

    private static readonly Color SlowTint = new Color(0.6f, 0.8f, 1f);
    private static readonly Color RootTint = new Color(0.6f, 1f, 0.6f);

    /// <summary>스포너가 풀에서 꺼낸 직후 부른다.</summary>
    public void Init(Area home, EnemySpawner owner)
    {
        area = home;
        spawner = owner;
        loopBody.Area = home;
        homeX = transform.position.x;
        bobPhase = Random.value * Mathf.PI * 2f;
        SnapToHeight();
    }

    protected virtual void OnEnable()
    {
        CurrentHP = data.maxHP;
        hitbox.enabled = true;
        spriteRenderer.color = Color.white;
        meleePending = false;
        stunnedUntil = 0f;
        burnTicksLeft = 0;
        slowUntil = 0f;
        rootedUntil = 0f;
        nextDecisionTime = 0f;
        nextAttackTime = Time.time + 1f;
        animator.OnClipFinished += HandleClipFinished;

        // 풀에서 두 번째로 꺼낼 때는 Death 프레임에 잠겨 있다. 처음 켜질 때는 SpriteAnimator.Awake가 이미 Idle을 튼다.
        if (started) animator.Play(AnimState.Idle, force: true);
    }

    protected virtual void OnDisable()
    {
        animator.OnClipFinished -= HandleClipFinished;
        StopAllCoroutines();
    }

    private void Start()
    {
        started = true;
        // 꺼낸 프레임에 바로 죽었으면 Death를 덮어쓰지 않는다. 덮어쓰면 Death가 끝나지 않아 시체가 남는다.
        if (!IsDead) animator.Play(AnimState.Idle, force: true);
    }

    protected virtual void Update()
    {
        if (area == null || IsDead) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        SnapToHeight();
        UpdateStatus();
        if (IsDead) return;   // 화상으로 방금 죽었을 수 있다

        PlayerController player = PlayerController.Instance;
        bool playerHere = player != null && !player.Stats.IsDead && player.CurrentArea == area;

        if (playerHere) DealContactDamage(player);

        if (Time.time < stunnedUntil || Time.time < rootedUntil || animator.IsBusy) return;

        float dx = playerHere ? area.DeltaX(transform.position.x, player.transform.position.x) : 0f;
        float dy = playerHere ? player.transform.position.y - area.GroundY : 0f;
        bool reachableHeight = dy < 2.5f;

        if (playerHere && reachableHeight && TryUseSkill(player, dx)) return;

        if (playerHere && reachableHeight && Mathf.Abs(dx) <= data.attackRange && Time.time >= nextAttackTime)
        {
            StartMelee(dx);
        }
        else if (playerHere && reachableHeight && Mathf.Abs(dx) <= data.detectRange)
        {
            // 너무 붙으면 멈춰서 공격을 기다린다.
            Move(Mathf.Abs(dx) > data.attackRange * 0.6f ? Mathf.Sign(dx) * data.moveSpeed * 1.3f : 0f, dx);
        }
        else
        {
            Wander();
        }
    }

    /// <summary>보스처럼 추가 공격이 있는 몬스터가 덮어쓴다. true면 이번 프레임은 그걸로 끝.</summary>
    protected virtual bool TryUseSkill(PlayerController player, float dx) => false;

    private void Wander()
    {
        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + Random.Range(1.5f, 3.5f);
            float fromHome = area.DeltaX(homeX, transform.position.x);
            if (Mathf.Abs(fromHome) > data.wanderRange) wanderDirection = -Mathf.Sign(fromHome);
            else wanderDirection = Random.Range(-1, 2);
        }
        Move(wanderDirection * data.moveSpeed, wanderDirection);
    }

    private void Move(float velocity, float faceTowards)
    {
        if (Time.time < slowUntil) velocity *= slowFactor;
        transform.position += Vector3.right * (velocity * Time.deltaTime);
        animator.SetFacing(faceTowards);
        animator.Play(velocity != 0f ? AnimState.Walk : AnimState.Idle);
    }

    private void StartMelee(float dx)
    {
        animator.SetFacing(dx);
        animator.Play(AnimState.Attack);
        meleePending = true;
        nextAttackTime = Time.time + data.attackCooldown;
    }

    /// <summary>공격 판정은 공격 동작이 끝난 순간에 한 번. 그 사이에 맞으면 취소된다.</summary>
    protected virtual void HandleClipFinished(AnimState state)
    {
        if (state == AnimState.Attack && meleePending)
        {
            meleePending = false;
            PlayerController player = PlayerController.Instance;
            if (player != null && player.CurrentArea == area)
            {
                float dx = area.DeltaX(transform.position.x, player.transform.position.x);
                float dy = player.transform.position.y - area.GroundY;
                if (Mathf.Abs(dx) <= data.attackRange + 0.4f && dy < 1.5f) player.Stats.TakeDamage(data.attackDamage);
            }
        }
        else if (state == AnimState.Death)
        {
            StartCoroutine(Despawn());
        }
    }

    private void DealContactDamage(PlayerController player)
    {
        if (data.contactDamage <= 0 || player.Stats.IsInvincible) return;

        Bounds bounds = hitbox.bounds;
        area.OverlapBox(bounds.center, bounds.size, playerMask, contacts);
        if (contacts.Count > 0) player.Stats.TakeDamage(data.contactDamage);
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, flinch: true);
    }

    /// <summary>
    /// 범위베기에 실린 원소 효과를 건다. 데미지는 이미 <see cref="TakeDamage"/>로 들어간 뒤다.
    /// </summary>
    /// <param name="dealt">방금 맞은 데미지. 화상량이 이것에 비례한다.</param>
    public void ApplyElement(ElementData element, int dealt)
    {
        if (IsDead || element == null) return;
        float now = Time.time;

        if (element.burnRatio > 0f)
        {
            burnPerTick = Mathf.Max(1, Mathf.RoundToInt(dealt * element.burnRatio));
            burnTint = Color.Lerp(Color.white, element.color, 0.6f);   // Fire는 주황, Arcane은 보라
            burnInterval = element.burnInterval;
            // 3초 동안 1초마다면 3번. 마지막 틱이 끝나는 순간과 겹쳐 빠지지 않도록 횟수로 센다.
            burnTicksLeft = Mathf.Max(1, Mathf.RoundToInt(element.burnDuration / element.burnInterval));
            nextBurnTick = now + burnInterval;
        }
        if (element.slowFactor < 1f)
        {
            slowFactor = element.slowFactor;
            slowUntil = now + element.slowDuration;
        }
        if (element.rootDuration > 0f)
        {
            rootedUntil = now + element.rootDuration;
            meleePending = false;   // 휘두르던 공격도 끊긴다
            animator.Play(AnimState.Idle, force: true);
        }
    }

    /// <summary>화상 피해를 넣고, 걸린 상태에 맞춰 색을 입힌다.</summary>
    private void UpdateStatus()
    {
        float now = Time.time;
        if (burnTicksLeft > 0 && now >= nextBurnTick)
        {
            burnTicksLeft--;
            nextBurnTick += burnInterval;
            ApplyDamage(burnPerTick, flinch: false);   // 지속 피해는 움찔하거나 밀리지 않는다
            if (IsDead) return;
        }

        if (now < rootedUntil) spriteRenderer.color = RootTint;
        else if (burnTicksLeft > 0) spriteRenderer.color = burnTint;
        else if (now < slowUntil) spriteRenderer.color = SlowTint;
        else spriteRenderer.color = Color.white;
    }

    private void ApplyDamage(int amount, bool flinch)
    {
        if (IsDead || amount <= 0) return;   // 죽은 뒤 또 맞으면 보상이 여러 번 들어간다

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        CombatEvents.RaiseDamaged(gameObject, amount);

        if (IsDead)
        {
            meleePending = false;
            hitbox.enabled = false;
            spriteRenderer.color = Color.white;
            animator.Play(AnimState.Death);
            CombatEvents.RaiseDied(gameObject);
            return;
        }

        if (flinch && !data.superArmor)
        {
            meleePending = false;
            stunnedUntil = Time.time + 0.3f;
            animator.Play(AnimState.Hit);

            PlayerController player = PlayerController.Instance;
            if (player != null)
            {
                float away = -Mathf.Sign(area.DeltaX(transform.position.x, player.transform.position.x));
                transform.position += Vector3.right * (away * 0.25f);
            }
        }
    }

    private IEnumerator Despawn()
    {
        yield return new WaitForSeconds(corpseTime);

        Color color = Color.white;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            color.a = 1f - t / 0.4f;
            spriteRenderer.color = color;
            yield return null;
        }

        if (spawner != null) spawner.NotifyGone(this);
        else gameObject.SetActive(false);
    }

    private void SnapToHeight()
    {
        if (area == null) return;
        float y = area.GroundY;
        if (data.flying) y += data.hoverHeight + Mathf.Sin(Time.time * 2f + bobPhase) * 0.15f;
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;
    }
}
