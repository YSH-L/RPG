using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨·경험치·체력 표시. UIManager는 점수/패널만 다루므로, 이 RPG 고유 정보는 따로 구독해서 그린다.
/// HUD_Panel 안에 배치하고 필드에 텍스트/슬라이더를 연결한다.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    private void OnEnable()
    {
        if (player == null) return;

        player.OnLevelChanged += HandleLevelChanged;
        player.OnExpChanged += HandleExpChanged;
        player.OnHPChanged += HandleHPChanged;

        HandleLevelChanged(player.CurrentLevel);
        HandleExpChanged(player.CurrentExp, player.ExpToNextLevel);
        HandleHPChanged(player.CurrentHP, player.MaxHP);
    }

    private void OnDisable()
    {
        if (player == null) return;

        player.OnLevelChanged -= HandleLevelChanged;
        player.OnExpChanged -= HandleExpChanged;
        player.OnHPChanged -= HandleHPChanged;
    }

    private void HandleLevelChanged(int level)
    {
        if (levelText != null) levelText.text = $"Lv.{level}";
    }

    private void HandleExpChanged(int current, int toNext)
    {
        if (expSlider == null) return;
        expSlider.value = toNext <= 0 ? 1f : (float)current / toNext;
    }

    private void HandleHPChanged(int current, int max)
    {
        if (hpSlider != null) hpSlider.value = max <= 0 ? 0f : (float)current / max;
        if (hpText != null) hpText.text = $"{current} / {max}";
    }
}
