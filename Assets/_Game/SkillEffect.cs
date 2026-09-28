using UnityEngine;

/// <summary>
/// 한 번 재생하고 풀로 돌아가는 스킬 이펙트. 프레임은 SpriteAnimator의 <b>Death</b> 칸에 넣는다 —
/// Death는 끝나면 마지막 프레임에서 멈추고 Idle로 돌아가지 않아서, 한 번만 도는 이펙트에 그대로 맞는다.
/// </summary>
[RequireComponent(typeof(SpriteAnimator))]
public class SkillEffect : MonoBehaviour
{
    [SerializeField] private SpriteAnimator animator;

    private ObjectPool pool;

    /// <summary>풀에서 꺼낸 직후 부른다. facing이 음수면 좌우를 뒤집는다.</summary>
    public void Init(ObjectPool sourcePool, float facing)
    {
        pool = sourcePool;
        animator.SetFacing(facing);
    }

    private void OnEnable()
    {
        animator.OnClipFinished += HandleClipFinished;
        animator.Play(AnimState.Death, force: true);
    }

    private void OnDisable()
    {
        animator.OnClipFinished -= HandleClipFinished;
    }

    private void HandleClipFinished(AnimState state)
    {
        if (pool != null) pool.Release(gameObject);
        else gameObject.SetActive(false);
    }
}
