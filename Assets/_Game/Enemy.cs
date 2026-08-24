using UnityEngine;

/// <summary>
/// 일반 몬스터: 탐지 범위 안에 들어온 플레이어를 쫓아가고, 닿으면 접촉 피해를 준다.
/// 종류가 늘어나도 이 스크립트는 그대로 두고 EnemyStatsData 에셋만 새로 만들면 된다.
/// </summary>
public class Enemy : EnemyBase
{
    [Header("접촉 피해")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField, Min(0f)] private float stopDistance = 0.35f;

    private float contactTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        contactTimer = 0f;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || isDead) return;
        if (contactTimer > 0f) contactTimer -= Time.deltaTime;

        Transform target = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        if (target == null) return;

        float dx = target.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);

        if (distance <= data.detectionRange && distance > stopDistance)
        {
            float moveX = Mathf.Sign(dx);
            transform.position += Vector3.right * moveX * data.moveSpeed * Time.deltaTime;
            animator.SetFacing(moveX);
            animator.Play(AnimState.Walk);
        }
        else
        {
            animator.Play(AnimState.Idle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryContactDamage(other);
    private void OnTriggerStay2D(Collider2D other) => TryContactDamage(other);

    private void TryContactDamage(Collider2D other)
    {
        if (isDead || contactTimer > 0f) return;
        if (((1 << other.gameObject.layer) & playerMask) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(data.contactDamage);
            contactTimer = data.contactDamageInterval;
        }
    }
}
