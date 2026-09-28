using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 저장·불러오기. 플레이어의 상태 컴포넌트(PlayerController·PlayerInventory·PlayerQuests)와
/// 처치한 보스, 마지막 맵을 JSON 파일 하나로 쓴다. 상태를 들고 있지는 않고 모아서 쓰고 되돌리기만 한다.
/// <list type="bullet">
/// <item>불러오기: 씬이 시작될 때(Ready) 자동. 죽고 R로 재시작해도 씬이 다시 열리므로 마지막 저장에서 이어진다.
/// 보스맵에서 저장됐으면 그 앞 필드에서 시작한다.</item>
/// <item>자동 저장: 맵 이동·보스 처치·레벨업은 바로, 골드·아이템·퀘스트 변화는 autoSaveInterval에 한 번.</item>
/// <item>수동 저장: F5.</item>
/// <item>죽은 뒤에는 저장하지 않는다. 마지막 보스를 잡으면 저장을 지워서 다음은 새 게임이 된다.</item>
/// </list>
/// Player 오브젝트에 붙인다.
/// </summary>
public class PlayerSave : MonoBehaviour
{
    private const string FileName = "save.json";

    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerQuests quests;
    [SerializeField] private CameraFollow2D cameraFollow;
    [Tooltip("씬의 모든 맵. 첫 번째가 새 게임 시작 맵.")]
    [SerializeField] private MapArea[] areas;
    [SerializeField] private BossController[] bosses;
    [Tooltip("저장 파일의 아이템 이름을 에셋으로 되돌릴 때 찾는 목록. 새 아이템을 만들면 여기에도 넣는다.")]
    [SerializeField] private ItemData[] itemCatalog;
    [Tooltip("자잘한 변화(골드·아이템·퀘스트 진행)를 모아서 저장하는 간격(초).")]
    [SerializeField, Min(1f)] private float autoSaveInterval = 5f;

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>저장했을 때. true면 F5로 직접 저장한 것. 구독했으면 OnDisable에서 해제한다.</summary>
    public event Action<bool> OnSaved;

    private readonly HashSet<string> defeatedBosses = new HashSet<string>();
    private MapArea currentArea;
    private bool dirty;
    private bool loading;
    private bool cleared;
    private float lastSaveTime = float.NegativeInfinity;

    private void OnEnable()
    {
        MapArea.OnEntered += HandleAreaEntered;
        CombatEvents.OnDied += HandleDied;
        if (player != null) player.OnLevelChanged += HandleLevelChanged;
        if (inventory != null) inventory.OnChanged += MarkDirty;
        if (quests != null) quests.OnChanged += MarkDirty;
    }

    private void OnDisable()
    {
        MapArea.OnEntered -= HandleAreaEntered;
        CombatEvents.OnDied -= HandleDied;
        if (player != null) player.OnLevelChanged -= HandleLevelChanged;
        if (inventory != null) inventory.OnChanged -= MarkDirty;
        if (quests != null) quests.OnChanged -= MarkDirty;
    }

    private void Start()
    {
        currentArea = areas != null && areas.Length > 0 ? areas[0] : null;
        Load();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.f5Key.wasPressedThisFrame)
        {
            Save(manual: true);
            return;
        }

