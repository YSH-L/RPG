using UnityEngine;

/// <summary>위에서 ↑를 누르면 다른 공간으로 보내 준다. 들어갈 공간의 필요 레벨을 확인한다.</summary>
public class Portal : MonoBehaviour, IInteractable
{
    [SerializeField] private Area destination;
    [Tooltip("도착 공간 중심에서 얼마나 떨어진 곳에 내릴지.")]
    [SerializeField] private float arrivalOffsetX;

    public Area Destination => destination;

    public void Interact(PlayerController player)
    {
        if (destination == null) return;

        if (player.Stats.Level < destination.RequiredLevel)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage($"Requires Lv.{destination.RequiredLevel}");
            }
            return;
        }

        AreaManager.Instance.Travel(destination, destination.CenterX + arrivalOffsetX);
    }
}
