using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 저장할 때 화면 구석에 잠깐 "Saved"를 띄운다. F5로 직접 저장했으면 "Game saved"로 조금 더 오래.
/// HUD_Panel 안에 둔다.
/// </summary>
public class SaveIndicatorHUD : MonoBehaviour
{
    [SerializeField] private PlayerSave save;
    [SerializeField] private TMP_Text text;
    [SerializeField, Min(0.1f)] private float duration = 1.2f;

    private void OnEnable()
    {
        if (text != null) text.text = "";
        if (save != null) save.OnSaved += HandleSaved;
    }

    private void OnDisable()
    {
        if (save != null) save.OnSaved -= HandleSaved;
    }

    private void HandleSaved(bool manual)
    {
        StopAllCoroutines();
        StartCoroutine(Show(manual ? "Game saved" : "Saved", manual ? duration * 1.5f : duration));
    }

    private IEnumerator Show(string message, float seconds)
    {
        text.text = message;
        text.alpha = 1f;
        // 일시정지 중에도 사라지도록 실제 시간으로 잰다.
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            text.alpha = Mathf.Clamp01((seconds - elapsed) / (seconds * 0.4f));
            yield return null;
        }
        text.text = "";
    }
}
