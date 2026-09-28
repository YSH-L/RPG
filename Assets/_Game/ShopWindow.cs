using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 상점 창. ShopNPC가 Open으로 연다. 열려 있는 동안 플레이어는 움직이지 않고 피해도 받지 않는다.
/// <para>
/// 게임을 멈추지 않는 이유: Time.timeScale은 GameManager.PauseGame()만 건드리는 규칙이고,
/// PauseGame을 부르면 Pause 패널이 같이 뜬다. 그래서 대신 플레이어를 잠그고 무적으로 둔다.
/// </para>
/// 닫기: [Close] 버튼, 또는 ↑/W를 한 번 더. HUD_Panel 안에 둔다.
/// </summary>
public class ShopWindow : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerController player;
    [Tooltip("켜고 끌 창 본체. 이 스크립트가 붙은 오브젝트 자신이 아니라 자식이어야 한다.")]
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button closeButton;
    [SerializeField] private ItemRowView[] rows;

    private ShopNPC shop;
    private int openedFrame;

    public bool IsOpen => content != null && content.activeSelf;

    private void Awake()
    {
        if (content != null) content.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += Refresh;
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
        // HUD가 통째로 꺼지면(GameOver 등) 플레이어 잠금이 남지 않게 한다.
        if (IsOpen) Close();
    }

    public void Open(ShopNPC npc)
    {
        if (content == null || player == null) return;

        shop = npc;
        openedFrame = Time.frameCount;
        content.SetActive(true);

        player.ControlLocked = true;
        if (player.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        Refresh();
    }

    public void Close()
    {
        if (content != null) content.SetActive(false);
        shop = null;
        if (player != null) player.ControlLocked = false;
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (player != null) player.GrantInvulnerability(0.2f);

        // 여는 데 쓴 ↑ 입력으로 바로 닫히지 않게, 연 프레임은 건너뛴다.
        Keyboard kb = Keyboard.current;
        if (kb != null && Time.frameCount > openedFrame &&
            (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
        {
            Close();
        }
    }

    private void Refresh()
    {
        if (!IsOpen || shop == null || inventory == null) return;

        if (titleText != null) titleText.text = shop.ShopName;
        if (goldText != null) goldText.text = $"Gold  {inventory.Gold}";

        ItemData[] stock = shop.Stock ?? new ItemData[0];
        for (int i = 0; i < rows.Length; i++)
        {
            if (i >= stock.Length || stock[i] == null)
            {
                rows[i].Hide();
                continue;
            }

            ItemData item = stock[i];
            bool owned = item.IsEquipment && inventory.CountOf(item) > 0;
            string detail = item.Describe() + (item.IsEquipment ? "" : $"   (have {inventory.CountOf(item)})");

            rows[i].Set(item, item.displayName, detail,
                owned ? "Owned" : $"Buy {item.price}G",
                !owned && inventory.Gold >= item.price,
                () => inventory.TryBuy(item));
        }
    }
}
