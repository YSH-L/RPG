using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 시작 화면(Ready)에서 ←→로 캐릭터를 고른다. 고른 캐릭터는 재시작해도 기억한다.
/// 시작(Space)은 <see cref="GameManager"/>가 그대로 처리한다 — 여기서는 화살표만 읽는다.
/// </summary>
public class CharacterSelect : MonoBehaviour
{
    [Serializable]
    public class Profile
    {
        public string displayName = "Swordsman";
        public PlayerStatsData stats;
        [Tooltip("플레이어에 붙어 있는 이 캐릭터의 SpriteAnimator. 고르지 않은 쪽은 꺼진다.")]
        public SpriteAnimator animator;
    }

    [SerializeField] private PlayerController player;
    [SerializeField] private Profile[] profiles;
    [Tooltip("시작 화면 안내 글. 고른 캐릭터를 맨 위 줄에 보여 준다.")]
    [SerializeField] private TMP_Text guideText;

    /// <summary>씬을 다시 불러도 남도록 static. RestartGame 뒤에도 같은 캐릭터로 시작한다.</summary>
    private static int lastChoice;

    private int current = -1;
    private string baseGuide;

    private void Start()
    {
        if (guideText != null) baseGuide = guideText.text;
        Select(Mathf.Clamp(lastChoice, 0, profiles.Length - 1));
    }

    private void Update()
    {
        if (profiles.Length < 2 || GameManager.Instance == null || GameManager.Instance.State != GameState.Ready) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.leftArrowKey.wasPressedThisFrame) Select((current - 1 + profiles.Length) % profiles.Length);
        if (keyboard.rightArrowKey.wasPressedThisFrame) Select((current + 1) % profiles.Length);
    }

    private void Select(int index)
    {
        if (index == current || index < 0 || index >= profiles.Length) return;
        current = index;
        lastChoice = index;

        // 고르지 않은 애니메이터는 모두 끈다. 켜진 채로 두면 서로 스프라이트를 덮어써서 깜빡인다.
        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i].animator != null && i != index) profiles[i].animator.enabled = false;
        }
        player.SetCharacter(profiles[index].stats, profiles[index].animator);

        if (guideText != null && baseGuide != null)
        {
            guideText.text = $"<  {profiles[index].displayName}  >\n<size=60%>LEFT/RIGHT choose character</size>\n{baseGuide}";
        }
    }
}
