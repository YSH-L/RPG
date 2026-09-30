using System.Collections.Generic;
using UnityEngine;

/// <summary>보스가 쏘는 투사체. 풀에서 꺼내 <see cref="Launch"/>로 쏘고, 맞거나 수명이 다하면 풀로 돌아간다.</summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private LoopBody loopBody;
    [SerializeField] private Vector2 hitSize = new Vector2(0.5f, 0.4f);
    [SerializeField, Min(0.1f)] private float lifetime = 4f;

    private ObjectPool pool;
    private Area area;
    private Vector2 velocity;
    private int damage;
    private int targetMask;
    private float releaseTime;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    public void Launch(ObjectPool owner, Area home, Vector2 direction, float speed, int amount, LayerMask mask)
    {
        pool = owner;
        area = home;
        loopBody.Area = home;
        velocity = direction.normalized * speed;
        damage = amount;
        targetMask = mask;
        releaseTime = Time.time + lifetime;
        spriteRenderer.flipX = direction.x < 0f;   // 원본은 오른쪽으로 날아간다
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
            if (hit.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(damage);
                Release();
                return;
            }
        }
    }

    private void Release()
    {
        area = null;
        if (pool != null) pool.Release(gameObject);
        else gameObject.SetActive(false);
    }
}
