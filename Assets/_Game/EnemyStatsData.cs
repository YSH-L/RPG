using UnityEngine;

/// <summary>
/// 몬스터 종류별 기본값. 종류마다 에셋을 하나씩 만든다 (Mushroom.asset, Skeleton.asset ...).
/// 상태(CurrentHP)는 Enemy가 들고 있는다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Enemy Stats")]
public class EnemyStatsData : ScriptableObject
{
    [Header("전투")]
    public int maxHP = 50;
    public int contactDamage = 10;
    public int expReward = 10;
    [Tooltip("처치하면 바로 들어오는 골드.")]
    public int goldReward = 5;

    [Header("이동/탐지")]
    public float moveSpeed = 1.5f;
    [Tooltip("이 거리 안에 플레이어가 들어오면 쫓아간다.")]
    public float detectionRange = 4f;
    [Tooltip("접촉 피해를 반복해서 주는 간격(초).")]
    public float contactDamageInterval = 1f;
}
