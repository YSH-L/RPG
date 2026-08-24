using UnityEngine;

/// <summary>
/// 효과음과 BGM 재생. AudioSource는 Awake에서 자식으로 직접 만들기 때문에
/// 인스펙터에서 붙여줄 것이 없다.
/// 재생은 문자열 키가 아니라 AudioClip을 직접 받는다 — _Game에서 [SerializeField]로 클립을 물려 쓴다.
/// </summary>
[DefaultExecutionOrder(-90)]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("볼륨 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource bgmSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SoundManager] 씬에 이미 있습니다. '{name}'를 제거합니다.", this);
            Destroy(this);
            return;
        }

        Instance = this;

        sfxSource = CreateSource("SFX Source", loop: false);
        bgmSource = CreateSource("BGM Source", loop: true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        var host = new GameObject(sourceName);
        host.transform.SetParent(transform, worldPositionStays: false);

        AudioSource source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        // 2D 사운드. 카메라와의 거리에 따라 볼륨이 변하면 안 된다.
        source.spatialBlend = 0f;
        // 일시정지(timeScale = 0)와 무관하게 재생되도록 한다.
        source.ignoreListenerPause = true;

        return source;
    }

    /// <param name="volume">이 재생 한 번에만 적용되는 배율. 최종 볼륨 = volume * SFX 볼륨.</param>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume);
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        // 같은 곡을 다시 요청하면 처음부터 되감지 않고 그대로 둔다.
        if (bgmSource.clip == clip && bgmSource.isPlaying && bgmSource.loop == loop) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        bgmSource.volume = bgmVolume;
    }
}
