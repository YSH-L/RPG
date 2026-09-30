using TMPro;
using UnityEngine;

/// <summary>보스가 나와 있는 동안 화면 위쪽에 체력바를 띄운다.</summary>
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform fill;
    [SerializeField] private TMP_Text nameText;

    private void Update()
    {
        BossEnemy boss = BossEnemy.Current;
        bool show = boss != null && !boss.IsDead;
        if (root.activeSelf != show) root.SetActive(show);
        if (!show) return;

        nameText.text = $"{boss.Data.displayName}   {boss.CurrentHP} / {boss.Data.maxHP}";
        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01((float)boss.CurrentHP / boss.Data.maxHP);
        fill.anchorMax = max;
    }
}
