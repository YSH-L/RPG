using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// I 키로 여닫는 인벤토리 창. 가진 아이템을 한 줄씩 보여주고, 물약은 [Use], 장비는 [Equip]으로 쓴다.
/// 창이 열려 있어도 게임은 계속 돈다 (메이플식). HUD_Panel 안에 둔다.
/// </summary>
public class InventoryWindow : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerController player;
    [Tooltip("켜고 끌 창 본체. 이 스크립트가 붙은 오브젝트 자신이 아니라 자식이어야 키 입력을 계속 받는다.")]
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text statsText;
    [Tooltip("미리 만들어 둔 줄. 아이템 종류가 이보다 많으면 뒤는 안 보인다.")]
    [SerializeField] private ItemRowView[] rows;

    public bool IsOpen => content != null && content.activeSelf;

    private void Awake()
    {
        if (content != null) content.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += Refresh;
        if (player != null) player.OnHPChanged += HandleHPChanged;
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
        if (player != null) player.OnHPChanged -= HandleHPChanged;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.iKey.wasPressedThisFrame) SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        if (content == null) return;
        content.SetActive(open);
        if (open) Refresh();
    }

    private void HandleHPChanged(int current, int max)
    {
        // 레벨업·장비로 스탯 줄이 바뀐다.
        if (IsOpen) Refresh();
    }

    private void Refresh()
    {
        if (!IsOpen || inventory == null) return;

        if (goldText != null) goldText.text = $"Gold  {inventory.Gold}";
        if (statsText != null && player != null)
        {
            string weapon = inventory.EquippedWeapon != null ? inventory.EquippedWeapon.displayName : "-";
            string armor = inventory.EquippedArmor != null ? inventory.EquippedArmor.displayName : "-";
            statsText.text = $"ATK {player.AttackPower}   DEF {player.Defense}   HP {player.CurrentHP}/{player.MaxHP}\n" +
                             $"Weapon: {weapon}   Armor: {armor}";
        }

        for (int i = 0; i < rows.Length; i++)
        {
            if (i >= inventory.Items.Count)
            {
                rows[i].Hide();
                continue;
            }

            ItemData item = inventory.Items[i];
            if (item.IsEquipment)
            {
                bool equipped = inventory.IsEquipped(item);
                rows[i].Set(item, item.displayName, item.Describe(), equipped ? "Equipped" : "Equip",
                    !equipped, () => inventory.Equip(item));
            }
            else
            {
                rows[i].Set(item, $"{item.displayName}  x{inventory.CountOf(item)}", item.Describe(), "Use",
                    true, () => inventory.Use(item));
            }
        }
    }
}
