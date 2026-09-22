using System;
using UnityEngine;

/// <summary>
/// 점수와 최고점수. 최고점수는 PlayerPrefs에 저장된다.
/// 점수 UI는 <see cref="OnScoreChanged"/>를 구독하는 UIManager가 갱신하므로,
/// _Game 쪽에서는 <see cref="AddScore"/>만 부르면 된다.
/// </summary>
[DefaultExecutionOrder(-90)]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public int BestScore { get; private set; }

    public event Action<int> OnScoreChanged;

    /// <summary>
    /// Application.productName을 포함한다. productName을 바꾸면 저장 위치가 달라진다.
    /// </summary>
    private static string BestScoreKey => $"{Application.productName}.BestScore";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[ScoreManager] 씬에 이미 있습니다. '{name}'를 제거합니다.", this);
            Destroy(this);
            return;
        }

        Instance = this;
        Score = 0;
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddScore(int amount = 1)
    {
        SetScore(Score + amount);
    }

    public void SetScore(int value)
    {
        if (Score == value) return;

        Score = value;

        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.Save();
        }

        OnScoreChanged?.Invoke(Score);
    }

    public void ResetScore()
    {
        SetScore(0);
    }
}
