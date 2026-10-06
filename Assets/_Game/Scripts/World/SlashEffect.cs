using UnityEngine;

/// <summary>
/// 범위베기 때 한 번 재생하고 꺼지는 베기 이펙트. 프레임은 SpriteAnimator의 Attack 묶음(반복 끔)에 넣는다.
/// 원소마다 프리팹이 하나씩 있고, <see cref="SlashVisual"/>이 처음 쓸 때 만들어 두고 재사용한다.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
public class SlashEffect : MonoBehaviour
{
    private SpriteAnimator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<SpriteAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() { animator.OnClipFinished += HandleFinished; }
    private void OnDisable() { animator.OnClipFinished -= HandleFinished; }

    public void Play(Vector3 position, bool flipX, float scale)
    {
        transform.position = position;
        transform.localScale = new Vector3(scale, scale, 1f);
        spriteRenderer.flipX = flipX;
        gameObject.SetActive(true);
        animator.Play(AnimState.Attack, force: true);
    }

    private void HandleFinished(AnimState state)
    {
        if (state == AnimState.Attack) gameObject.SetActive(false);
    }
}
