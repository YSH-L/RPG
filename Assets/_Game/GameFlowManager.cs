using System.Collections;
using UnityEngine;

/// <summary>
/// CombatEvents.OnDied 하나로 게임의 승패를 결정한다.
/// 몬스터가 죽으면 점수·경험치를 주고, 보스가 죽으면 승리, 플레이어가 죽으면 패배로 GameOver 처리한다.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField, Min(0f)] private float resultDelay = 1.2f;

    private void OnEnable()
    {
        CombatEvents.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        CombatEvents.OnDied -= HandleDied;
    }

    private void HandleDied(GameObject target)
    {
        if (player != null && target == player.gameObject)
        {
            StartCoroutine(EndGameAfterDelay());
            return;
        }

        if (target.TryGetComponent<BossController>(out _))
        {
            StartCoroutine(VictorySequence());
            return;
        }

        if (target.TryGetComponent<EnemyBase>(out var enemy))
        {
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(enemy.ExpReward);
            if (player != null) player.GainExp(enemy.ExpReward);
        }
    }

    private IEnumerator VictorySequence()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("승리!", resultDelay);
        yield return new WaitForSecondsRealtime(resultDelay);
        if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
    }

    private IEnumerator EndGameAfterDelay()
    {
        yield return new WaitForSecondsRealtime(resultDelay);
        if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
    }
}
