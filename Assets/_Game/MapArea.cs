using System;
using UnityEngine;

/// <summary>
/// 한 맵(필드·보스맵)의 이름과 카메라 경계. 포탈이 이 맵으로 넘어올 때 읽어 간다.
/// 모든 맵은 같은 씬 안에 x축으로 떨어져 놓여 있다 — 씬을 나누지 않아서 매니저와 재시작이 그대로 돈다.
/// </summary>
public class MapArea : MonoBehaviour
{
    [SerializeField] private string displayName = "Field";
    [Tooltip("이 맵에 있는 동안 카메라 중심이 갈 수 있는 최소 좌표.")]
    [SerializeField] private Vector2 cameraMin;
    [Tooltip("이 맵에 있는 동안 카메라 중심이 갈 수 있는 최대 좌표.")]
    [SerializeField] private Vector2 cameraMax;
    [Tooltip("보스맵이면 그 보스. 들어오면 화면 상단에 체력바가 뜬다. 필드는 비워둔다.")]
    [SerializeField] private BossController boss;
    [Tooltip("저장을 불러왔을 때 플레이어가 설 자리 (발 위치).")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("보스맵이면 그 앞 필드. 저장을 불러올 때 보스맵 대신 여기서 시작한다.")]
    [SerializeField] private MapArea retreatTo;
    [Tooltip("이 맵에 있는 동안 반복할 배경음악. MusicDirector가 튼다.")]
    [SerializeField] private AudioClip bgm;

    public string DisplayName => displayName;
    public BossController Boss => boss;
    public Transform SpawnPoint => spawnPoint;
    public MapArea RetreatTo => retreatTo;
    public AudioClip Bgm => bgm;

    /// <summary>플레이어가 포탈로 이 맵에 들어왔을 때. 구독했으면 OnDisable에서 해제한다.</summary>
    public static event Action<MapArea> OnEntered;

    /// <summary>카메라를 이 맵의 경계로 바꾸고 즉시 붙인 뒤 입장을 알린다. 순간이동 직후에 부른다.</summary>
    public void Enter(CameraFollow2D cameraFollow)
    {
        if (cameraFollow != null)
        {
            cameraFollow.SetBounds(cameraMin, cameraMax);
            cameraFollow.SnapToTarget();
        }

        OnEntered?.Invoke(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (cameraMin + cameraMax) * 0.5f;
        Gizmos.DrawWireCube(center, cameraMax - cameraMin);
    }
}
