using UnityEngine;

/// <summary>
/// SoundManager.PlaySFX 축약. 클립이 비어 있거나 SoundManager가 없으면 조용히 넘어간다 —
/// 소리는 빠져도 게임은 돌아야 해서다. 재생 자체는 _Core의 SoundManager가 한다.
/// </summary>
public static class Sfx
{
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null && SoundManager.Instance != null) SoundManager.Instance.PlaySFX(clip, volume);
    }
}
