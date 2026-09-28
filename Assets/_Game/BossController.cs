using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 공용: 가까우면 근접 공격, 사거리 밖이면 접근하거나 투사체를 쏜다.
/// Fire Worm(파이어볼)과 Mecha-stone Golem(팔 발사)이 같은 스크립트를 쓰고, 차이는 BossStatsData 에셋과
/// 투사체 풀로만 난다. data 필드에는 BossStatsData 에셋을 넣는다 (EnemyStatsData를 상속해서 그대로 꽂힌다).
/// </summary>
public class BossController : EnemyBase
{
    [Header("원거리 공격")]
    [SerializeField] private ObjectPool projectilePool;
    [Tooltip("오른쪽을 볼 때 기준, 발밑에서 투사체가 나가는 위치까지의 거리(월드 유닛). 왼쪽을 보면 x가 뒤집힌다.")]
    [SerializeField] private Vector2 fireOffset = new Vector2(1f, 1f);

    private BossStatsData Boss => (BossStatsData)data;

    public bool EndsGame => Boss.endsGame;
    public string DisplayName => Boss.displayName;

    private float meleeTimer;
    private float rangedTimer;
    private float facingSign = 1f;

    protected override void OnEnable()
    {
        base.OnEnable();
        meleeTimer = 0f;
        rangedTimer = Boss.rangedCooldown * 0.3f;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || isDead) return;

        Transform target = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        if (target == null) return;

        if (meleeTimer > 0f) meleeTimer -= Time.deltaTime;
        if (rangedTimer > 0f) rangedTimer -= Time.deltaTime;

        // 공격 동작 중에는 제자리에서 방향도 바꾸지 않는다.
        if (animator.IsBusy) return;

        float dx = target.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);
        float moveX = Mathf.Sign(dx);

        if (distance <= Boss.meleeRange)
        {
            Face(moveX);

            if (meleeTimer <= 0f)
            {
                meleeTimer = Boss.meleeCooldown;
                StartCoroutine(MeleeAttackRoutine());
            }
            else
            {
                animator.Play(AnimState.Idle);
            }
        }
        else if (distance <= data.detectionRange)
        {
            Face(moveX);

            if (distance >= Boss.rangedMinDistance && rangedTimer <= 0f)
            {
                rangedTimer = Boss.rangedCooldown;
                StartCoroutine(RangedAttackRoutine());
            }
            else
            {
                transform.position += Vector3.right * moveX * data.moveSpeed * Time.deltaTime;
                animator.Play(AnimState.Walk);
            }
        }
        else
        {
            animator.Play(AnimState.Idle);
        }
    }

    private void Face(float directionX)
    {
        facingSign = directionX;
        animator.SetFacing(directionX);
    }

    private IEnumerator MeleeAttackRoutine()
    {
        animator.Play(AnimState.Attack);
        yield return new WaitForSeconds(Boss.meleeHitDelay);
        if (isDead) yield break;
        Sfx.Play(Boss.attackSound);

        Transform target = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        if (target == null) yield break;

        if (Mathf.Abs(target.position.x - transform.position.x) <= Boss.meleeRange + 0.3f)
        {
            if (target.TryGetComponent<IDamageable>(out var dmgTarget)) dmgTarget.TakeDamage(data.contactDamage);
        }
    }

    private IEnumerator RangedAttackRoutine()
    {
        animator.Play(AnimState.Attack);
        yield return new WaitForSeconds(Boss.rangedFireDelay);
        if (isDead || projectilePool == null) yield break;

        Vector3 spawnPos = transform.position + new Vector3(fireOffset.x * facingSign, fireOffset.y, 0f);
        Sfx.Play(Boss.attackSound);
        GameObject go = projectilePool.Get(spawnPos, Quaternion.identity);
        if (go == null || !go.TryGetComponent<Projectile>(out var projectile)) yield break;

        // 발이 아니라 몸통을 노린다.
        Transform target = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        Vector2 dir = target != null
            ? (Vector2)(target.position + Vector3.up * 0.5f - spawnPos)
            : Vector2.right * facingSign;
        projectile.Launch(dir, Boss.projectileDamage, Boss.projectileSpeed, projectilePool);
    }
}
