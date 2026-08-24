using System;
using UnityEngine;

/// <summary>
/// 전투에서 벌어진 일을 알리는 전역 채널. 매니저가 아니다 — 상태를 하나도 들고 있지 않고,
/// 발행자와 구독자를 서로 모르게 이어주기만 한다.
/// <code>
/// // 맞은 쪽 (_Game)
/// CombatEvents.RaiseDied(gameObject);
///
/// // 반응하는 쪽 (_Game) — 서로를 참조하지 않는다
/// void OnEnable()  { CombatEvents.OnDied += HandleDied; }
/// void OnDisable() { CombatEvents.OnDied -= HandleDied; }   // 반드시
/// </code>
/// </summary>
/// <remarks>
/// 킬 카운트·퀘스트 진행도·경험치·데미지 숫자 표시처럼 <b>누가 죽였는지 모르는 채로 반응해야 하는 것</b>에 쓴다.
/// 몬스터가 UI를 직접 참조하게 만들면 몬스터 프리팹이 UI를 알아야 해서 종류를 늘릴 때마다 연결이 늘어난다.
/// <para>
/// <b>구독 해제를 빠뜨리면 이 클래스는 실제로 샌다.</b> <c>static</c>이라
/// <c>GameManager.RestartGame()</c>이 씬을 다시 불러도 이 이벤트는 초기화되지 않는다.
/// 파괴된 오브젝트의 메서드가 목록에 남아서, 재시작 뒤 몬스터를 한 마리 잡으면 킬 카운트가 두 번 오르거나
/// <c>MissingReferenceException</c>이 뜬다. 컴포넌트에 붙은 이벤트(<c>OnStateChanged</c> 등)는
/// 발행자도 씬과 함께 새로 만들어져서 이 증상이 안 나오지만, 여기는 다르다.
/// </para>
/// </remarks>
public static class CombatEvents
{
    /// <summary>피해를 입었을 때. (맞은 대상, 실제로 깎인 양)</summary>
    public static event Action<GameObject, int> OnDamaged;

    /// <summary>죽었을 때. 플레이어가 죽은 것인지 몬스터가 죽은 것인지는 구독하는 쪽에서 판별한다.</summary>
    public static event Action<GameObject> OnDied;

    /// <summary>피해를 입혔을 때 부른다. <see cref="IDamageable.TakeDamage"/> 구현 안에서 호출하면 된다.</summary>
    public static void RaiseDamaged(GameObject target, int amount)
    {
        OnDamaged?.Invoke(target, amount);
    }

    /// <summary>죽었을 때 부른다. 같은 대상에 대해 두 번 부르지 않도록 부르는 쪽에서 막는다.</summary>
    public static void RaiseDied(GameObject target)
    {
        OnDied?.Invoke(target);
    }
}
