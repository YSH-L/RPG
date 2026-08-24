using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 진행 상태. <see cref="GameManager.State"/>의 값이고,
/// <see cref="GameManager.OnStateChanged"/>로 전달된다.
/// </summary>
public enum GameState
{
    /// <summary>시작 대기. 아직 아무것도 움직이지 않는다.</summary>
    Ready,

    /// <summary>플레이 중. _Game 스크립트는 이 상태에서만 동작해야 한다.</summary>
    Playing,

    /// <summary>일시정지. Time.timeScale == 0.</summary>
    Paused,

    /// <summary>게임오버. timeScale은 1로 유지된다.</summary>
    GameOver
}

/// <summary>
/// 게임 상태(Ready/Playing/Paused/GameOver)의 단일 출처.
/// 상태를 바꾸는 것은 이 클래스만 하고, 나머지는 <see cref="OnStateChanged"/>를 구독한다.
/// </summary>
/// <remarks>
/// Awake가 다른 컴포넌트보다 먼저 돌아야 하므로 실행 순서를 앞으로 당겨둔다.
/// 덕분에 다른 스크립트는 OnEnable에서 Instance를 안전하게 참조할 수 있다.
/// </remarks>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Ready;

    /// <summary>State == GameState.Playing 축약.</summary>
    public bool IsPlaying => State == GameState.Playing;

    public event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameManager] 씬에 이미 있습니다. '{name}'를 제거합니다.", this);
            Destroy(this);
            return;
        }

        Instance = this;

        // 이전 플레이에서 0으로 남은 timeScale을 물려받지 않도록 한다.
        Time.timeScale = 1f;
        State = GameState.Ready;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 상태 전환 키. Space(시작) / Esc(일시정지·해제) / R(재시작).
    /// 이 프로젝트는 Input System 패키지 전용 모드라 레거시 Input.GetKey는 실행 시 예외가 난다.
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        switch (State)
        {
            case GameState.Ready:
                if (keyboard.spaceKey.wasPressedThisFrame) StartGame();
                break;

            case GameState.Playing:
                if (keyboard.escapeKey.wasPressedThisFrame) PauseGame();
                break;

            case GameState.Paused:
                if (keyboard.escapeKey.wasPressedThisFrame) ResumeGame();
                break;

            case GameState.GameOver:
                if (keyboard.rKey.wasPressedThisFrame) RestartGame();
                break;
        }
    }

    public void StartGame()
    {
        if (State == GameState.Playing) return;

        Time.timeScale = 1f;
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
        SetState(GameState.Playing);
    }

    /// <summary>
    /// 게임오버. 멱등이므로 여러 곳에서 중복 호출해도 안전하다.
    /// timeScale은 건드리지 않는다 — 연출과 사운드가 계속 돌아가야 하기 때문이다.
    /// </summary>
    public void TriggerGameOver()
    {
        if (State == GameState.GameOver) return;
        SetState(GameState.GameOver);
    }

    public void PauseGame()
    {
        if (State != GameState.Playing) return;

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused) return;

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    /// <summary>
    /// 현재 씬을 다시 불러 Ready 상태로 되돌린다.
    /// 씬을 통째로 다시 만들기 때문에 _Game 쪽에 위치 초기화 코드를 따로 쓸 필요가 없다.
    /// </summary>
    public void RestartGame()
    {
        // 일시정지 중에 눌렸을 수 있으므로 씬 로드 전에 되돌린다. timeScale은 씬을 넘어 유지된다.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetState(GameState next)
    {
        if (State == next) return;

        State = next;
        OnStateChanged?.Invoke(State);
    }
}
