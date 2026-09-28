using UnityEngine;

/// <summary>
/// 보스의 파이어볼, 플레이어의 검기 같은 원거리 투사체. targetMask로 누구를 맞힐지 정한다. ObjectPool로 재사용한다.
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
    private float activeLifetime;

    /// <param name="sourcePool">이 투사체를 반납할 풀.</param>
    /// <param name="maxDistance">0보다 크면 이 거리만큼 날아간 뒤 사라진다. 0이면 프리팹의 lifetime을 쓴다.</param>
    public void Launch(Vector2 dir, int dmg, float projectileSpeed, ObjectPool sourcePool, float maxDistance = 0f)
    {
        activeLifetime = maxDistance > 0f && projectileSpeed > 0f ? maxDistance / projectileSpeed : lifetime;
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
        activeLifetime = lifetime;
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        timer += Time.deltaTime;
        if (timer >= activeLifetime) Release();
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
