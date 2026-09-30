using TMPro;
using UnityEngine;

/// <summary>맞은 대상 머리 위로 떠오르며 사라지는 데미지 숫자. <see cref="DamagePopupSpawner"/>가 풀에서 꺼낸다.</summary>
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField, Min(0.1f)] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1.2f;

    private ObjectPool pool;
    private float age;
    private Color baseColor;

    public void Show(ObjectPool owner, int amount, Color color)
    {
        pool = owner;
        age = 0f;
        baseColor = color;
        text.text = amount.ToString();
        text.color = color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        Color color = baseColor;
        color.a = 1f - Mathf.Clamp01((age - lifetime * 0.5f) / (lifetime * 0.5f));
        text.color = color;

        if (age >= lifetime)
        {
            if (pool != null) pool.Release(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
