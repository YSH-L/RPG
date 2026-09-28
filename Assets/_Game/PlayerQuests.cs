using System;
using UnityEngine;

/// <summary>
/// 일직선 메인 퀘스트의 진행 상태. chain 순서대로 한 번에 하나만 진행한다.
/// 수락 → 목표 달성 → 보고(보상) → 다음 퀘스트. 수락·보고는 QuestNPC 대화창에서 부른다.
/// Player 오브젝트에 붙인다. 추적 창·대화창은 OnChanged를 구독해서 다시 그린다.
/// </summary>
public class PlayerQuests : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerInventory inventory;
    [Tooltip("메인 퀘스트 순서.")]
    [SerializeField] private QuestData[] chain;

    public QuestData CurrentQuest => chain != null && currentIndex < chain.Length ? chain[currentIndex] : null;
    public bool IsAccepted { get; private set; }
    public bool AllDone => CurrentQuest == null;
    /// <summary>수락했고 목표를 전부 채웠다. 보고만 남았다.</summary>
    public bool IsReadyToReport => IsAccepted && CurrentQuest != null && AllObjectivesMet();

    /// <summary>진행 상태가 바뀌면. 구독했으면 OnDisable에서 해제한다.</summary>
    public event Action OnChanged;

    private int currentIndex;
    private int[] kills = new int[0];
    private bool wasReady;

    private void OnEnable()
    {
        CombatEvents.OnDied += HandleDied;
        if (player != null) player.OnLevelChanged += HandleLevelChanged;
        if (inventory != null) inventory.OnChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        CombatEvents.OnDied -= HandleDied;
        if (player != null) player.OnLevelChanged -= HandleLevelChanged;
        if (inventory != null) inventory.OnChanged -= HandleStateChanged;
    }

    public void Accept()
    {
        if (CurrentQuest == null || IsAccepted) return;

        IsAccepted = true;
        kills = new int[CurrentQuest.objectives.Length];
        wasReady = false;
        ShowMessage($"Quest accepted: {CurrentQuest.title}");
        NotifyChanged();
    }

    /// <summary>목표를 다 채웠으면 보상을 주고 다음 퀘스트로 넘어간다.</summary>
    public bool TryReport()
    {
        if (!IsReadyToReport) return false;

        QuestData done = CurrentQuest;
        currentIndex++;
        IsAccepted = false;
        kills = new int[0];
        wasReady = false;

        // 경험치를 먼저 주면 레벨업 알림이 보상 메시지를 덮으므로 메시지를 마지막에 띄운다.
        if (inventory != null) inventory.AddGold(done.goldReward);
        if (player != null) player.GainExp(done.expReward);
        ShowMessage($"Quest complete: {done.title}  (+{done.expReward} EXP, +{done.goldReward} Gold)");
        NotifyChanged();
        return true;
    }

    /// <summary>목표 i의 (현재, 필요) 수치.</summary>
    public (int current, int required) GetProgress(int index)
    {
        QuestData quest = CurrentQuest;
        if (quest == null || index >= quest.objectives.Length) return (0, 0);

        QuestObjective o = quest.objectives[index];
        switch (o.type)
        {
            case QuestObjectiveType.KillEnemy:
                return (Mathf.Min(IsAccepted && index < kills.Length ? kills[index] : 0, o.count), o.count);
            case QuestObjectiveType.ReachLevel:
                return (player != null ? Mathf.Min(player.CurrentLevel, o.level) : 0, o.level);
            default:
                return (IsEquipped(o.itemType) ? 1 : 0, 1);
        }
    }

    private bool AllObjectivesMet()
    {
        for (int i = 0; i < CurrentQuest.objectives.Length; i++)
        {
            var (current, required) = GetProgress(i);
            if (current < required) return false;
        }
        return true;
    }

    private bool IsEquipped(ItemType slot)
    {
        if (inventory == null) return false;
        return slot == ItemType.Weapon ? inventory.EquippedWeapon != null
             : slot == ItemType.Armor && inventory.EquippedArmor != null;
    }

    private void HandleDied(GameObject target)
    {
        if (!IsAccepted || CurrentQuest == null) return;
        if (!target.TryGetComponent<EnemyBase>(out var enemy)) return;

        bool counted = false;
        QuestObjective[] objectives = CurrentQuest.objectives;
        for (int i = 0; i < objectives.Length; i++)
        {
            if (objectives[i].type == QuestObjectiveType.KillEnemy && objectives[i].targetEnemy == enemy.Data && kills[i] < objectives[i].count)
            {
                kills[i]++;
                counted = true;
            }
        }

        if (counted) NotifyChanged();
    }

    private void HandleLevelChanged(int level) => HandleStateChanged();

    private void HandleStateChanged()
    {
        if (IsAccepted) NotifyChanged();
    }

    private void NotifyChanged()
    {
        // 방금 목표를 다 채웠으면 한 번만 알린다.
        bool ready = IsReadyToReport;
        if (ready && !wasReady) ShowMessage($"Objectives done! Report to {CurrentQuest.reporterName}", 2.5f);
        wasReady = ready;

        OnChanged?.Invoke();
    }

    // UI 폰트(LiberationSans)에 한글 글리프가 없어서 화면 문구는 영어로 쓴다.
    private static void ShowMessage(string text, float duration = 1.5f)
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(text, duration);
    }
}
