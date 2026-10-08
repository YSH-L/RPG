using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에서 쓰는 소리 목록. 에셋 하나(Data/Sounds.asset)에 모아 두고 각 스크립트가 참조한다.
/// 소리를 바꾸려면 이 에셋의 칸에 다른 클립을 끌어다 놓으면 된다. 비워 두면 그 소리는 나지 않는다.
/// 구역 BGM은 여기가 아니라 각 <see cref="Area"/>의 BGM 칸에 있다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Sound Set")]
public class SoundSet : ScriptableObject
{
    [Header("BGM")]
    public AudioClip titleBgm;
    public AudioClip victoryBgm;
    [Tooltip("쓰러졌을 때 한 번 울린다. BGM은 멈춘다.")]
    public AudioClip gameOver;

    [Header("플레이어")]
    public AudioClip swordSwing;
    public AudioClip arrowShot;
    [Tooltip("A 스킬 — Swordsman 범위베기")]
    public AudioClip skillSlash;
    [Tooltip("A 스킬 — Archer 화살 난사")]
    public AudioClip skillVolley;
    [Tooltip("S 스킬 — Swordsman 방어")]
    public AudioClip guard;
    [Tooltip("S 스킬 — Archer 회피")]
    public AudioClip dodge;
    public AudioClip jump;
    public AudioClip playerHurt;
    public AudioClip potion;
    public AudioClip levelUp;

    [Header("전투")]
    public AudioClip enemyHit;
    public AudioClip enemyDeath;
    public AudioClip bossDeath;

    [Header("월드")]
    public AudioClip portal;

    [Header("상점")]
    public AudioClip shopOpen;
    public AudioClip shopClose;
    public AudioClip shopMove;
    public AudioClip shopBuy;
    public AudioClip shopFail;

    [Header("시작 화면")]
    public AudioClip menuMove;
    public AudioClip menuConfirm;
    public AudioClip gameStart;

    private static readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();

    /// <summary>
    /// 효과음 재생. 같은 소리가 아주 짧은 간격으로 겹치면(화살 다섯 발이 한꺼번에 맞을 때 등) 한 번만 낸다.
    /// </summary>
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || SoundManager.Instance == null) return;

        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(clip, out float last) && now - last < 0.05f && now >= last) return;
        lastPlayed[clip] = now;
        SoundManager.Instance.PlaySFX(clip, volume);
    }
}
