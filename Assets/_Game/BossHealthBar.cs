using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단의 보스 체력바. 보스맵(MapArea.Boss가 있는 맵)에 들어오면 뜨고, 보스가 죽거나 맵을 나가면 사라진다.
/// 보스를 직접 구독하지 않고 CombatEvents로만 체력 변화를 받는다.
/// HUD_Panel 안에 두면 GameOver에서 UIManager가 패널째로 숨겨준다.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Tooltip("켜고 끌 묶음(이름 + 바). 이 스크립트가 붙은 오브젝트 자신이 아니라 자식이어야 구독이 유지된다.")]
    [SerializeField] private GameObject content;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("보스가 죽은 뒤 바를 치우기까지의 시간(초).")]
    [SerializeField, Min(0f)] private float hideDelay = 1.5f;

    private BossController boss;

    private void Awake()
    {
        if (content != null) content.SetActive(false);
    }

    private void OnEnable()
    {
        MapArea.OnEntered += HandleAreaEntered;
        CombatEvents.OnDamaged += HandleDamaged;
        CombatEvents.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        MapArea.OnEntered -= HandleAreaEntered;
        CombatEvents.OnDamaged -= HandleDamaged;
        CombatEvents.OnDied -= HandleDied;
    }

    private void HandleAreaEntered(MapArea area)
    {
        StopAllCoroutines();

        BossController next = area.Boss;
        if (next == null || !next.gameObject.activeInHierarchy || next.CurrentHP <= 0)
        {
            Hide();
            return;
        }

        boss = next;
        if (content != null) content.SetActive(true);
        Refresh();
    }

    private void HandleDamaged(GameObject target, int amount)
    {
        if (boss != null && target == boss.gameObject) Refresh();
    }

    private void HandleDied(GameObject target)
    {
        if (boss == null || target != boss.gameObject) return;

        Refresh();
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        Hide();
    }

    private void Hide()
    {
        boss = null;
        if (content != null) content.SetActive(false);
    }

    private void Refresh()
    {
        if (boss == null) return;

        int current = Mathf.Max(0, boss.CurrentHP);
        int max = Mathf.Max(1, boss.MaxHP);

        if (hpSlider != null) hpSlider.value = (float)current / max;
        if (nameText != null) nameText.text = $"{boss.DisplayName}   {current} / {max}";
    }
}
