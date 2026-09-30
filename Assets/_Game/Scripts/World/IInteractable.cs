/// <summary>플레이어가 가까이서 ↑를 눌렀을 때 반응하는 것. 포탈과 상인이 구현한다.</summary>
public interface IInteractable
{
    void Interact(PlayerController player);
}

/// <summary>상점 창처럼 조작을 가져가는 UI가 열려 있는지. 열려 있으면 캐릭터가 움직이지 않는다.</summary>
public static class InputLock
{
    public static bool Locked { get; set; }
}
