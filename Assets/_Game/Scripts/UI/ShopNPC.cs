using UnityEngine;

/// <summary>가까이서 ↑를 누르면 상점 창을 연다.</summary>
public class ShopNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string shopTitle = "Shop";
    [SerializeField] private ItemData[] items;
    [SerializeField] private ShopWindow window;

    public void Interact(PlayerController player)
    {
        if (window != null) window.Open(shopTitle, items, player.Stats);
    }
}
