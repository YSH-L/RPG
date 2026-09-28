using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>캐릭터가 지금 하고 있는 동작. 프레임 묶음 하나에 하나씩 대응한다.</summary>
public enum AnimState
{
    Idle,
    Walk,
    Jump,
    Attack,
    Hit,
    Death
}

/// <summary>
/// 스프라이트를 차례로 갈아끼워 동작을 보여준다. Animator Controller 없이
/// <see cref="SpriteRenderer"/>만 바꾸므로, 인스펙터에서 프레임을 넣고 초당 프레임 수를 바로 조절할 수 있다.
/// <code>
/// animator.Play(AnimState.Walk);      // 매 프레임 불러도 된다. 같은 동작이면 다시 시작하지 않는다
/// animator.Play(AnimState.Attack);    // 끝까지 재생되고 저절로 Idle로 돌아온다
/// animator.SetFacing(moveX);          // 좌우 뒤집기
/// </code>
/// </summary>
/// <remarks>
/// <b>왜 _Core에 있는가.</b> 애니메이션은 틀려도 에러가 안 난다 — 그냥 어색하게 보인다.
/// 특히 <c>Update()</c>에서 이동 입력에 따라 매 프레임 <c>Play(Walk)</c>를 부르면
/// 공격 동작이 1프레임마다 끊겨서 <b>영원히 보이지 않는다.</b> 원인 추정이 거의 불가능한 종류라
/// 여기서 우선순위로 막아둔다.
/// <list type="bullet">
/// <item>반복하지 않는 동작(Attack/Hit/Death)이 재생 중이면 <b>더 낮은 우선순위의 요청은 무시된다.</b></item>
/// <item>우선순위는 Death &gt; Hit &gt; Attack &gt; Jump &gt; Idle·Walk. 그래서 맞으면 공격이 끊긴다.</item>
/// <item>반복 동작(Idle/Walk)끼리는 그냥 바뀐다. 그쪽은 _Game이 매 프레임 결정한다.</item>
/// <item>Death는 마지막 프레임에서 멈추고 <b>그 뒤로는 <c>force: true</c>가 아닌 요청을 받지 않는다.</b>
/// 풀에서 다시 꺼내 쓸 때는 <c>Play(AnimState.Idle, force: true)</c>로 되살린다.</item>
/// </list>
/// <para>
/// 일시정지는 따로 처리하지 않는다. <c>Time.deltaTime</c>을 쓰므로
/// <c>GameManager.PauseGame()</c>의 <c>timeScale = 0</c>에서 저절로 멈춘다.
/// </para>
/// <para>
/// 유니티 표준 Animator Controller를 쓰고 싶으면 그래도 된다. 이 컴포넌트를 떼고 쓰면 된다 —
/// 다만 이쪽이 프레임 배열만 넣으면 끝나서 빠르다.
/// </para>
/// </remarks>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    /// <summary>동작 하나에 해당하는 프레임 묶음. 인스펙터에서 채운다.</summary>
    [Serializable]
    public class Clip
    {
        [Tooltip("이 프레임 묶음이 어떤 동작인지.")]
        public AnimState state;

        [Tooltip("순서대로 보여줄 스프라이트.")]
        public Sprite[] frames;

        [Tooltip("초당 프레임 수. 걷기는 8~12, 공격은 12~16쯤이 무난하다.")]
        [Min(0.1f)] public float framesPerSecond = 10f;

        [Tooltip("끝나면 처음으로 돌아갈지. Idle·Walk는 켜고 Attack·Hit·Death는 끈다.")]
        public bool loop = true;
    }

    [Header("동작")]
    [Tooltip("동작별 프레임 묶음. 없는 동작은 Play해도 무시된다.")]
    [SerializeField] private Clip[] clips;

    [Tooltip("시작할 때 재생할 동작.")]
    [SerializeField] private AnimState defaultState = AnimState.Idle;

    [Tooltip("반복하지 않는 동작이 끝나면 돌아갈 동작.")]
    [SerializeField] private AnimState returnState = AnimState.Idle;

    [Header("방향")]
    [Tooltip("원본 스프라이트가 오른쪽을 보고 있으면 켠다. 왼쪽을 보고 있으면 끈다.")]
    [SerializeField] private bool spriteFacesRight = true;

    /// <summary>지금 재생 중인 동작.</summary>
    public AnimState Current { get; private set; }

    /// <summary>반복하지 않는 동작이 아직 재생 중인지. true면 낮은 우선순위 요청이 막힌다.</summary>
    public bool IsBusy => currentClip != null && !currentClip.loop && !finished;

    /// <summary>Death를 끝내고 잠긴 상태인지.</summary>
    public bool IsDeadLocked => locked;

    /// <summary>
    /// 반복하지 않는 동작이 끝났을 때 발사된다. 공격 판정을 동작 끝에 맞출 때 쓴다.
    /// 구독은 OnEnable, 해제는 <b>OnDisable</b>에서 한다.
    /// </summary>
    public event Action<AnimState> OnClipFinished;

    private readonly Dictionary<AnimState, Clip> table = new Dictionary<AnimState, Clip>();
    private readonly HashSet<AnimState> warned = new HashSet<AnimState>();

    private SpriteRenderer spriteRenderer;
    private Clip currentClip;
    private float elapsed;
    private int frameIndex;
    private bool finished;
    private bool locked;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (clips != null)
        {
            foreach (Clip clip in clips)
            {
                if (clip == null) continue;
                // 같은 동작이 두 번 들어 있으면 먼저 넣은 것을 쓴다. 조용히 덮어쓰면 찾기 어렵다.
                if (table.ContainsKey(clip.state))
                {
                    Debug.LogWarning($"[SpriteAnimator] '{name}'에 {clip.state} 프레임 묶음이 둘 이상 있습니다. 첫 번째만 씁니다.", this);
                    continue;
                }
                table.Add(clip.state, clip);
            }
        }

        Play(defaultState, force: true);
    }

    /// <summary>
    /// 동작을 재생한다. 매 프레임 불러도 된다 — 같은 동작이면 처음부터 다시 시작하지 않는다.
    /// </summary>
    /// <param name="state">재생할 동작.</param>
    /// <param name="force">
    /// 우선순위와 중복 검사를 무시하고 처음부터 재생한다. Death로 잠긴 것도 이때만 풀린다.
    /// ObjectPool에서 꺼내 되살릴 때 <c>Play(AnimState.Idle, force: true)</c>로 부른다.
    /// </param>
    public void Play(AnimState state, bool force = false)
    {
        // Death는 마지막 프레임에서 잠긴다. force로만 풀리게 해서, 풀에서 재사용되는 몬스터가
        // 죽은 프레임에 굳은 채 되살아나는 것을 막는다. force 없이는 예전처럼 아무것도 받지 않는다.
        if (locked)
        {
            if (!force) return;
            locked = false;
        }

        if (!force)
        {
            if (state == Current && !finished) return;
            if (IsBusy && Priority(state) <= Priority(Current)) return;
        }

        if (!table.TryGetValue(state, out Clip clip) || clip.frames == null || clip.frames.Length == 0)
        {
            if (warned.Add(state))
            {
                Debug.LogWarning($"[SpriteAnimator] '{name}'에 {state} 프레임이 없습니다. 인스펙터의 Clips에 추가하세요.", this);
            }
            return;
        }

        Current = state;
        currentClip = clip;
        frameIndex = 0;
        elapsed = 0f;
        finished = false;
        ApplyFrame();
    }

    /// <summary>
    /// 좌우를 바라보게 한다. 이동 입력값을 그대로 넘기면 된다.
    /// 0을 넘기면 지금 방향을 유지하므로, 멈췄을 때 정면으로 돌아가지 않는다.
    /// </summary>
    public void SetFacing(float directionX)
    {
        if (Mathf.Approximately(directionX, 0f)) return;

        bool faceRight = directionX > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !faceRight : faceRight;
    }

    private void Update()
    {
        if (finished || currentClip == null) return;

        elapsed += Time.deltaTime;
        float interval = 1f / Mathf.Max(0.1f, currentClip.framesPerSecond);

        // 프레임이 크게 밀렸을 때도 남은 시간이 쌓이지 않도록 따라잡는다.
        while (elapsed >= interval)
        {
            elapsed -= interval;

            int next = frameIndex + 1;
            if (next >= currentClip.frames.Length)
            {
                if (!currentClip.loop)
                {
                    frameIndex = currentClip.frames.Length - 1;
                    ApplyFrame();
                    HandleFinished();
                    return;
                }
                next = 0;
            }

            frameIndex = next;
        }

        ApplyFrame();
    }

    private void HandleFinished()
    {
        finished = true;
        AnimState justFinished = Current;

        if (justFinished == AnimState.Death)
        {
            // 죽은 뒤에 Idle로 돌아가면 시체가 숨을 쉰다. 마지막 프레임에서 멈추고 잠근다.
            locked = true;
            OnClipFinished?.Invoke(justFinished);
            return;
        }

        OnClipFinished?.Invoke(justFinished);

        // 구독자가 이미 다른 동작을 시작했으면 그것을 존중한다.
        if (Current == justFinished) Play(returnState);
    }

    private void ApplyFrame()
    {
        if (currentClip == null || currentClip.frames == null) return;
        if (frameIndex < 0 || frameIndex >= currentClip.frames.Length) return;

        spriteRenderer.sprite = currentClip.frames[frameIndex];
    }

    private static int Priority(AnimState state)
    {
        switch (state)
        {
            case AnimState.Death: return 4;
            case AnimState.Hit: return 3;
            case AnimState.Attack: return 2;
            case AnimState.Jump: return 1;
            default: return 0;
        }
    }
}
