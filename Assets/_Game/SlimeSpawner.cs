using UnityEngine;

/// <summary>
/// 자식 Transform 각각을 스폰 지점으로 삼아, 시작할 때 풀에서 몬스터를 한 마리씩 꺼내 배치한다.
/// </summary>
public class SlimeSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPool pool;

    private void Start()
    {
        if (pool == null) return;

        foreach (Transform point in transform)
        {
            GameObject enemy = pool.Get(point.position, Quaternion.identity);
            if (enemy != null && enemy.TryGetComponent<EnemyBase>(out var eb)) eb.SetPool(pool);
        }
    }
}
