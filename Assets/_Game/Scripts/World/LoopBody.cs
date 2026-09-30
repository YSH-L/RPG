using UnityEngine;

/// <summary>
/// 공간 경계를 넘으면 반대편으로 옮겨서 좌우가 끝없이 이어지게 한다.
/// 플레이어·몬스터·투사체처럼 움직이는 것에 붙인다.
/// </summary>
/// <remarks>
/// 카메라가 따라가는 대상이면 카메라도 같은 거리만큼 같이 옮긴다.
/// <c>SnapToTarget()</c>으로 맞추면 카메라가 따라오던 간격만큼 화면이 튀어서 이음새가 보이기 때문이다.
/// 매 프레임 따라가는 것은 여전히 <see cref="CameraFollow2D"/>가 한다 — 여기서는 순간이동 한 번만 한다.
/// </remarks>
public class LoopBody : MonoBehaviour
{
    [SerializeField] private Area area;

    [Tooltip("이 오브젝트를 따라가는 카메라. 플레이어만 넣는다.")]
    [SerializeField] private CameraFollow2D followingCamera;

    private Rigidbody2D body;
    private WrapGhost[] ghosts;

    public Area Area
    {
        get => area;
        set
        {
            area = value;
            if (ghosts == null) ghosts = GetComponentsInChildren<WrapGhost>(true);
            foreach (WrapGhost ghost in ghosts) ghost.Area = value;
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (ghosts == null) ghosts = GetComponentsInChildren<WrapGhost>(true);
        if (area != null) Area = area;
    }

    private void Update()
    {
        if (area == null) return;

        Vector3 position = transform.position;
        float wrapped = area.Wrap(position.x);
        float shift = wrapped - position.x;
        if (Mathf.Abs(shift) < 0.001f) return;

        Vector3 delta = new Vector3(shift, 0f, 0f);
        transform.position = position + delta;
        if (body != null) body.position = transform.position;

        if (followingCamera != null && followingCamera.Target == transform)
        {
            followingCamera.transform.position += delta;
        }
    }
}
