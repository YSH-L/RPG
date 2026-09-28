using UnityEngine;

/// <summary>
/// 배경음악 고르기. 지금 있는 맵(MapArea.Bgm)의 음악을 Playing이 되면 틀고, 맵을 옮기면 바꾼다.
/// 플레이어가 죽으면 패배, 마지막 보스를 잡으면 승리 징글을 울린다. 재생은 SoundManager가 한다.
/// Managers 오브젝트에 붙인다.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    [Tooltip("새 게임 시작 맵. 저장을 불러오면 그 맵으로 바뀐다.")]
    [SerializeField] private MapArea startArea;
    [SerializeField] private AudioClip victoryJingle;
    [SerializeField] private AudioClip defeatJingle;

    private MapArea currentArea;
    private AudioClip playing;
    private bool subscribedToGame;

    private void OnEnable()
    {
        MapArea.OnEntered += HandleAreaEntered;
        CombatEvents.OnDied += HandleDied;
    }

    private void Start()
    {
        // 저장 불러오기(PlayerSave.Start)가 먼저 돌았으면 currentArea가 이미 정해져 있다.
        if (currentArea == null) currentArea = startArea;

        // GameManager는 다른 오브젝트라 OnEnable 시점엔 아직 없을 수 있어서 Start에서 구독한다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            subscribedToGame = true;
            if (GameManager.Instance.IsPlaying) PlayAreaMusic();
        }
    }

    private void OnDisable()
    {
        MapArea.OnEntered -= HandleAreaEntered;
        CombatEvents.OnDied -= HandleDied;
        if (subscribedToGame && GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
        subscribedToGame = false;
    }

    private void HandleAreaEntered(MapArea area)
    {
        currentArea = area;
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying) PlayAreaMusic();
    }

    private void HandleStateChanged(GameState state)
    {
        // 일시정지에서 돌아올 때도 Playing이 오지만, 같은 곡이면 처음부터 다시 틀지 않는다.
        if (state == GameState.Playing) PlayAreaMusic();
    }

    private void HandleDied(GameObject target)
    {
        PlayerController player = PlayerController.Instance;
        if (player != null && target == player.gameObject)
        {
            EndWith(defeatJingle);
            return;
        }

        if (target.TryGetComponent<BossController>(out var boss) && boss.EndsGame) EndWith(victoryJingle);
    }

    private void PlayAreaMusic()
    {
        AudioClip clip = currentArea != null ? currentArea.Bgm : null;
        if (clip == playing || SoundManager.Instance == null) return;

        playing = clip;
        if (clip != null) SoundManager.Instance.PlayBGM(clip);
        else SoundManager.Instance.StopBGM();
    }

    private void EndWith(AudioClip jingle)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.StopBGM();
        playing = null;
        Sfx.Play(jingle);
    }
}
