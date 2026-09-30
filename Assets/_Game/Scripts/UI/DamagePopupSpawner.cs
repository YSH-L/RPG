using UnityEngine;

/// <summary>누가 맞았는지 몰라도 되게 <see cref="CombatEvents.OnDamaged"/>를 구독해서 숫자를 띄운다.</summary>
public class DamagePopupSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPool pool;
    [SerializeField] private Color enemyHitColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color playerHitColor = new Color(0.8f, 0.4f, 1f);

    private void OnEnable() { CombatEvents.OnDamaged += HandleDamaged; }
    private void OnDisable() { CombatEvents.OnDamaged -= HandleDamaged; }

    private void HandleDamaged(GameObject target, int amount)
    {
        if (target == null) return;

        Vector3 position = target.transform.position + Vector3.up;
        if (target.TryGetComponent(out Collider2D body)) position = new Vector3(body.bounds.center.x, body.bounds.max.y + 0.2f, 0f);
        position.x += Random.Range(-0.15f, 0.15f);

        GameObject popup = pool.Get(position, Quaternion.identity);
        if (popup != null && popup.TryGetComponent(out DamagePopup damagePopup))
        {
            bool isPlayer = target.TryGetComponent(out PlayerStats _);
            damagePopup.Show(pool, amount, isPlayer ? playerHitColor : enemyHitColor);
        }
    }
}
