using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 하단의 물약 퀵슬롯(1, 2)과 골드 표시. PlayerInventory.OnChanged로 다시 그린다. HUD_Panel 안에 둔다.
/// </summary>
public class QuickSlotHUD : MonoBehaviour
{
    [Serializable]
    public class SlotView
    {
        public Image icon;
        public TMP_Text keyText;
        public TMP_Text countText;
    }

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private SlotView[] views;
    [SerializeField] private TMP_Text goldText;

    private void OnEnable()
    {
        if (inventory == null) return;
        inventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (goldText != null) goldText.text = $"Gold {inventory.Gold}";

        for (int i = 0; i < views.Length; i++)
        {
            SlotView view = views[i];
            ItemData item = inventory.GetQuickSlot(i);
            int count = inventory.CountOf(item);

            if (view.keyText != null) view.keyText.text = (i + 1).ToString();
            if (view.countText != null) view.countText.text = item != null ? count.ToString() : "";
            if (view.icon != null)
            {
                view.icon.sprite = item != null ? item.icon : null;
                view.icon.enabled = view.icon.sprite != null;
                // 다 떨어지면 흐리게
                view.icon.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
        }
    }
}
