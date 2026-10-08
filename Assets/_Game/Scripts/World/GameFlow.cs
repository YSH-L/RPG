using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("승리했을 때만 제목에 쓰는 글씨체. 비우면 GAME OVER와 같은 글씨체.")]
    [SerializeField] private TMP_FontAsset victoryTitleFont;
    [Tooltip("마지막 보스를 잡고 화면이 하얘지기 시작할 때까지(초).")]
    [SerializeField, Min(0f)] private float victoryDelay = 0f;

    [Header("승리 연출 — 화면이 서서히 하얘진 뒤 결과 화면")]
    [Tooltip("게임 화면을 덮는 흰 판. 결과 화면 글자보다 뒤에 있어야 한다.")]
    [SerializeField] private Image whiteOverlay;
    [SerializeField, Min(0f)] private float whiteFadeDuration = 5f;
    [Tooltip("흰 배경에서도 보이도록 승리했을 때만 결과 화면 글자색을 바꾼다.")]
    [SerializeField] private Color victoryTitleColor = new Color(0.85f, 0.6f, 0.1f);
    [SerializeField] private Color victoryTextColor = new Color(0.15f, 0.12f, 0.1f);

    private readonly HashSet<BossStatsData> defeated = new HashSet<BossStatsData>();

    /// <summary>보스를 전부 잡아서 끝났는지. 게임오버 화면에서 승리 곡을 틀지 정할 때 본다.</summary>
    public bool Won { get; private set; }

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

        // 연출 동안은 움직이지도, 맞지도 않는다. 조용한 가운데 화면만 하얘진다.
        player.Untouchable = true;
        InputLock.Locked = true;
        if (SoundManager.Instance != null) SoundManager.Instance.StopBGM();

        // Time.deltaTime이라 일시정지하면 연출도 멈춘다.
        for (float t = 0f; t < whiteFadeDuration; t += Time.deltaTime)
        {
            SetWhite(t / whiteFadeDuration);
            yield return null;
        }
        SetWhite(1f);

        if (gameOverTitle != null)
        {
            gameOverTitle.text = victoryTitle;
            if (victoryTitleFont != null)
            {
                gameOverTitle.font = victoryTitleFont;
                gameOverTitle.fontSharedMaterial = victoryTitleFont.material;
            }
            if (whiteOverlay != null)
            {
                foreach (TMP_Text text in gameOverTitle.transform.parent.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.color = text == gameOverTitle ? victoryTitleColor : victoryTextColor;
                }
            }
        }
        Won = true;
        InputLock.Locked = false;
        GameManager.Instance.TriggerGameOver();
    }

    /// <summary>
    /// 화면이 하얀 정도(0~1)를 흰 판의 알파로 바꾼다. 시작 화면이 밝아질 때와 같은 이유로
    /// Linear 색 공간에서는 보이는 밝기가 고르게 오르도록 알파를 선형 값으로 바꿔 준다.
    /// </summary>
    private void SetWhite(float whiteness)
    {
        if (whiteOverlay == null) return;
        whiteness = Mathf.Clamp01(whiteness);
        float alpha = QualitySettings.activeColorSpace == ColorSpace.Linear ? Mathf.GammaToLinearSpace(whiteness) : whiteness;
        Color color = whiteOverlay.color;
        color.a = alpha;
        whiteOverlay.color = color;
        whiteOverlay.gameObject.SetActive(alpha > 0f);
    }
}
