using System;
using UnityEngine;

public enum QuestObjectiveType
{
    /// <summary>targetEnemy 종류를 count마리 처치. 보스도 여기에 BossStats 에셋을 넣으면 된다.</summary>
    KillEnemy,
    /// <summary>level 이상 달성.</summary>
    ReachLevel,
    /// <summary>itemType 칸(무기·방어구)에 무언가를 착용.</summary>
    EquipItem
}

[Serializable]
public class QuestObjective
{
    public QuestObjectiveType type = QuestObjectiveType.KillEnemy;

    [Tooltip("KillEnemy: 잡아야 할 종류의 스탯 에셋.")]
    public EnemyStatsData targetEnemy;
    [Tooltip("KillEnemy: 몇 마리.")]
    [Min(1)] public int count = 1;

    [Tooltip("ReachLevel: 목표 레벨.")]
    [Min(1)] public int level = 1;

    [Tooltip("EquipItem: 어느 칸에 착용해야 하는지.")]
    public ItemType itemType = ItemType.Weapon;

    [Tooltip("추적 창에 보일 문구. 비우면 자동으로 만든다 (예: \"Defeat Slime\").")]
    public string label;

    /// <summary>진행도 표시용 목표 수치. 킬 수, 레벨, 착용(1).</summary>
    public int Required => type == QuestObjectiveType.KillEnemy ? count : type == QuestObjectiveType.ReachLevel ? level : 1;

    public string Label
    {
        get
        {
            if (!string.IsNullOrEmpty(label)) return label;
            switch (type)
            {
                case QuestObjectiveType.KillEnemy:
                    return targetEnemy != null ? $"Defeat {targetEnemy.name}" : "Defeat enemies";
                case QuestObjectiveType.ReachLevel:
                    return $"Reach Lv.{level}";
                default:
                    return $"Equip a {itemType.ToString().ToLower()}";
            }
        }
    }
}

/// <summary>
/// 퀘스트 하나의 정의. 메인 퀘스트 순서는 PlayerQuests의 chain 배열이 정하고,
/// 누가 주고 누가 받는지는 씬의 QuestNPC가 offers / completes 목록으로 정한다.
/// 진행 상태(몇 마리 잡았나)는 PlayerQuests가 들고 있는다.
/// </summary>
[CreateAssetMenu(menuName = "RPG/Quest")]
public class QuestData : ScriptableObject
{
    public string title = "Quest";
    [Tooltip("수락 전 대화창에 보이는 대사.")]
    [TextArea(2, 5)] public string description;
    [Tooltip("보고할 때 대화창에 보이는 대사.")]
    [TextArea(2, 5)] public string completionText;

    [Tooltip("추적 창 안내용 이름. 실제로 주고받는 NPC는 QuestNPC 목록이 정한다 — 이름을 맞춰 둔다.")]
    public string giverName = "Chief";
    public string reporterName = "Chief";

    public QuestObjective[] objectives = new QuestObjective[0];

    [Header("보상")]
    [Min(0)] public int expReward = 0;
    [Min(0)] public int goldReward = 0;

    public string RewardText => $"Reward: {expReward} EXP, {goldReward} Gold";
}
