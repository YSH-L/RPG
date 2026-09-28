using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 하단의 스킬 칸 (Z / X / C). 잠김이면 해금 레벨을, 쿨타임 중이면 남은 초와 어두운 덮개를 보여준다.
/// 쿨타임은 매 프레임 줄어드는 값이라 이벤트 대신 Update에서 읽는다.
/// HUD_Panel 안에 둔다.
/// </summary>
public class SkillHUD : MonoBehaviour
{
    [Serializable]
    public class SlotView
    {
        public Image background;
        [Tooltip("쿨타임 덮개. 아래에서부터 남은 비율만큼 차 있다.")]
        public RectTransform cooldownCover;
        public TMP_Text keyText;
        public TMP_Text labelText;
    }

    [SerializeField] private PlayerSkills skills;
    [SerializeField] private SlotView[] views;
    [SerializeField] private Color readyColor = new Color(0.15f, 0.2f, 0.35f, 0.9f);
    [SerializeField] private Color lockedColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    private void Update()
    {
        if (skills == null || views == null) return;

        for (int i = 0; i < views.Length; i++)
        {
            SlotView view = views[i];
            SkillData skill = i < skills.SlotCount ? skills.GetSkill(i) : null;

            if (skill == null)
            {
                SetLabel(view, "");
                SetCover(view, 0f);
                continue;
            }

            if (view.keyText != null) view.keyText.text = PlayerSkills.KeyLabel(i);

            if (!skills.IsUnlocked(i))
            {
                if (view.background != null) view.background.color = lockedColor;
                SetLabel(view, $"Lv.{skill.unlockLevel}");
                SetCover(view, 0f);
                continue;
            }

            if (view.background != null) view.background.color = readyColor;

            float remaining = skills.GetCooldownRemaining(i);
            if (remaining > 0f)
            {
                SetLabel(view, remaining.ToString("0.0"));
                SetCover(view, skill.cooldown > 0f ? remaining / skill.cooldown : 0f);
            }
            else
            {
                SetLabel(view, skill.displayName);
                SetCover(view, 0f);
            }
        }
    }

    private static void SetLabel(SlotView view, string text)
    {
        if (view.labelText != null && view.labelText.text != text) view.labelText.text = text;
    }

    private static void SetCover(SlotView view, float fraction)
    {
        if (view.cooldownCover == null) return;
        view.cooldownCover.anchorMax = new Vector2(1f, Mathf.Clamp01(fraction));
    }
}
