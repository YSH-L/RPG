using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 골드·보유 아이템·착용 장비. 아이템 정의는 ItemData 에셋이고, 여기는 "몇 개 가졌나"만 든다.
/// 퀵슬롯 1, 2 키로 물약을 바로 마신다. Player 오브젝트에 PlayerController와 같이 붙인다.
/// 창(InventoryWindow·ShopWindow·QuickSlotHUD)은 OnChanged를 구독해서 다시 그린다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [Tooltip("순서대로 1, 2 키에 연결할 물약.")]
    [SerializeField] private ItemData[] quickSlots = new ItemData[2];
    [SerializeField, Min(0)] private int startingGold = 0;

    public int Gold { get; private set; }
    public ItemData EquippedWeapon { get; private set; }
    public ItemData EquippedArmor { get; private set; }

    /// <summary>골드·아이템·장비 중 무엇이든 바뀌면. 구독했으면 OnDisable에서 해제한다.</summary>
    public event Action OnChanged;

    private readonly Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();
    /// <summary>창에 보여줄 순서 (얻은 순서).</summary>
    private readonly List<ItemData> order = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => order;
    public int QuickSlotCount => quickSlots.Length;
    public ItemData GetQuickSlot(int index) => index < quickSlots.Length ? quickSlots[index] : null;
    public int CountOf(ItemData item) => item != null && counts.TryGetValue(item, out int n) ? n : 0;
    public bool IsEquipped(ItemData item) => item != null && (item == EquippedWeapon || item == EquippedArmor);

    private void Awake()
    {
        Gold = startingGold;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || player.IsDead) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) UseQuickSlot(0);
        else if (kb.digit2Key.wasPressedThisFrame) UseQuickSlot(1);
    }

    public void UseQuickSlot(int index)
    {
        ItemData item = GetQuickSlot(index);
        if (item == null) return;

        if (CountOf(item) <= 0)
        {
            ShowMessage($"No {item.displayName} left");
            return;
        }
        Use(item);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        OnChanged?.Invoke();
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return;

        if (!counts.ContainsKey(item))
        {
            counts[item] = 0;
            order.Add(item);
        }
        counts[item] += amount;
        OnChanged?.Invoke();
    }

    /// <summary>골드를 내고 산다. 장비는 하나만 가질 수 있다.</summary>
    public bool TryBuy(ItemData item)
    {
        if (item == null) return false;

        if (item.IsEquipment && CountOf(item) > 0)
        {
            ShowMessage("Already owned");
            return false;
        }
        if (Gold < item.price)
        {
            ShowMessage("Not enough gold");
            return false;
        }

        Gold -= item.price;
        AddItem(item);   // OnChanged는 여기서 한 번 나간다
        ShowMessage($"Bought {item.displayName}");
        return true;
    }

    /// <summary>물약이면 마시고, 장비면 착용한다.</summary>
    public void Use(ItemData item)
    {
        if (item == null || CountOf(item) <= 0) return;

        if (item.IsEquipment)
        {
            Equip(item);
            return;
        }

        if (!player.Heal(item.healAmount))
        {
            ShowMessage("HP is already full");
            return;
        }

        counts[item]--;
        if (counts[item] <= 0)
        {
            counts.Remove(item);
            order.Remove(item);
        }
        OnChanged?.Invoke();
    }

    /// <summary>같은 칸의 장비는 바꿔 낀다. 가진 장비만 낄 수 있다.</summary>
    public void Equip(ItemData item)
    {
        if (item == null || !item.IsEquipment || CountOf(item) <= 0) return;

        if (item.type == ItemType.Weapon) EquippedWeapon = item;
        else EquippedArmor = item;

        ApplyEquipment();
        OnChanged?.Invoke();
    }

    private void ApplyEquipment()
    {
        int attack = 0, defense = 0, maxHP = 0;
        foreach (ItemData e in new[] { EquippedWeapon, EquippedArmor })
        {
            if (e == null) continue;
            attack += e.attackBonus;
            defense += e.defenseBonus;
            maxHP += e.maxHPBonus;
        }
        player.SetEquipmentBonus(attack, defense, maxHP);
    }

    // UI 폰트(LiberationSans)에 한글 글리프가 없어서 화면 문구는 영어로 쓴다.
    private static void ShowMessage(string text)
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(text);
    }
}
