using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공간 하나의 몬스터를 채운다. 플레이어가 들어오면 <see cref="Activate"/>, 나가면 <see cref="Deactivate"/>.
/// 죽은 몬스터는 풀로 돌려보내고 잠시 뒤 다시 채운다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Area area;
    [SerializeField] private ObjectPool pool;
    [SerializeField, Min(0)] private int maxAlive = 6;
    [SerializeField, Min(0f)] private float respawnDelay = 5f;
    [Tooltip("끄면 한 번 잡은 뒤로 다시 나오지 않는다. 보스용.")]
    [SerializeField] private bool respawn = true;
    [Tooltip("도착 지점(공간 중심) 주변 이 거리 안에는 스폰하지 않는다.")]
    [SerializeField, Min(0f)] private float safeRadius = 4f;
    [Tooltip("지정하면 무작위 대신 이 x(공간 중심 기준)에 스폰한다.")]
    [SerializeField] private bool useFixedX;
    [SerializeField] private float fixedX;
    [Tooltip("보스가 쏠 투사체 풀. 프리팹은 씬 오브젝트를 참조할 수 없어서 꺼낼 때 넣어 준다.")]
    [SerializeField] private ObjectPool projectilePool;

    private readonly List<Enemy> alive = new List<Enemy>();
    private bool active;
    private bool cleared;
    private int pending;

    public bool Cleared => cleared;

    public void Activate()
    {
        active = true;
        Fill();
    }

    public void Deactivate()
    {
        active = false;
        StopAllCoroutines();
        pending = 0;
        foreach (Enemy enemy in alive)
        {
            if (enemy != null) pool.Release(enemy.gameObject);
        }
        alive.Clear();
    }

    /// <summary>몬스터가 죽는 연출을 끝냈을 때 그 몬스터가 부른다.</summary>
    public void NotifyGone(Enemy enemy)
    {
        alive.Remove(enemy);
        pool.Release(enemy.gameObject);

        if (!respawn)
        {
            cleared = true;
            return;
        }

        if (active)
        {
            pending++;
            StartCoroutine(RespawnLater());
        }
    }

    private IEnumerator RespawnLater()
    {
        yield return new WaitForSeconds(respawnDelay);
        pending--;
        if (active) Fill();
    }

    private void Fill()
    {
        if (cleared) return;
        while (alive.Count + pending < maxAlive)
        {
            if (!SpawnOne()) break;
        }
    }

    private bool SpawnOne()
    {
        float x;
        if (useFixedX)
        {
            x = area.CenterX + fixedX;
        }
        else
        {
            float half = area.LoopWidth * 0.5f;
            float side = Random.value < 0.5f ? -1f : 1f;
            x = area.CenterX + side * Random.Range(Mathf.Min(safeRadius, half), half);
        }

        GameObject go = pool.Get(new Vector3(x, area.GroundY, 0f), Quaternion.identity);
        if (go == null || !go.TryGetComponent(out Enemy enemy)) return false;

        enemy.Init(area, this);
        if (projectilePool != null && enemy is BossEnemy boss) boss.SetProjectilePool(projectilePool);
        alive.Add(enemy);
        return true;
    }
}
