using UnityEngine;

/// <summary>
/// 보스. 일반 몬스터의 근접 공격에 더해, 멀리 있으면 투사체를 쏜다.
/// 체력바는 <see cref="BossHealthBar"/>가 <see cref="Current"/>를 보고 그린다.
/// </summary>
public class BossEnemy : Enemy
{
    [SerializeField] private ObjectPool projectilePool;

    /// <summary>지금 활성화된 보스. 없으면 null.</summary>
    public static BossEnemy Current { get; private set; }

    public BossStatsData BossData => (BossStatsData)data;

    private float nextShotTime;
    private bool shotPending;
    private float shotDirection;

    public void SetProjectilePool(ObjectPool pool)
    {
        projectilePool = pool;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Current = this;
        shotPending = false;
        nextShotTime = Time.time + 2f;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (Current == this) Current = null;
    }

    protected override bool TryUseSkill(PlayerController player, float dx)
    {
        BossStatsData boss = BossData;
        float distance = Mathf.Abs(dx);
        if (Time.time < nextShotTime || distance <= data.attackRange || distance > boss.rangedRange) return false;

        nextShotTime = Time.time + boss.rangedCooldown;
        shotPending = true;
        shotDirection = Mathf.Sign(dx);
        animator.SetFacing(dx);
        animator.Play(AnimState.Attack);
        return true;
    }

    protected override void HandleClipFinished(AnimState state)
    {
        if (state == AnimState.Attack && shotPending)
        {
            shotPending = false;
            Fire();
            return;
        }
        base.HandleClipFinished(state);
    }

    private void Fire()
    {
        if (projectilePool == null || IsDead) return;

        BossStatsData boss = BossData;
        Vector3 muzzle = transform.position + new Vector3(shotDirection * 0.6f, boss.muzzleHeight, 0f);
        GameObject shot = projectilePool.Get(muzzle, Quaternion.identity);
        if (shot != null && shot.TryGetComponent(out Projectile projectile))
        {
            projectile.Launch(projectilePool, area, new Vector2(shotDirection, 0f), boss.projectileSpeed, boss.projectileDamage, playerMask);
        }
    }
}
