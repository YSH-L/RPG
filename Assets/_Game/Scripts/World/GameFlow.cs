using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 게임의 끝을 정한다. 보스를 전부 잡으면 승리, 레벨업·보스 처치는 메시지로 알린다.
/// 패널 전환은 하지 않는다 — <see cref="GameManager.TriggerGameOver"/>만 부르면 UIManager가 알아서 바꾼다.
/// </summary>
public class GameFlow : MonoBehaviour
{
    [SerializeField] private PlayerStats player;
    [Tooltip("전부 잡으면 게임이 끝나는 보스들.")]
    [SerializeField] private BossStatsData[] bossesToWin;
    [Tooltip("게임오버 화면의 제목. 승리하면 글자만 바꾼다.")]
    [SerializeField] private TMP_Text gameOverTitle;
    [SerializeField] private string victoryTitle = "VICTORY!";
    [Tooltip("마지막 보스를 잡고 결과 화면이 뜨기까지(초).")]
    [SerializeField, Min(0f)] private float victoryDelay = 2f;

    private readonly HashSet<BossStatsData> defeated = new HashSet<BossStatsData>();

    private void OnEnable()
    {
        CombatEvents.OnDied += HandleDied;
        player.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        CombatEvents.OnDied -= HandleDied;
        player.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int level)
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"LEVEL UP!  Lv.{level}", 2f);
    }

    private void HandleDied(GameObject target)
    {
        if (target == null || !target.TryGetComponent(out BossEnemy boss)) return;

        defeated.Add(boss.BossData);
        int remaining = 0;
        foreach (BossStatsData needed in bossesToWin)
        {
            if (!defeated.Contains(needed)) remaining++;
        }

        if (remaining > 0)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage($"{boss.Data.displayName} defeated!  {remaining} boss left", 3f);
            }
            return;
        }

        StartCoroutine(Victory());
    }

    /// <summary>
    /// 마지막 보스가 쓰러지는 것을 보여준 뒤에 끝낸다.
    /// 같은 프레임에 끝내면 보스 처치 점수가 결과 화면에 반영되기 전에 패널이 바뀐다.
    /// </summary>
    private IEnumerator Victory()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("All bosses defeated!", 2f);
        yield return new WaitForSeconds(victoryDelay);
        if (GameManager.Instance.State == GameState.GameOver) yield break;   // 그 사이에 쓰러졌으면 패배가 우선

        if (gameOverTitle != null) gameOverTitle.text = victoryTitle;
        GameManager.Instance.TriggerGameOver();
    }
}
