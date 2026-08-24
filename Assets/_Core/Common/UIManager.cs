using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 패널 전환과 점수 표시. GameManager의 상태 변화와 ScoreManager의 점수 변화를
/// 구독해서 <b>자동으로</b> 갱신한다.
/// 그래서 _Game 쪽에서 이 클래스를 부를 일은 <see cref="ShowMessage"/> 하나뿐이다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("패널 — 상태에 따라 자동으로 켜고 끈다")]
    [Tooltip("Ready 상태에 보인다.")]
    [SerializeField] private GameObject readyPanel;

    [Tooltip("Playing / Paused 상태에 보인다. 점수 숫자가 들어 있다.")]
    [SerializeField] private GameObject hudPanel;

    [Tooltip("GameOver 상태에 보인다.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Paused 상태에 보인다.")]
    [SerializeField] private GameObject pausePanel;

    [Header("텍스트")]
    [Tooltip("HUD의 현재 점수.")]
    [SerializeField] private TMP_Text scoreText;

    [Tooltip("게임오버 화면의 이번 점수.")]
    [SerializeField] private TMP_Text finalScoreText;

    [Tooltip("게임오버 화면의 최고점수.")]
    [SerializeField] private TMP_Text bestScoreText;

    [Tooltip("ShowMessage()가 띄우는 텍스트. 평소에는 꺼져 있다.")]
    [SerializeField] private TMP_Text messageText;

    private Coroutine messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[UIManager] 씬에 이미 있습니다. '{name}'를 제거합니다.", this);
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        // GameManager는 DefaultExecutionOrder가 앞이라 이 시점에 Instance가 준비돼 있다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += ApplyState;
            ApplyState(GameManager.Instance.State);
        }
        else
        {
            Debug.LogWarning("[UIManager] 씬에 GameManager가 없어 패널 전환이 동작하지 않습니다.", this);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += ApplyScore;
            ApplyScore(ScoreManager.Instance.Score);
        }

        HideMessage();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= ApplyState;
        if (ScoreManager.Instance != null) ScoreManager.Instance.OnScoreChanged -= ApplyScore;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 화면 가운데에 잠깐 문구를 띄운다. 같은 호출이 겹치면 뒤에 온 것이 이긴다.
    /// </summary>
    /// <param name="duration">표시 시간(초). 일시정지 중에도 흐른다.</param>
    public void ShowMessage(string text, float duration = 1.5f)
    {
        if (messageText == null)
        {
            Debug.LogWarning($"[UIManager] messageText가 비어 있어 '{text}'를 띄울 수 없습니다.", this);
            return;
        }

        if (messageRoutine != null) StopCoroutine(messageRoutine);

        messageText.text = text;
        messageText.gameObject.SetActive(true);
        messageRoutine = StartCoroutine(HideMessageAfter(duration));
    }

    private IEnumerator HideMessageAfter(float duration)
    {
        // Time.timeScale이 0인 일시정지 중에도 사라지도록 실시간으로 센다.
        yield return new WaitForSecondsRealtime(duration);

        HideMessage();
        messageRoutine = null;
    }

    private void HideMessage()
    {
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    private void ApplyState(GameState state)
    {
        SetActive(readyPanel, state == GameState.Ready);
        SetActive(hudPanel, state == GameState.Playing || state == GameState.Paused);
        SetActive(gameOverPanel, state == GameState.GameOver);
        SetActive(pausePanel, state == GameState.Paused);

        if (state == GameState.GameOver) ApplyResult();
    }

    private void ApplyScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }

    /// <summary>게임오버 화면의 숫자는 상태가 바뀌는 순간 ScoreManager에서 가져온다.</summary>
    private void ApplyResult()
    {
        if (ScoreManager.Instance == null) return;

        if (finalScoreText != null) finalScoreText.text = $"Score {ScoreManager.Instance.Score}";
        if (bestScoreText != null) bestScoreText.text = $"Best {ScoreManager.Instance.BestScore}";
    }

    private static void SetActive(GameObject panel, bool value)
    {
        if (panel != null && panel.activeSelf != value) panel.SetActive(value);
    }
}
