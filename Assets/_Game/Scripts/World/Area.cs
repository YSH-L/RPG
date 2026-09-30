using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좌우가 이어진 공간 하나(마을·상점·던전·보스 던전).
/// 폭(<see cref="LoopWidth"/>)만큼 한쪽으로 가면 반대편에서 다시 나온다.
/// </summary>
/// <remarks>
/// 공간들은 한 씬 안에 세로로 떨어뜨려 놓여 있다. 이 오브젝트의 위치가 공간의 중심 x와 지면 y다.
/// 경계 근처에서도 판정이 끊기지 않도록, 판정은 <see cref="OverlapBox"/>로 경계 너머까지 같이 본다.
/// </remarks>
public class Area : MonoBehaviour
{
    private static readonly List<Area> all = new List<Area>();

    [SerializeField] private string displayName = "Area";
    [Tooltip("이 공간에 들어가는 데 필요한 레벨.")]
    [SerializeField, Min(1)] private int requiredLevel = 1;
    [Tooltip("한 바퀴 폭. 이만큼 가면 제자리로 돌아온다. 배경 반복 폭의 배수여야 이음새가 안 보인다.")]
    [SerializeField, Min(10f)] private float loopWidth = 31f;
    [Tooltip("카메라 중심이 지면에서 얼마나 위에 있는지.")]
    [SerializeField] private float cameraHeight = 2.75f;
    [Tooltip("이 공간의 몬스터 스포너. 마을·상점은 비워 둔다.")]
    [SerializeField] private EnemySpawner spawner;
    [Tooltip("들어왔을 때 틀 BGM. 비워 두면 바꾸지 않는다.")]
    [SerializeField] private AudioClip bgm;

    public string DisplayName => displayName;
    public int RequiredLevel => requiredLevel;
    public float LoopWidth => loopWidth;
    public float CenterX => transform.position.x;
    public float GroundY => transform.position.y;
    public float CameraY => GroundY + cameraHeight;
    public EnemySpawner Spawner => spawner;
    public AudioClip Bgm => bgm;

    private void OnEnable() { all.Add(this); }
    private void OnDisable() { all.Remove(this); }

    /// <summary>x를 이 공간의 한 바퀴 안(중심 ± 폭/2)으로 접는다.</summary>
    public float Wrap(float x)
    {
        return CenterX + DeltaX(CenterX, x);
    }

    /// <summary>from에서 to까지 가장 가까운 방향의 x 거리. 경계를 넘어가는 쪽이 가까우면 그쪽으로 잰다.</summary>
    public float DeltaX(float from, float to)
    {
        float half = loopWidth * 0.5f;
        return Mathf.Repeat(to - from + half, loopWidth) - half;
    }

    /// <summary>
    /// 경계 너머까지 포함한 상자 판정. 결과는 중복 없이 results에 담긴다.
    /// </summary>
    public int OverlapBox(Vector2 center, Vector2 size, int layerMask, List<Collider2D> results)
    {
        results.Clear();
        for (int i = -1; i <= 1; i++)
        {
            Vector2 shifted = center + Vector2.right * (loopWidth * i);
            Collider2D[] hits = Physics2D.OverlapBoxAll(shifted, size, 0f, layerMask);
            foreach (Collider2D hit in hits)
            {
                if (!results.Contains(hit)) results.Add(hit);
            }
        }
        return results.Count;
    }

    /// <summary>그 위치가 속한 공간. 공간들은 세로로 충분히 떨어져 있어서 가장 가까운 지면을 고른다.</summary>
    public static Area FindAt(Vector2 position)
    {
        Area best = null;
        float bestDistance = float.MaxValue;
        foreach (Area area in all)
        {
            float distance = Mathf.Abs(position.y - area.GroundY);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = area;
            }
        }
        return best;
    }
}
