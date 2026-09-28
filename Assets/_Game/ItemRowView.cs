using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리·상점 창의 한 줄: 아이콘 | 이름 | 설명 | 버튼. 창이 Set/Hide로 채운다.
/// </summary>
public class ItemRowView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text buttonText;

    public void Set(ItemData item, string title, string detail, string buttonLabel, bool interactable, Action onClick)
    {
        gameObject.SetActive(true);

        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = icon.sprite != null;
        }
        if (nameText != null) nameText.text = title;
        if (detailText != null) detailText.text = detail;
        if (buttonText != null) buttonText.text = buttonLabel;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = interactable;
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }

    public void Hide()
    {
        if (button != null) button.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }
}
