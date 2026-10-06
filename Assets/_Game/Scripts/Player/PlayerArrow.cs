using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Archer가 쏘는 화살. 처음 맞힌 몬스터 하나에 데미지를 주고 풀로 돌아간다.
/// 원소 효과·흡혈·이펙트는 쏜 쪽(<see cref="PlayerController.OnArrowHit"/>)이 건다.
/// </summary>
/// <remarks>
/// 몬스터 투사체(<see cref="Projectile"/>)와 같은 방식으로, 공간 경계 너머까지 <see cref="Area.OverlapBox"/>로 판정한다.
/// </remarks>
public class PlayerArrow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private LoopBody loopBody;
    [SerializeField] private Vector2 hitSize = new Vector2(0.5f, 0.3f);

    private ObjectPool pool;
    private Area area;
    private Vector2 velocity;
    private int damage;
    private int targetMask;
    private float releaseTime;
    private PlayerController owner;
    private ElementData element;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    public void Launch(ObjectPool fromPool, Area home, Vector2 direction, float speed, float lifetime, int amount,
        LayerMask mask, PlayerController shooter, ElementData withElement, Color tint)
    {
        pool = fromPool;
        area = home;
        loopBody.Area = home;
        velocity = direction.normalized * speed;
        damage = amount;
        targetMask = mask;
        releaseTime = Time.time + lifetime;
        owner = shooter;
        element = withElement;
        spriteRenderer.color = tint;

        // 원본 그림은 오른쪽을 향한다. 왼쪽으로 쏘면 뒤집고, 위아래 각도만큼 기울인다.
        spriteRenderer.flipX = velocity.x < 0f;
        float angle = Mathf.Atan2(velocity.y, Mathf.Abs(velocity.x)) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, velocity.x < 0f ? -angle : angle);
    }

    private void Update()
    {
        if (area == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.GameOver) Release();
            return;
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (Time.time >= releaseTime)
        {
            Release();
            return;
        }

        area.OverlapBox(transform.position, hitSize, targetMask, hits);
        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent(out IDamageable target)) continue;
            if (hit.TryGetComponent(out Enemy enemy) && enemy.IsDead) continue;

            target.TakeDamage(damage);
            if (owner != null) owner.OnArrowHit(hit, damage, element);
            Release();
            return;
        }
    }

    private void Release()
    {
        area = null;
        if (pool != null) pool.Release(gameObject);
        else gameObject.SetActive(false);
    }
}
