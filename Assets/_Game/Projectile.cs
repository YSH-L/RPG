using UnityEngine;

/// <summary>
/// 보스의 파이어볼 같은 원거리 투사체. ObjectPool로 재사용한다.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask;
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    private Vector2 direction;
    private float speed;
    private int damage;
    private ObjectPool pool;
    private float timer;

    /// <param name="pool">이 투사체를 반납할 풀.</param>
    public void Launch(Vector2 dir, int dmg, float projectileSpeed, ObjectPool sourcePool)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        damage = dmg;
        speed = projectileSpeed;
        pool = sourcePool;

        // 원본 스프라이트가 오른쪽을 보고 있으므로 날아가는 방향으로 돌린다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        timer += Time.deltaTime;
        if (timer >= lifetime) Release();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetMask) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
        }

        Release();
    }

    private void Release()
    {
        if (pool != null) pool.Release(gameObject);
        else gameObject.SetActive(false);
    }
}
