using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범위베기의 원소 연출. 누르는 순간 원소 칼이 플레이어 주위를 한 바퀴 돌고,
/// 판정이 들어가는 순간 같은 원소의 베기 이펙트가 앞뒤로 터진다. 게임 규칙은 모른다.
/// </summary>
public class SlashVisual : MonoBehaviour
{
    [Tooltip("원소 칼을 그릴 자식 SpriteRenderer.")]
    [SerializeField] private SpriteRenderer blade;
    [Tooltip("칼이 도는 중심 높이(발 기준).")]
    [SerializeField] private float centerHeight = 1f;
    [Tooltip("중심에서 칼 가운데까지. 맨 아래에서 칼끝이 땅에 닿을 만큼(중심 높이 - 반지름 ≈ 칼 길이의 절반).")]
    [SerializeField] private float bladeRadius = 0.5f;
    [SerializeField] private float bladeScale = 0.6f;
    [Tooltip("칼이 한 바퀴 도는 시간(초). Atk2 모션 길이에 맞춘다.")]
    [SerializeField, Min(0.05f)] private float spinDuration = 0.5f;

    [Tooltip("베기 이펙트가 플레이어에서 떨어진 거리.")]
    [SerializeField] private float effectDistance = 1.2f;
    [SerializeField] private float effectHeight = 0.6f;
    [SerializeField] private float effectScale = 1.6f;

    private readonly Dictionary<GameObject, SlashEffect[]> effects = new Dictionary<GameObject, SlashEffect[]>();
    private Coroutine spin;

    private void Awake()
    {
        if (blade != null) blade.enabled = false;
    }

    private void OnDisable()
    {
        spin = null;
        if (blade != null) blade.enabled = false;
    }

    /// <summary>범위베기를 누른 순간. 원소 칼을 돌린다.</summary>
    public void PlayBlade(ElementData element, float facing)
    {
        if (element == null || element.blade == null || blade == null) return;
        if (spin != null) StopCoroutine(spin);
        spin = StartCoroutine(Spin(element.blade, facing));
    }

    /// <summary>판정이 들어가는 순간. 앞뒤로 베기 이펙트를 터뜨린다.</summary>
    public void PlayEffect(ElementData element, float facing)
    {
        if (element == null || element.slashEffect == null) return;

        SlashEffect[] pair = GetEffects(element.slashEffect);
        Vector3 center = transform.position + Vector3.up * effectHeight;
        float side = facing >= 0f ? 1f : -1f;
        pair[0].Play(center + Vector3.right * (effectDistance * side), side < 0f, effectScale);
        pair[1].Play(center - Vector3.right * (effectDistance * side), side > 0f, effectScale);
    }

    /// <summary>원소마다 앞·뒤 두 개를 한 번만 만들어 두고 계속 다시 쓴다.</summary>
    private SlashEffect[] GetEffects(GameObject prefab)
    {
        if (effects.TryGetValue(prefab, out SlashEffect[] pair)) return pair;

        pair = new SlashEffect[2];
        for (int i = 0; i < pair.Length; i++)
        {
            GameObject go = Instantiate(prefab);
            go.SetActive(false);
            pair[i] = go.GetComponent<SlashEffect>();
        }
        effects[prefab] = pair;
        return pair;
    }

    /// <summary>등 뒤에서 시작해 머리 위를 지나 앞으로 내려치며 한 바퀴. 판정 순간(절반)에 칼이 정면에 온다.</summary>
    private IEnumerator Spin(Sprite sprite, float facing)
    {
        float side = facing >= 0f ? 1f : -1f;
        blade.sprite = sprite;
        blade.enabled = true;
        blade.transform.localScale = new Vector3(bladeScale, bladeScale, 1f);

        for (float t = 0f; t < spinDuration; t += Time.deltaTime)
        {
            // 오른쪽을 볼 때 180°(등 뒤) → 0°(정면) → -180°. 왼쪽이면 좌우를 뒤집는다.
            float angle = 180f - 360f * (t / spinDuration);
            float rad = angle * Mathf.Deg2Rad;
            blade.transform.localPosition = new Vector3(Mathf.Cos(rad) * bladeRadius * side, centerHeight + Mathf.Sin(rad) * bladeRadius, 0f);
            // 칼끝이 바깥을 향하게. 스프라이트는 칼끝이 위쪽이다.
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, side > 0f ? angle - 90f : 90f - angle);
            yield return null;
        }

        blade.enabled = false;
        spin = null;
    }
}
