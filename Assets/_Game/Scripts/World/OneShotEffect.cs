using UnityEngine;

/// <summary>
/// 한 번 재생하고 풀로 돌아가는 이펙트. 프레임은 SpriteAnimator의 Attack 묶음(반복 끔)에 넣는다.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
public class OneShotEffect : MonoBehaviour
{
    private SpriteAnimator animator;
    private ObjectPool pool;

    private void Awake()
    {
        animator = GetComponent<SpriteAnimator>();
    }

    private void OnEnable() { animator.OnClipFinished += HandleFinished; }
    private void OnDisable() { animator.OnClipFinished -= HandleFinished; }

    public void Play(ObjectPool owner, Area area)
    {
        pool = owner;
        if (TryGetComponent(out WrapGhost ghost)) ghost.Area = area;
        GetComponent<SpriteRenderer>().flipX = Random.value < 0.5f;
        animator.Play(AnimState.Attack, force: true);
    }

    private void HandleFinished(AnimState state)
    {
        if (state != AnimState.Attack) return;
        if (pool != null) pool.Release(gameObject);
        else gameObject.SetActive(false);
    }
}