        if (dirty && Time.unscaledTime - lastSaveTime >= autoSaveInterval) Save(manual: false);
    }

    private void OnApplicationQuit()
    {
        if (dirty) Save(manual: false);
    }

    // ── 자동 저장 계기 ──────────────────────────────────────────────

    private void HandleAreaEntered(MapArea area)
    {
        currentArea = area;
        if (!loading) Save(manual: false);
    }

    private void HandleLevelChanged(int level)
    {
        if (!loading) Save(manual: false);
    }

    private void MarkDirty()
    {
        if (!loading) dirty = true;
    }

    private void HandleDied(GameObject target)
    {
        if (!target.TryGetComponent<BossController>(out var boss) || boss.Data == null) return;

        if (boss.EndsGame)
        {
            // 클리어. 다음에 R을 누르면 새 게임이다.
            cleared = true;
            DeleteSaveFile();
            return;
        }

        defeatedBosses.Add(boss.Data.name);
        Save(manual: false);
    }

    // ── 저장 ──────────────────────────────────────────────────────

    private bool CanSave => !cleared && !loading && player != null && !player.IsDead;

    public void Save(bool manual)
    {
        if (!CanSave) return;

        var data = new SaveData
        {
            level = player.CurrentLevel,
            exp = player.CurrentExp,
            area = currentArea != null ? currentArea.name : null,
            defeatedBosses = new List<string>(defeatedBosses),
        };

        if (inventory != null)
        {
            data.gold = inventory.Gold;
            foreach (ItemData item in inventory.Items)
            {
                data.items.Add(new ItemStack { item = item.name, count = inventory.CountOf(item) });
            }
            data.weapon = inventory.EquippedWeapon != null ? inventory.EquippedWeapon.name : null;
            data.armor = inventory.EquippedArmor != null ? inventory.EquippedArmor.name : null;
        }

        if (quests != null)
        {
            data.questIndex = quests.CurrentIndex;
            data.questAccepted = quests.IsAccepted;
            data.questKills = quests.GetKillCounts();
        }

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerSave] 저장 실패: {e.Message}");
            return;
        }

        dirty = false;
        lastSaveTime = Time.unscaledTime;
        OnSaved?.Invoke(manual);
    }

    public static void DeleteSaveFile()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }

    // ── 불러오기 ───────────────────────────────────────────────────

    private void Load()
    {
        if (!File.Exists(SavePath)) return;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerSave] 저장 파일을 읽지 못해 새 게임으로 시작합니다: {e.Message}");
            return;
        }
        if (data == null) return;

        loading = true;

        // 레벨 먼저 (HP 가득) → 장비 (최대 HP 보너스만큼 더 채워진다) → 퀘스트 (레벨·장비 목표를 제대로 판정)
        player.RestoreProgress(data.level, data.exp);

        if (inventory != null)
        {
            var items = new List<KeyValuePair<ItemData, int>>();
            foreach (ItemStack stack in data.items)
            {
                ItemData item = FindItem(stack.item);
                if (item != null) items.Add(new KeyValuePair<ItemData, int>(item, stack.count));
            }
            inventory.RestoreState(data.gold, items, FindItem(data.weapon), FindItem(data.armor));
        }

        if (quests != null) quests.RestoreState(data.questIndex, data.questAccepted, data.questKills);

        defeatedBosses.Clear();
        foreach (string id in data.defeatedBosses) defeatedBosses.Add(id);
        foreach (BossController boss in bosses)
        {
            // 이미 잡은 보스는 꺼 둔다. Portal이 이걸 보고 다음 지역 포탈을 연다.
            if (boss != null && boss.Data != null && defeatedBosses.Contains(boss.Data.name)) boss.gameObject.SetActive(false);
        }

        MapArea area = FindArea(data.area);
        if (area != null && area.RetreatTo != null) area = area.RetreatTo;
        if (area != null && area.SpawnPoint != null)
        {
            Vector3 spawn = area.SpawnPoint.position;
            if (player.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.position = spawn;
                rb.linearVelocity = Vector2.zero;
            }
            player.transform.position = spawn;
            area.Enter(cameraFollow);   // 카메라 경계. OnEntered로 currentArea도 갱신된다
        }

        loading = false;
        dirty = false;

        ShowMessage($"Save loaded  (Lv.{player.CurrentLevel})");
    }

    private ItemData FindItem(string assetName)
    {
        if (string.IsNullOrEmpty(assetName) || itemCatalog == null) return null;
        return Array.Find(itemCatalog, i => i != null && i.name == assetName);
    }

    private MapArea FindArea(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || areas == null) return null;
        return Array.Find(areas, a => a != null && a.name == objectName);
    }

    // UI 폰트(LiberationSans)에 한글 글리프가 없어서 화면 문구는 영어로 쓴다.
    private static void ShowMessage(string text)
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(text, 2.5f);
    }
}
