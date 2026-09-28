using UnityEngine;

/// <summary>
/// 카메라가 대상을 부드럽게 따라간다. 카메라에 붙이고 인스펙터에 Target만 넣으면 된다.
/// <code>
/// cameraFollow.SetTarget(player.transform);
/// cameraFollow.SnapToTarget();   // 리스폰처럼 순간이동시킬 때
/// </code>
/// </summary>
/// <remarks>
/// <b>왜 _Core에 있는가.</b> 카메라 추적은 틀려도 에러가 안 나고 "왠지 화면이 떨린다"로만 드러난다.
/// <list type="bullet">
/// <item><c>Update()</c>에서 따라가면 <b>플레이어가 움직인 뒤에 카메라가 움직일지 전에 움직일지가 프레임마다 달라져서</b>
/// 캐릭터가 미세하게 떤다. 그래서 여기서는 <c>LateUpdate()</c>에서만 움직인다.</item>
/// <item>맵 경계를 안 막으면 끝에서 <b>맵 바깥의 빈 공간</b>이 보인다. 버그처럼 안 보이고 그냥 허전해 보인다.</item>
/// <item>z를 같이 따라가면 2D 카메라가 대상과 같은 평면으로 와서 <b>화면이 아무것도 안 보이게</b> 된다.
/// 여기서는 z를 건드리지 않는다.</item>
/// </list>
/// <para>
/// 이 클래스는 게임 규칙을 모른다. 누구를 따라갈지·언제 경계를 바꿀지는 전부 _Game이 정한다.
/// 일시정지(<c>timeScale = 0</c>)에서는 <c>Time.deltaTime</c>이 0이 되어 저절로 멈춘다.
/// </para>
/// </remarks>
public class CameraFollow2D : MonoBehaviour
{
    [Header("대상")]
    [Tooltip("따라갈 대상. 비어 있으면 아무것도 하지 않는다.")]
    [SerializeField] private Transform target;

    [Tooltip("대상 기준 오프셋. 캐릭터를 화면 아래쪽에 두고 싶으면 y를 올린다.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("추적")]
    [Tooltip("따라잡는 데 걸리는 시간(초). 0에 가까울수록 딱 붙고, 크면 느긋하게 따라온다.")]
    [SerializeField, Min(0f)] private float smoothTime = 0.15f;

    [Header("맵 경계")]
    [Tooltip("켜면 아래 범위 밖으로 카메라가 나가지 않는다.")]
    [SerializeField] private bool useBounds = false;

    [Tooltip("카메라 중심이 갈 수 있는 최소 좌표.")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-10f, -10f);

    [Tooltip("카메라 중심이 갈 수 있는 최대 좌표.")]
    [SerializeField] private Vector2 boundsMax = new Vector2(10f, 10f);

    /// <summary>따라가는 대상. 없으면 null.</summary>
    public Transform Target => target;

    /// <summary>맵 경계를 쓰는지.</summary>
    public bool UseBounds
    {
        get => useBounds;
        set => useBounds = value;
    }

    private Vector3 velocity;

    /// <summary>따라갈 대상을 바꾼다. null을 넣으면 그 자리에 멈춘다.</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        velocity = Vector3.zero;
    }

    /// <summary>부드럽게 따라가지 않고 즉시 대상 위치로 옮긴다. 씬 시작·리스폰·순간이동에 쓴다.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        velocity = Vector3.zero;
        transform.position = Clamp(Desired());
    }

    /// <summary>맵 경계를 정한다. 방이나 스테이지가 바뀔 때 _Game에서 부른다.</summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = Vector2.Min(min, max);
        boundsMax = Vector2.Max(min, max);
        useBounds = true;
    }

    private void Start()
    {
        // 첫 프레임에 카메라가 원점에서 대상까지 미끄러져 오는 것을 막는다.
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 next = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        transform.position = Clamp(next);
    }

    /// <summary>대상 + 오프셋. z는 카메라가 원래 쓰던 값을 그대로 유지한다.</summary>
    private Vector3 Desired()
    {
        return new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);
    }

    private Vector3 Clamp(Vector3 position)
    {
        if (!useBounds) return position;

        return new Vector3(
            Mathf.Clamp(position.x, boundsMin.x, boundsMax.x),
            Mathf.Clamp(position.y, boundsMin.y, boundsMax.y),
            position.z);
    }
}
