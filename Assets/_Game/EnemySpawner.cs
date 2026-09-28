using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 고정 배치 + 리스폰. 스폰 지점마다 몬스터를 한 마리씩 풀에서 꺼내 두고,
/// 그 몬스터가 죽으면 respawnDelay 뒤에 같은 자리에 다시 꺼낸다.
/// 몬스터 종류(풀)마다 하나씩 둔다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private ObjectPool pool;
    [Tooltip("몬스터를 세울 자리. 비행형은 y를 살짝 올린다.")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(0f)] private float respawnDelay = 6f;

    /// <summary>지금 살아 있는 몬스터 → 그 몬스터가 서 있던 자리.</summary>
    private readonly Dictionary<GameObject, Transform> alive = new Dictionary<GameObject, Transform>();

    private void OnEnable()
    {
        CombatEvents.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        CombatEvents.OnDied -= HandleDied;
    }

    private void Start()
    {
        if (pool == null || spawnPoints == null) return;

        foreach (Transform point in spawnPoints)
        {
            if (point != null) Spawn(point);
        }
    }

    private void Spawn(Transform point)
    {
        GameObject enemy = pool.Get(point.position, Quaternion.identity);
        if (enemy == null) return;

        if (enemy.TryGetComponent<EnemyBase>(out var enemyBase)) enemyBase.SetPool(pool);
        alive[enemy] = point;
    }

    private void HandleDied(GameObject target)
    {
        if (!alive.TryGetValue(target, out Transform point)) return;

        alive.Remove(target);
        StartCoroutine(RespawnAfterDelay(point));
    }

    private IEnumerator RespawnAfterDelay(Transform point)
    {
        yield return new WaitForSeconds(respawnDelay);
        Spawn(point);
    }
}
