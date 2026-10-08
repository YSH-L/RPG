using UnityEngine;

/// <summary>
/// BGM과 이벤트로 알 수 있는 효과음. 시작 화면에서는 제목 곡, 게임 중에는 지금 공간의 곡,
/// 끝나면 승리 곡이나 게임오버 소리를 낸다. 피격·처치·레벨업·포탈 소리도 여기서 낸다.
/// 공격·스킬·상점처럼 누른 순간에 나는 소리는 각 스크립트가 <see cref="SoundSet.Play"/>로 직접 낸다.
/// </summary>
public class GameAudio : MonoBehaviour
{
    [SerializeField] private SoundSet sounds;
    [SerializeField] private AreaManager areaManager;
    [SerializeField] private PlayerStats player;
    [SerializeField] private GameFlow gameFlow;

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
        if (areaManager != null) areaManager.OnAreaChanged += HandleAreaChanged;
        if (player != null) player.OnLevelUp += HandleLevelUp;
        CombatEvents.OnDamaged += HandleDamaged;
        CombatEvents.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
        if (areaManager != null) areaManager.OnAreaChanged -= HandleAreaChanged;
        if (player != null) player.OnLevelUp -= HandleLevelUp;
        CombatEvents.OnDamaged -= HandleDamaged;
        CombatEvents.OnDied -= HandleDied;
    }

    private void Start()
    {
        if (GameManager.Instance != null) HandleStateChanged(GameManager.Instance.State);
    }

    private void HandleStateChanged(GameState state)
    {
        if (SoundManager.Instance == null || sounds == null) return;

        switch (state)
        {
            case GameState.Ready:
                SoundManager.Instance.PlayBGM(sounds.titleBgm);
                break;
            case GameState.Playing:
                PlayAreaBgm(areaManager != null ? areaManager.Current : null);
                break;
            case GameState.GameOver:
                if (gameFlow != null && gameFlow.Won)
                {
                    SoundManager.Instance.PlayBGM(sounds.victoryBgm);
                }
                else
                {
                    SoundManager.Instance.StopBGM();
                    SoundSet.Play(sounds.gameOver);
                }
                break;
        }
    }

    private void HandleAreaChanged(Area area)
    {
        // 시작할 때 마을에 놓이는 것도 공간 변경으로 들어온다. 시작 화면 동안은 제목 곡을 그대로 둔다.
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        SoundSet.Play(sounds != null ? sounds.portal : null);
        PlayAreaBgm(area);
    }

    private void PlayAreaBgm(Area area)
    {
        if (area != null && area.Bgm != null && SoundManager.Instance != null) SoundManager.Instance.PlayBGM(area.Bgm);
    }

    private void HandleLevelUp(int level)
    {
        if (sounds != null) SoundSet.Play(sounds.levelUp);
    }

    private void HandleDamaged(GameObject target, int amount)
    {
        if (sounds == null || target == null) return;
        bool isPlayer = player != null && target == player.gameObject;
        SoundSet.Play(isPlayer ? sounds.playerHurt : sounds.enemyHit, isPlayer ? 1f : 0.7f);
    }

    private void HandleDied(GameObject target)
    {
        if (sounds == null || target == null) return;
        if (player != null && target == player.gameObject) return;   // 쓰러지는 소리는 게임오버 때 낸다
        SoundSet.Play(target.TryGetComponent(out BossEnemy _) ? sounds.bossDeath : sounds.enemyDeath);
    }
}
