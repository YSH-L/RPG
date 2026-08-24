using System.Collections;
using UnityEngine;

/// <summary>
/// Fire Worm 보스: 가까우면 근접 공격, 사거리 밖이면 접근하거나 파이어볼을 쏜다.
/// data 필드에는 BossStatsData 에셋을 넣는다 (EnemyStatsData를 상속해서 인스펙터에 그대로 꽂힌다).
/// </summary>
public class BossController : EnemyBase
{
    [Header("원거리 공격")]
    [SerializeField] private ObjectPool fireballPool;
    [SerializeField] private Transform firePoint;

    private BossStatsData Boss => (BossStatsData)data;

    private float meleeTimer;
    private float rangedTimer;

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

        float dx = target.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);
        float moveX = Mathf.Sign(dx);

        if (distance <= Boss.meleeRange)
        {
            animator.SetFacing(moveX);

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
            if (distance >= Boss.rangedMinDistance && rangedTimer <= 0f)
            {
                rangedTimer = Boss.rangedCooldown;
                StartCoroutine(RangedAttackRoutine());
            }
            else
            {
                transform.position += Vector3.right * moveX * data.moveSpeed * Time.deltaTime;
                animator.SetFacing(moveX);
                animator.Play(AnimState.Walk);
            }
        }
        else
        {
            animator.Play(AnimState.Idle);
        }
    }

    private IEnumerator MeleeAttackRoutine()
    {
        animator.Play(AnimState.Attack);
        yield return new WaitForSeconds(0.3f);
        if (isDead) yield break;

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
        yield return new WaitForSeconds(0.35f);
        if (isDead || fireballPool == null) yield break;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject fb = fireballPool.Get(spawnPos, Quaternion.identity);
        if (fb == null || !fb.TryGetComponent<Projectile>(out var projectile)) yield break;

        Transform target = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        Vector2 dir = target != null ? ((Vector2)(target.position - spawnPos)) : Vector2.right;
        projectile.Launch(dir, Boss.projectileDamage, Boss.projectileSpeed, fireballPool);
    }
}
