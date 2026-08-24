/// <summary>
/// 피해를 받을 수 있는 것. 플레이어·몬스터·보스·부술 수 있는 상자가 모두 이것을 구현한다.
/// <code>
/// if (other.TryGetComponent&lt;IDamageable&gt;(out var target)) target.TakeDamage(damage);
/// </code>
/// </summary>
/// <remarks>
/// 공격하는 쪽이 <c>GetComponent&lt;Monster&gt;()</c> 같은 <b>구체 타입</b>을 찾으면
/// 몬스터 종류를 하나 늘리는 순간 조용히 안 맞는다. 컴파일도 되고 실행도 되는데
/// 새 몬스터만 안 맞는 형태라 원인을 찾기 어렵다. 그래서 때리는 쪽은 이 인터페이스만 본다.
/// <para>
/// 체력을 이 안에 두지 않는 것이 중요하다. <b>정의</b>(종류별 최대 체력)는 ScriptableObject 에셋에,
/// <b>상태</b>(지금 남은 체력)는 인스턴스에 둔다. 둘을 한군데로 뭉치면
/// 몬스터 열 마리가 체력을 공유하는 형태로 터진다.
/// </para>
/// </remarks>
public interface IDamageable
{
    /// <summary>
    /// 피해를 준다. 이미 죽은 대상에게 또 들어올 수 있으므로,
    /// 구현하는 쪽에서 죽은 뒤에는 무시하도록 막는다 (안 막으면 경험치가 여러 번 들어온다).
    /// </summary>
    /// <param name="amount">깎을 체력. 0 이하는 무시한다.</param>
    void TakeDamage(int amount);
}
