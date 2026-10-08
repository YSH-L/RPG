using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 시작 화면 순서. 제목 → 캐릭터 선택 → 조작법을 Space로 넘기고, 마지막 Space에서
/// <see cref="GameManager.StartGame"/>을 부른 뒤 검은 화면을 서서히 걷어낸다.
/// GameManager의 startOnSpace는 꺼 둬야 한다 — 켜져 있으면 첫 Space에 바로 시작해 버린다.
/// 페이지들은 Ready 패널 안에 있어서, 시작하면 UIManager가 패널째 끈다.
/// </summary>
public class TitleIntro : MonoBehaviour
{
    [Tooltip("Space를 누를 때마다 다음으로 넘어가는 페이지. 마지막 페이지에서 Space를 누르면 게임이 시작된다.")]
    [SerializeField] private GameObject[] pages;
    [Tooltip("Ready 동안 게임 화면을 가리는 검은 판. 시작하면 투명해진다.")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField, Min(0f)] private float fadeDuration = 5f;
    [SerializeField] private SoundSet sounds;

    private int page;
    private int pageShownFrame = -1;

    private void Start()
    {
        SetOverlayAlpha(1f);
        ShowPage(0);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Ready) return;
        Keyboard keyboard = Keyboard.current;
        // 같은 프레임에 두 페이지를 건너뛰지 않도록, 페이지가 바뀐 프레임의 입력은 무시한다.
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame || Time.frameCount == pageShownFrame) return;

        if (page < pages.Length - 1)
        {
            if (sounds != null) SoundSet.Play(sounds.menuConfirm);
            ShowPage(page + 1);
            return;
        }

        if (sounds != null) SoundSet.Play(sounds.gameStart);
        GameManager.Instance.StartGame();
        StartCoroutine(FadeIn());
    }

    private void ShowPage(int index)
    {
        page = index;
        pageShownFrame = Time.frameCount;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == index);
        }
    }

    /// <summary>완전히 검은 화면에서 fadeDuration초 동안 화면에 보이는 밝기가 일정하게 늘어난다.</summary>
    private IEnumerator FadeIn()
    {
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            SetOverlayAlpha(OverlayAlphaFor(t / fadeDuration));
            yield return null;
        }
        SetOverlayAlpha(0f);
    }

    /// <summary>
    /// 화면 밝기(0~1)를 검은 판의 알파로 바꾼다.
    /// Linear 색 공간에서는 섞기가 선형 값으로 일어나서, 알파를 일정하게 줄이면 앞쪽에서 확 밝아진다
    /// (알파 0.5면 이미 약 73% 밝기). 그래서 보이는 밝기를 선형 값으로 바꿔서 알파를 정한다.
    /// </summary>
    private static float OverlayAlphaFor(float brightness)
    {
        brightness = Mathf.Clamp01(brightness);
        float linear = QualitySettings.activeColorSpace == ColorSpace.Linear ? Mathf.GammaToLinearSpace(brightness) : brightness;
        return 1f - linear;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (fadeOverlay == null) return;
        Color color = fadeOverlay.color;
        color.a = alpha;
        fadeOverlay.color = color;
        // 다 걷히면 꺼서 그리지 않는다.
        fadeOverlay.gameObject.SetActive(alpha > 0f);
    }
}
