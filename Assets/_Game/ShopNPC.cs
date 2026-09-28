using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 필드의 상인. 앞에 서서 ↑(또는 W)를 누르면 상점 창이 열린다. 파는 물건 목록은 여기, 가격은 각 ItemData 에셋에 있다.
/// </summary>
public class ShopNPC : MonoBehaviour
{
    [SerializeField] private string shopName = "Shop";
    [SerializeField] private ItemData[] stock;
    [SerializeField] private ShopWindow window;
    [Tooltip("상인 중심에서 이 가로 거리 안에 플레이어가 있어야 열린다.")]
    [SerializeField, Min(0.1f)] private float useRadius = 0.9f;

    public string ShopName => shopName;
    public ItemData[] Stock => stock;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (window == null || window.IsOpen) return;

        PlayerController player = PlayerController.Instance;
        if (player == null || player.IsDead || player.ControlLocked) return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !(kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return;

        Vector2 offset = player.transform.position - transform.position;
        if (Mathf.Abs(offset.x) > useRadius || Mathf.Abs(offset.y) > 2f) return;

        window.Open(this);
    }
}
