using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 상점 창. ↑↓ 선택, Z 구매, X 닫기. 열려 있는 동안은 캐릭터 조작이 멈춘다(<see cref="InputLock"/>).
/// 줄(아이콘·글자)은 인스펙터에서 배치하고 여기서는 내용만 채운다.
/// </summary>
public class ShopWindow : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text infoText;

    [Header("줄 — 같은 순서로 채운다")]
    [SerializeField] private Image[] rowBackgrounds;
    [SerializeField] private Image[] rowIcons;
    [SerializeField] private TMP_Text[] rowTexts;

    [SerializeField] private Color normalRow = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Color selectedRow = new Color(1f, 0.8f, 0.3f, 0.45f);

    public bool IsOpen => root != null && root.activeSelf;

    private ItemData[] items;
    private PlayerStats customer;
    private int selected;
    private int openedFrame;

    private void Awake()
    {
        InputLock.Locked = false;
        if (root != null) root.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
        InputLock.Locked = false;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.GameOver || state == GameState.Ready) Close();
    }

    public void Open(string title, ItemData[] stock, PlayerStats stats)
    {
        items = stock ?? new ItemData[0];
        customer = stats;
        selected = 0;
        openedFrame = Time.frameCount;
        titleText.text = title;
        infoText.text = "UP/DOWN select   Z buy   X close";
        root.SetActive(true);
        InputLock.Locked = true;
        customer.OnChanged += Refresh;
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        root.SetActive(false);
        InputLock.Locked = false;
        if (customer != null) customer.OnChanged -= Refresh;
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.xKey.wasPressedThisFrame)
        {
            Close();
            return;
        }
        if (items.Length == 0) return;

        if (keyboard.upArrowKey.wasPressedThisFrame) Select(selected - 1);
        if (keyboard.downArrowKey.wasPressedThisFrame) Select(selected + 1);
        if (keyboard.zKey.wasPressedThisFrame) BuySelected();
    }

    private void Select(int index)
    {
        selected = (index + items.Length) % items.Length;
        Refresh();
    }

    private void BuySelected()
    {
        ItemData item = items[selected];
        switch (customer.Buy(item))
        {
            case BuyResult.Bought: infoText.text = $"Bought {item.itemName}!"; break;
            case BuyResult.NotEnoughGold: infoText.text = "Not enough gold."; break;
            case BuyResult.LevelTooLow: infoText.text = $"Requires Lv.{item.requiredLevel}."; break;
            case BuyResult.AlreadyBetter: infoText.text = "You already have equal or better gear."; break;
        }
        Refresh();
    }

    private void Refresh()
    {
        if (customer == null) return;
        goldText.text = $"Gold  {customer.Gold}";

        for (int i = 0; i < rowTexts.Length; i++)
        {
            bool used = i < items.Length && items[i] != null;
            rowBackgrounds[i].gameObject.SetActive(used);
            if (!used) continue;

            ItemData item = items[i];
            rowBackgrounds[i].color = i == selected ? selectedRow : normalRow;
            rowIcons[i].sprite = item.icon;
            rowIcons[i].enabled = item.icon != null;

            string owned = "";
            if (item == customer.Weapon || item == customer.Armor) owned = "  <color=#7CFC00>[EQUIPPED]</color>";
            else if (item.kind == ItemKind.Potion) owned = $"  <color=#aaaaaa>x{CountPotion(item)}</color>";

            string levelColor = customer.Level >= item.requiredLevel ? "#cccccc" : "#ff6060";
            rowTexts[i].text =
                $"{item.itemName}  <color=#ffd54a>{item.price}G</color>  <size=80%>{item.Describe()}  <color={levelColor}>Lv.{item.requiredLevel}</color></size>{owned}";
        }
    }

    private int CountPotion(ItemData item)
    {
        for (int slot = 0; slot < customer.PotionSlotCount; slot++)
        {
            if (customer.GetPotion(slot) == item) return customer.GetPotionCount(slot);
        }
        return 0;
    }
}
