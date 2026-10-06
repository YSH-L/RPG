using TMPro;
using UnityEngine;

/// <summary>
/// 레벨·HP·EXP·골드·장비·포션·지금 있는 공간을 보여준다. 값이 바뀔 때만 갱신한다.
/// 막대는 채움 오브젝트의 오른쪽 앵커를 비율만큼 옮겨서 그린다.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;

    [SerializeField] private TMP_Text levelText;
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private RectTransform expFill;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text statText;
    [SerializeField] private TMP_Text potionText;
    [SerializeField] private TMP_Text areaText;

    private PlayerController controller;
    private string potionLine = "";
    private string skillLine = "";

    private void OnEnable()
    {
        stats.OnChanged += Refresh;
        if (AreaManager.Instance != null)
        {
            AreaManager.Instance.OnAreaChanged += HandleAreaChanged;
            if (AreaManager.Instance.Current != null) HandleAreaChanged(AreaManager.Instance.Current);
        }
        Refresh();
    }

    private void OnDisable()
    {
        stats.OnChanged -= Refresh;
        if (AreaManager.Instance != null) AreaManager.Instance.OnAreaChanged -= HandleAreaChanged;
    }

    private void Start()
    {
        // AreaManager가 Start에서 첫 공간을 정하므로, 그보다 먼저 켜졌으면 여기서 다시 구독한다.
        if (AreaManager.Instance != null)
        {
            AreaManager.Instance.OnAreaChanged -= HandleAreaChanged;
            AreaManager.Instance.OnAreaChanged += HandleAreaChanged;
            if (AreaManager.Instance.Current != null) HandleAreaChanged(AreaManager.Instance.Current);
        }
        Refresh();
    }

    private void HandleAreaChanged(Area area)
    {
        if (areaText != null) areaText.text = area.DisplayName;
    }

    private void Refresh()
    {
        levelText.text = $"Lv.{stats.Level}";

        SetFill(hpFill, (float)stats.HP / Mathf.Max(1, stats.MaxHP));
        hpText.text = $"HP {stats.HP} / {stats.MaxHP}";

        if (stats.IsMaxLevel)
        {
            SetFill(expFill, 1f);
            expText.text = "EXP MAX";
        }
        else
        {
            SetFill(expFill, (float)stats.Exp / Mathf.Max(1, stats.ExpToNext));
            expText.text = $"EXP {stats.Exp} / {stats.ExpToNext}";
        }

        goldText.text = $"Gold {stats.Gold}";

        string weapon = stats.Weapon != null ? stats.Weapon.itemName : "-";
        string armor = stats.Armor != null ? stats.Armor.itemName : "-";
        statText.text = $"ATK {stats.Attack}   DEF {stats.Defense}\n<size=80%>{weapon} / {armor}</size>";

        string potions = "";
        for (int slot = 0; slot < stats.PotionSlotCount; slot++)
        {
            ItemData potion = stats.GetPotion(slot);
            if (potion == null) continue;
            if (potions.Length > 0) potions += "    ";
            potions += $"[{slot + 1}] {potion.itemName} x{stats.GetPotionCount(slot)}";
        }
        potionLine = potions;
        potionText.text = potionLine + skillLine;
    }

    /// <summary>스킬 쿨타임은 시간에 따라 바뀌므로 매 프레임 확인하되, 글자가 달라졌을 때만 바꾼다.</summary>
    private void Update()
    {
        if (controller == null) controller = stats.GetComponent<PlayerController>();
        if (controller == null) return;

        string line = SkillStatus(SkillType.Slash) + SkillStatus(SkillType.Guard);
        if (line == skillLine) return;
        skillLine = line;
        potionText.text = potionLine + skillLine;
    }

    private string SkillStatus(SkillType skill)
    {
        if (!stats.HasSkill(skill)) return "";
        float left = controller.SkillCooldownLeft(skill);
        string state = left > 0f ? $"<color=#aaaaaa>{left:0.0}s</color>" : "<color=#7CFC00>READY</color>";
        string name = controller.SkillName(skill);
        ElementData element = stats.Element;
        if (skill == SkillType.Slash && element != null)
        {
            name = $"<color=#{ColorUtility.ToHtmlStringRGB(element.color)}>{element.displayName}</color> {name}";
        }
        return $"    [{ItemData.SkillKey(skill)}] {name} {state}";
    }

    private static void SetFill(RectTransform fill, float ratio)
    {
        if (fill == null) return;
        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01(ratio);
        fill.anchorMax = max;
    }
}
