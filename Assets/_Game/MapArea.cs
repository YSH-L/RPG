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

    public string DisplayName => displayName;

    /// <summary>카메라를 이 맵의 경계로 바꾸고 즉시 붙인다. 순간이동 직후에 부른다.</summary>
    public void ApplyCamera(CameraFollow2D cameraFollow)
    {
        if (cameraFollow == null) return;

        cameraFollow.SetBounds(cameraMin, cameraMax);
        cameraFollow.SnapToTarget();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (cameraMin + cameraMax) * 0.5f;
        Gizmos.DrawWireCube(center, cameraMax - cameraMin);
    }
}
