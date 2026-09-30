using System;
using UnityEngine;

/// <summary>
/// 지금 어느 공간에 있는지와 공간 사이 이동. 포탈이 <see cref="Travel"/>을 부른다.
/// </summary>
public class AreaManager : MonoBehaviour
{
    public static AreaManager Instance { get; private set; }

    [SerializeField] private Area startArea;
    [SerializeField] private PlayerController player;
    [SerializeField] private CameraFollow2D cameraFollow;

    public Area Current { get; private set; }

    /// <summary>공간이 바뀌었을 때. HUD가 공간 이름을 갱신한다.</summary>
    public event Action<Area> OnAreaChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (startArea != null) Enter(startArea, startArea.CenterX);
    }

    /// <summary>다른 공간으로 옮긴다. x는 도착할 월드 x좌표.</summary>
    public void Travel(Area destination, float x)
    {
        if (destination == null) return;

        if (Current != null && Current.Spawner != null) Current.Spawner.Deactivate();
        Enter(destination, x);

        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(destination.DisplayName);
    }

    private void Enter(Area area, float x)
    {
        Current = area;

        player.TeleportTo(area, new Vector2(x, area.GroundY));

        // 좌우는 끝없이 이어지므로 막지 않고, 높이만 공간에 고정한다.
        cameraFollow.SetBounds(new Vector2(-100000f, area.CameraY), new Vector2(100000f, area.CameraY));
        cameraFollow.SnapToTarget();

        if (area.Spawner != null) area.Spawner.Activate();
        if (area.Bgm != null && SoundManager.Instance != null) SoundManager.Instance.PlayBGM(area.Bgm);

        OnAreaChanged?.Invoke(area);
    }
}
