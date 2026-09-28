using UnityEngine;

/// <summary>
/// 몬스터·보스가 공유하는 최소 골격: 체력, 피격, 사망, 풀 반납.
/// 이동·공격 패턴은 Enemy / BossController가 각자 구현한다.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("정의")]
    [SerializeField] protected EnemyStatsData data;

    [Header("참조")]
    [SerializeField] protected SpriteAnimator animator;
    [Tooltip("이 인스턴스를 꺼내 쓴 풀. 죽으면 여기로 반납한다.")]
    [SerializeField] protected ObjectPool pool;

    public int CurrentHP { get; protected set; }
    public int MaxHP => data.maxHP;
    public int ExpReward => data.expReward;
    public int GoldReward => data.goldReward;
    protected bool isDead;

    /// <summary>스포너가 풀에서 꺼낼 때 불러준다. 프리팹은 씬의 풀을 참조할 수 없어서다.</summary>
    public void SetPool(ObjectPool sourcePool) => pool = sourcePool;

    protected virtual void OnEnable()
    {
        CurrentHP = data.maxHP;
        isDead = false;
        animator.OnClipFinished += HandleClipFinished;
        animator.Play(AnimState.Idle, force: true);
    }

    protected virtual void OnDisable()
    {
        animator.OnClipFinished -= HandleClipFinished;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

        CurrentHP -= amount;
        CombatEvents.RaiseDamaged(gameObject, amount);

        if (CurrentHP <= 0)
        {
            Die();
        }
        else
        {
            animator.Play(AnimState.Hit);
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        CombatEvents.RaiseDied(gameObject);
        animator.Play(AnimState.Death, force: true);
    }

    private void HandleClipFinished(AnimState state)
    {
        if (state != AnimState.Death) return;

        if (pool != null) pool.Release(gameObject);
        else gameObject.SetActive(false);
    }
}
