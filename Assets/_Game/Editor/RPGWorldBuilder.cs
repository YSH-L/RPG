using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 한 번으로 일반 몬스터 8종·보스 2종·포탈·맵 4개를 RPG.unity에 배치한다.
/// <b>RPG ▸ Build Monsters &amp; Boss Maps</b>. 다시 실행하면 "Generated" 아래를 지우고 새로 만든다.
/// <para>
/// 맵 배치 (전부 같은 씬, x축으로 60씩 떨어져 있다):
/// 필드1(x=0) → [Lv.5] → 보스맵1 Fire Worm(x=60) → [보스 처치] → 필드2(x=120) → [Lv.10] → 보스맵2 Golem(x=180)
/// </para>
/// 수치는 여기 없다. 전부 _Game/Data의 에셋에서 고친다.
/// </summary>
public static class RPGWorldBuilder
{
    private const string ScenePath = "Assets/Scenes/RPG.unity";
    private const string PrefabDir = "Assets/_Game/Prefabs";
    private const string DataDir = "Assets/_Game/Data";
    private const string N1 = "Assets/Sprites/Enemy/Normal/Monsters Creatures Fantasy/Sprites/";
    private const string N2 = "Assets/Sprites/Enemy/Normal/Monsters Creatures Fantasy 2/Sprites/";
    private const string Worm = "Assets/Sprites/Enemy/Boss/Fire Worm/Sprites/";
    private const string GolemSheet = "Assets/Sprites/Enemy/Boss/Mecha-stone Golem 0.1/PNG sheet/Character_sheet.png";
    private const string GolemArm = "Assets/Sprites/Enemy/Boss/Mecha-stone Golem 0.1/weapon PNG/arm_projectile.png";
    private const string PortalFrames = "Assets/Sprites/Skills/Warlock/VFX3/Frames/";
    private const string DashFxFrames = "Assets/Sprites/Skills/Slash/128x128/Slash 2/color5/Frames/Slash2_color5_frame";
    private const string WhirlFxFrames = "Assets/Sprites/Skills/Warrior/VFX 1/Frames/warrior_skill1_frame";
    private const string WaveFrames = "Assets/Sprites/Skills/Frost Knight/VFX1/frames/FrostKnight_skill1_frame";
    private const string ShopkeeperIdle = "Assets/Sprites/Character/Archer/Archer-Idle-spritesheet.png";

    private const int LayerDefault = 0;
    private const int LayerPlayer = 9;
    private const int LayerEnemy = 10;

    private const float AreaSpacing = 60f;

    // ── 애니메이션 정의 ─────────────────────────────────────────────

    private struct ClipDef
    {
        public AnimState state;
        public string path;
        public string prefix;   // 한 장에 여러 동작이 있는 시트(Golem)에서 이름 앞부분으로 거른다
        public float fps;
        public bool loop;

        public ClipDef(AnimState state, string path, float fps, bool loop, string prefix = null)
        {
            this.state = state;
            this.path = path;
            this.fps = fps;
            this.loop = loop;
            this.prefix = prefix;
        }
    }

    private static ClipDef[] Clips(string idle, string walk, string attack, string hit, string death)
    {
        return new[]
        {
            new ClipDef(AnimState.Idle, idle, 8f, true),
            new ClipDef(AnimState.Walk, walk, 10f, true),
            new ClipDef(AnimState.Attack, attack, 14f, false),
            new ClipDef(AnimState.Hit, hit, 10f, false),
            new ClipDef(AnimState.Death, death, 8f, false),
        };
    }

    private class MonsterDef
    {
        public string name;
        public ClipDef[] clips;
        public Vector2 colliderSize;
        public float colliderCenterY;
    }

    private static readonly MonsterDef[] Monsters =
    {
        new MonsterDef { name = "Slime", colliderSize = new Vector2(1.2f, 0.6f), colliderCenterY = 0.31f,
            clips = Clips(N2 + "Slime/idle.png", N2 + "Slime/walk.png", N2 + "Slime/attack.png", N2 + "Slime/hurt.png", N2 + "Slime/death.png") },
        new MonsterDef { name = "Rat", colliderSize = new Vector2(1.0f, 0.6f), colliderCenterY = 0.31f,
            clips = Clips(N2 + "Rat/idle.png", N2 + "Rat/run.png", N2 + "Rat/attack_bite.png", N2 + "Rat/hurt.png", N2 + "Rat/rat-death.png") },
        new MonsterDef { name = "Mushroom", colliderSize = new Vector2(0.7f, 1.1f), colliderCenterY = 0.58f,
            clips = Clips(N1 + "Mushroom/Idle.png", N1 + "Mushroom/Run.png", N1 + "Mushroom/Attack1.png", N1 + "Mushroom/Take Hit.png", N1 + "Mushroom/Death.png") },
        new MonsterDef { name = "Bat", colliderSize = new Vector2(0.9f, 0.9f), colliderCenterY = 0.86f,
            clips = Clips(N2 + "Bat/fly.png", N2 + "Bat/fly.png", N2 + "Bat/attack.png", N2 + "Bat/hurt.png", N2 + "Bat/death.png") },
        new MonsterDef { name = "Goblin", colliderSize = new Vector2(0.8f, 1.1f), colliderCenterY = 0.56f,
            clips = Clips(N1 + "Goblin/Idle.png", N1 + "Goblin/Run.png", N1 + "Goblin/Attack1.png", N1 + "Goblin/Take Hit.png", N1 + "Goblin/Death.png") },
        new MonsterDef { name = "Skeleton", colliderSize = new Vector2(0.9f, 1.5f), colliderCenterY = 0.8f,
            clips = Clips(N1 + "Skeleton/Idle.png", N1 + "Skeleton/Walk.png", N1 + "Skeleton/Attack1.png", N1 + "Skeleton/Take Hit.png", N1 + "Skeleton/Death.png") },
        new MonsterDef { name = "FlyingEye", colliderSize = new Vector2(1.0f, 0.9f), colliderCenterY = 0.77f,
            clips = Clips(N1 + "Flying eye/Flight.png", N1 + "Flying eye/Flight.png", N1 + "Flying eye/Attack1.png", N1 + "Flying eye/Take Hit.png", N1 + "Flying eye/Death.png") },
        new MonsterDef { name = "Mimic", colliderSize = new Vector2(1.1f, 1.0f), colliderCenterY = 0.53f,
            clips = Clips(N2 + "Mimic/idle_transformed.png", N2 + "Mimic/walk.png", N2 + "Mimic/attack_1.png", N2 + "Mimic/hurt.png", N2 + "Mimic/death.png") },
    };

    /// <summary>필드별 스폰 배치. x는 그 필드 기준 좌표, 비행형은 y를 살짝 띄운다.</summary>
    private static readonly Dictionary<string, Vector2[]> Field1Spawns = new Dictionary<string, Vector2[]>
    {
        { "Slime",    new[] { new Vector2(5f, 0f),  new Vector2(15f, 0f) } },
        { "Rat",      new[] { new Vector2(8f, 0f),  new Vector2(-10f, 0f) } },
        { "Mushroom", new[] { new Vector2(11f, 0f), new Vector2(19f, 0f) } },
        { "Bat",      new[] { new Vector2(13f, 0.1f), new Vector2(-6f, 0.1f) } },
    };

    private static readonly Dictionary<string, Vector2[]> Field2Spawns = new Dictionary<string, Vector2[]>
    {
        { "Goblin",    new[] { new Vector2(-4f, 0f), new Vector2(6f, 0f), new Vector2(16f, 0f) } },
        { "Skeleton",  new[] { new Vector2(2f, 0f), new Vector2(12f, 0f) } },
        { "FlyingEye", new[] { new Vector2(9f, 0.1f), new Vector2(18f, 0.1f) } },
        // 도착 지점(-11.5) 옆에 상인이 서 있어서 왼쪽 끝은 비워 둔다.
        { "Mimic",     new[] { new Vector2(4f, 0f), new Vector2(21f, 0f) } },
    };

    // ── 진입점 ─────────────────────────────────────────────────────

    [MenuItem("RPG/Build Monsters & Boss Maps")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabDir);

        // 씬을 먼저 연다. 프리팹을 만들 때 잠깐 생기는 임시 오브젝트도 이 씬에서 만들고 지운다.
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 1) 프리팹
        var enemyPrefabs = new Dictionary<string, GameObject>();
        foreach (MonsterDef m in Monsters) enemyPrefabs[m.name] = BuildEnemyPrefab(m);

        GameObject fireballPrefab = BuildFireballPrefab();
        GameObject armPrefab = BuildGolemArmPrefab();
        GameObject wormPrefab = BuildWormPrefab();
        GameObject golemPrefab = BuildGolemPrefab();
        GameObject portalPrefab = BuildPortalPrefab();
        GameObject dashFxPrefab = BuildEffectPrefab("FX_DashSlash", NumberedFrames(DashFxFrames, 1, 7), 18f, 1.5f);
        GameObject whirlFxPrefab = BuildEffectPrefab("FX_Whirlwind", NumberedFrames(WhirlFxFrames, 1, 10), 18f, 2.8f);
        GameObject swordWavePrefab = BuildSwordWavePrefab();
        GameObject shopNpcPrefab = BuildShopNpcPrefab();
        GameObject questNpcPrefab = BuildQuestNpcPrefab();

        // 2) 씬
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            // 예전에 씬에 직접 놓았던 Slime과 이전 빌드 결과는 지우고 다시 만든다.
            if (root.name == "Enemy_Slime" || root.name == "Generated") Object.DestroyImmediate(root);
        }
        roots = scene.GetRootGameObjects();

        GameObject player = roots.FirstOrDefault(r => r.name == "Player");
        CameraFollow2D cameraFollow = Object.FindFirstObjectByType<CameraFollow2D>();
        if (player == null || cameraFollow == null)
        {
            Debug.LogError("[RPGWorldBuilder] 씬에서 Player 또는 CameraFollow2D를 찾지 못했습니다.");
            return;
        }

        // 가만히 서 있는 플레이어가 잠들면 몬스터 접촉 판정(OnTriggerStay2D)이 멈춘다.
        if (player.TryGetComponent<Rigidbody2D>(out var playerBody))
        {
            playerBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            EditorUtility.SetDirty(playerBody);
        }

        var generated = new GameObject("Generated");

        // 필드1 원본 비주얼. 나머지 맵은 이것을 복제해서 x로 민다.
        string[] visualNames = { "Sky_Far", "Terrain_Entrance", "Terrain_Mine", "Terrain_Trees", "Decorations", "Ground" };
        GameObject[] visuals = roots.Where(r => visualNames.Contains(r.name)).ToArray();
        BoxCollider2D groundCol = roots.First(r => r.name == "Ground").GetComponent<BoxCollider2D>();
        float groundLeft = groundCol.bounds.min.x;
        float groundRight = groundCol.bounds.max.x;

        SerializedObject camSo = new SerializedObject(cameraFollow);
        Vector2 camMin = camSo.FindProperty("boundsMin").vector2Value;
        Vector2 camMax = camSo.FindProperty("boundsMax").vector2Value;

        MapArea field1 = CreateArea(generated, "Area_Field1", "Field 1", 0f, camMin, camMax, groundLeft, groundRight, visuals, Color.white);
        MapArea boss1 = CreateArea(generated, "Area_Boss1", "Boss: Fire Worm", 1f, camMin, camMax, groundLeft, groundRight, visuals, new Color(1f, 0.72f, 0.65f));
        MapArea field2 = CreateArea(generated, "Area_Field2", "Field 2", 2f, camMin, camMax, groundLeft, groundRight, visuals, new Color(0.8f, 0.9f, 0.8f));
        MapArea boss2 = CreateArea(generated, "Area_Boss2", "Boss: Mecha-stone Golem", 3f, camMin, camMax, groundLeft, groundRight, visuals, new Color(0.7f, 0.75f, 0.9f));

        // 3) 풀 + 스포너 (고정 배치 + 리스폰)
        var poolRoot = new GameObject("Pools");
        poolRoot.transform.SetParent(generated.transform, false);

        BuildSpawners(field1.transform, poolRoot.transform, enemyPrefabs, Field1Spawns, 0f);
        BuildSpawners(field2.transform, poolRoot.transform, enemyPrefabs, Field2Spawns, AreaSpacing * 2f);

        ObjectPool fireballPool = CreatePool(poolRoot.transform, "Pool_Fireball", fireballPrefab, 4);
        ObjectPool armPool = CreatePool(poolRoot.transform, "Pool_GolemArm", armPrefab, 4);

        // 4) 보스 — 오른쪽 끝에서 기다린다
        BossController worm = PlaceBoss(wormPrefab, boss1.transform, new Vector3(AreaSpacing * 1f + 18f, 0f, 0f), fireballPool);
        BossController golem = PlaceBoss(golemPrefab, boss2.transform, new Vector3(AreaSpacing * 3f + 18f, 0f, 0f), armPool);
        SetAreaBoss(boss1, worm);
        SetAreaBoss(boss2, golem);

        // 화면 상단 보스 체력바. HUD_Panel 안에 넣어서 GameOver 때 패널째로 숨겨지게 한다.
        BuildBossHealthBar(roots);

        // 플레이어 스킬 (Z / X / C) + 화면 하단 스킬 칸
        ObjectPool dashFxPool = CreatePool(poolRoot.transform, "Pool_FxDashSlash", dashFxPrefab, 2);
        ObjectPool whirlFxPool = CreatePool(poolRoot.transform, "Pool_FxWhirlwind", whirlFxPrefab, 2);
        ObjectPool swordWavePool = CreatePool(poolRoot.transform, "Pool_SwordWave", swordWavePrefab, 4);
        PlayerSkills skills = SetupPlayerSkills(player, dashFxPool, whirlFxPool, swordWavePool);
        BuildSkillHUD(roots, skills);

        // 인벤토리 · 상점 · 퀵슬롯
        PlayerInventory inventory = SetupPlayerInventory(player);
        Transform hudPanel = FindHudPanel(roots);
        if (hudPanel != null)
        {
            BuildQuickSlotHUD(hudPanel, inventory);
            BuildInventoryWindow(hudPanel, inventory, player.GetComponent<PlayerController>());
            ShopWindow shopWindow = BuildShopWindow(hudPanel, inventory, player.GetComponent<PlayerController>());

            // 필드1은 초급, 필드2는 상급 장비를 판다. 물약은 둘 다.
            PlaceShop(shopNpcPrefab, field1.transform, "Shop_Field1", -3f, "Field 1 Shop", shopWindow,
                "Item_SmallPotion", "Item_LargePotion", "Item_BronzeSword", "Item_IronSword", "Item_LeatherArmor", "Item_ChainMail");
            PlaceShop(shopNpcPrefab, field2.transform, "Shop_Field2", AreaSpacing * 2f - 9f, "Field 2 Shop", shopWindow,
                "Item_SmallPotion", "Item_LargePotion", "Item_SteelSword", "Item_KnightSword", "Item_PlateArmor", "Item_KnightArmor");

            // 메인 퀘스트: 필드1 Chief가 1~6번을 주고, 6번(Fire Worm)은 필드2 Guard에게 보고한다.
            string[] chain =
            {
                "Quest_01_SlimeTrouble", "Quest_02_GearUp", "Quest_03_Pests", "Quest_04_Mushrooms", "Quest_05_ReadyForTheNest",
                "Quest_06_FireWorm", "Quest_07_GoblinsAndMimics", "Quest_08_BonesAndEyes", "Quest_09_ReadyForTheGolem",
            };
            PlayerQuests quests = SetupPlayerQuests(player, inventory, chain);
            BuildQuestTracker(hudPanel, quests);
            QuestDialogWindow dialog = BuildQuestDialog(hudPanel, quests, player.GetComponent<PlayerController>());

            PlaceQuestNpc(questNpcPrefab, field1.transform, "QuestNPC_Chief", 2.5f, "Chief", dialog,
                chain.Take(6).ToArray(), chain.Take(5).ToArray());
            PlaceQuestNpc(questNpcPrefab, field2.transform, "QuestNPC_Guard", AreaSpacing * 2f - 14f, "Guard", dialog,
                chain.Skip(6).ToArray(), chain.Skip(5).ToArray());
        }
        else
        {
            Debug.LogError("[RPGWorldBuilder] Canvas/HUD_Panel을 찾지 못해 인벤토리·상점 UI를 만들지 않았습니다.");
        }

        // 5) 포탈
        float leftX = groundLeft + 2.5f;
        float rightX = groundRight - 2.5f;

        // 필드1 → 보스맵1 (Lv.5)
        PlacePortal(portalPrefab, field1.transform, "Portal_ToBoss1", rightX, boss1, leftX + AreaSpacing * 1f + 1.5f, 5, null, cameraFollow);
        // 보스맵1 → 필드1 (되돌아가기)
        PlacePortal(portalPrefab, boss1.transform, "Portal_BackToField1", leftX + AreaSpacing * 1f, field1, rightX - 1.5f, 0, null, cameraFollow);
        // 보스맵1 → 필드2 (Fire Worm 처치 후)
        PlacePortal(portalPrefab, boss1.transform, "Portal_ToField2", rightX + AreaSpacing * 1f, field2, leftX + AreaSpacing * 2f + 1.5f, 0, worm, cameraFollow);
        // 필드2 → 보스맵2 (Lv.10)
        PlacePortal(portalPrefab, field2.transform, "Portal_ToBoss2", rightX + AreaSpacing * 2f, boss2, leftX + AreaSpacing * 3f + 1.5f, 10, null, cameraFollow);
        // 보스맵2 → 필드2 (되돌아가기)
        PlacePortal(portalPrefab, boss2.transform, "Portal_BackToField2", leftX + AreaSpacing * 3f, field2, rightX + AreaSpacing * 2f - 1.5f, 0, null, cameraFollow);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[RPGWorldBuilder] 완료: 일반 몬스터 8종, 보스 2종, 포탈 5개, 맵 4개, 보스 체력바, 플레이어 스킬 3개, 상점 2곳과 인벤토리, 퀘스트 NPC 2명을 배치했습니다.");
        Selection.activeGameObject = generated;
    }

    // ── 맵 ────────────────────────────────────────────────────────

    private static MapArea CreateArea(GameObject parent, string objName, string displayName, float index,
        Vector2 camMin, Vector2 camMax, float groundLeft, float groundRight, GameObject[] visuals, Color tint)
    {
        float dx = AreaSpacing * index;

        var area = new GameObject(objName);
        area.transform.SetParent(parent.transform, false);

        // 필드1은 원본 비주얼을 그대로 쓰고, 나머지는 복제해서 옮긴다.
        if (index > 0f)
        {
            foreach (GameObject v in visuals)
            {
                GameObject copy = Object.Instantiate(v, area.transform);
                copy.name = v.name;
                copy.transform.position = v.transform.position + new Vector3(dx, 0f, 0f);

                if (copy.name == "Ground") continue;
                foreach (SpriteRenderer sr in copy.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.color = sr.color * tint;
                }
            }
        }

        // 맵 양 끝의 벽. Ground 레이어로 두면 벽에 붙어서 점프가 되므로 Default로 둔다.
        CreateWall(area.transform, "Wall_Left", groundLeft + dx - 0.5f);
        CreateWall(area.transform, "Wall_Right", groundRight + dx + 0.5f);

        MapArea mapArea = area.AddComponent<MapArea>();
        var so = new SerializedObject(mapArea);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("cameraMin").vector2Value = camMin + new Vector2(dx, 0f);
        so.FindProperty("cameraMax").vector2Value = camMax + new Vector2(dx, 0f);
        so.ApplyModifiedPropertiesWithoutUndo();

        return mapArea;
    }

    private static void CreateWall(Transform parent, string objName, float x)
    {
        var wall = new GameObject(objName);
        wall.layer = LayerDefault;
        wall.transform.SetParent(parent, false);
        wall.transform.position = new Vector3(x, 5f, 0f);
        var col = wall.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 20f);
    }

    // ── 스포너 ─────────────────────────────────────────────────────

    private static void BuildSpawners(Transform area, Transform poolRoot, Dictionary<string, GameObject> prefabs,
        Dictionary<string, Vector2[]> spawns, float dx)
    {
        foreach (KeyValuePair<string, Vector2[]> entry in spawns)
        {
            ObjectPool pool = CreatePool(poolRoot, "Pool_" + entry.Key, prefabs[entry.Key], entry.Value.Length);

            var spawnerObj = new GameObject("Spawner_" + entry.Key);
            spawnerObj.transform.SetParent(area, false);

            var points = new Transform[entry.Value.Length];
            for (int i = 0; i < entry.Value.Length; i++)
            {
                var p = new GameObject("SpawnPoint_" + i);
                p.transform.SetParent(spawnerObj.transform, false);
                p.transform.position = new Vector3(entry.Value[i].x + dx, entry.Value[i].y, 0f);
                points[i] = p.transform;
            }

            EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("pool").objectReferenceValue = pool;
            SerializedProperty arr = so.FindProperty("spawnPoints");
            arr.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static ObjectPool CreatePool(Transform parent, string objName, GameObject prefab, int initialSize)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        ObjectPool pool = go.AddComponent<ObjectPool>();
        var so = new SerializedObject(pool);
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("initialSize").intValue = initialSize;
        so.FindProperty("expandable").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        return pool;
    }

    // ── 보스·포탈 배치 ───────────────────────────────────────────────

    private static BossController PlaceBoss(GameObject prefab, Transform parent, Vector3 position, ObjectPool projectilePool)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        BossController boss = go.GetComponent<BossController>();
        var so = new SerializedObject(boss);
        so.FindProperty("projectilePool").objectReferenceValue = projectilePool;
        so.ApplyModifiedPropertiesWithoutUndo();
        // 처음엔 플레이어 쪽(왼쪽)을 보게 한다.
        go.GetComponent<SpriteRenderer>().flipX = true;
        return boss;
    }

    private static void PlacePortal(GameObject prefab, Transform parent, string objName, float x,
        MapArea destination, float arrivalX, int requiredLevel, BossController unlockOnDeath, CameraFollow2D cameraFollow)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = objName;
        go.transform.position = new Vector3(x, 1.2f, 0f);

        var arrival = new GameObject(objName + "_Arrival");
        arrival.transform.SetParent(parent, false);
        arrival.transform.position = new Vector3(arrivalX, 0.05f, 0f);

        Portal portal = go.GetComponent<Portal>();
        var so = new SerializedObject(portal);
        so.FindProperty("destination").objectReferenceValue = destination;
        so.FindProperty("arrivalPoint").objectReferenceValue = arrival.transform;
        so.FindProperty("cameraFollow").objectReferenceValue = cameraFollow;
        so.FindProperty("requiredLevel").intValue = requiredLevel;
        so.FindProperty("unlockOnDeath").objectReferenceValue = unlockOnDeath;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAreaBoss(MapArea area, BossController boss)
    {
        var so = new SerializedObject(area);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── 보스 체력바 UI ───────────────────────────────────────────────
    // 처음 위치·크기만 잡아준다. 색·폰트·위치는 인스펙터에서 조정한다.

    private static void BuildBossHealthBar(GameObject[] roots)
    {
        Transform hud = FindHudPanel(roots);
        if (hud == null)
        {
            Debug.LogError("[RPGWorldBuilder] Canvas/HUD_Panel을 찾지 못해 보스 체력바를 만들지 않았습니다.");
            return;
        }

        Transform old = hud.Find("BossHPBar");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // 스크립트는 항상 켜져 있는 바깥 오브젝트에, 켜고 끄는 건 안쪽 Content에.
        var root = new GameObject("BossHPBar", typeof(RectTransform));
        root.transform.SetParent(hud, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(root.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 1f);
        contentRt.anchorMax = new Vector2(0.5f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = new Vector2(0f, -20f);
        contentRt.sizeDelta = new Vector2(700f, 60f);

        var nameGo = new GameObject("NameText", typeof(RectTransform));
        nameGo.transform.SetParent(content.transform, false);
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = Vector2.zero;
        nameRt.sizeDelta = new Vector2(0f, 30f);
        TextMeshProUGUI nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.text = "Boss";
        nameText.fontSize = 24f;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;

        // DefaultControls는 손잡이가 달린 입력용 슬라이더를 만든다. 손잡이를 떼고 표시 전용으로 바꾼다.
        GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGo.name = "HPSlider";
        sliderGo.transform.SetParent(content.transform, false);
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = new Vector2(1f, 0f);
        sliderRt.pivot = new Vector2(0.5f, 0f);
        sliderRt.anchoredPosition = Vector2.zero;
        sliderRt.sizeDelta = new Vector2(0f, 22f);

        Slider slider = sliderGo.GetComponent<Slider>();
        Transform handleArea = sliderGo.transform.Find("Handle Slide Area");
        if (handleArea != null) Object.DestroyImmediate(handleArea.gameObject);
        slider.handleRect = null;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        StretchFull(sliderGo.transform.Find("Background") as RectTransform);
        StretchFull(sliderGo.transform.Find("Fill Area") as RectTransform);
        StretchFull(sliderGo.transform.Find("Fill Area/Fill") as RectTransform);
        SetImageColor(sliderGo.transform.Find("Background"), new Color(0.1f, 0.1f, 0.12f, 0.85f));
        SetImageColor(sliderGo.transform.Find("Fill Area/Fill"), new Color(0.85f, 0.15f, 0.15f, 1f));

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;   // UI

        content.SetActive(false);

        BossHealthBar bar = root.AddComponent<BossHealthBar>();
        var so = new SerializedObject(bar);
        so.FindProperty("content").objectReferenceValue = content;
        so.FindProperty("hpSlider").objectReferenceValue = slider;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StretchFull(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetImageColor(Transform t, Color color)
    {
        if (t != null && t.TryGetComponent<Image>(out var image)) image.color = color;
    }

    // ── 플레이어 스킬 ───────────────────────────────────────────────

    private static PlayerSkills SetupPlayerSkills(GameObject player, ObjectPool dashFx, ObjectPool whirlFx, ObjectPool swordWave)
    {
        PlayerSkills skills = player.GetComponent<PlayerSkills>();
        if (skills == null) skills = player.AddComponent<PlayerSkills>();

        var so = new SerializedObject(skills);
        so.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        so.FindProperty("animator").objectReferenceValue = player.GetComponent<SpriteAnimator>();
        so.FindProperty("enemyMask").intValue = 1 << LayerEnemy;

        SerializedProperty slots = so.FindProperty("slots");
        slots.arraySize = 3;
        FillSkillSlot(slots.GetArrayElementAtIndex(0), "Skill_DashSlash", dashFx, null);
        FillSkillSlot(slots.GetArrayElementAtIndex(1), "Skill_Whirlwind", whirlFx, null);
        FillSkillSlot(slots.GetArrayElementAtIndex(2), "Skill_SwordWave", null, swordWave);
        so.ApplyModifiedPropertiesWithoutUndo();
        return skills;
    }

    private static void FillSkillSlot(SerializedProperty slot, string skillAsset, ObjectPool effectPool, ObjectPool projectilePool)
    {
        slot.FindPropertyRelative("skill").objectReferenceValue = LoadData<SkillData>(skillAsset);
        slot.FindPropertyRelative("effectPool").objectReferenceValue = effectPool;
        slot.FindPropertyRelative("projectilePool").objectReferenceValue = projectilePool;
    }

    private static void BuildSkillHUD(GameObject[] roots, PlayerSkills skills)
    {
        Transform hud = FindHudPanel(roots);
        if (hud == null)
        {
            Debug.LogError("[RPGWorldBuilder] Canvas/HUD_Panel을 찾지 못해 스킬 칸을 만들지 않았습니다.");
            return;
        }

        Transform old = hud.Find("SkillBar");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var bar = new GameObject("SkillBar", typeof(RectTransform));
        bar.transform.SetParent(hud, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0f);
        barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0f, 20f);
        barRt.sizeDelta = new Vector2(3 * 90f - 10f, 80f);

        SkillHUD skillHud = bar.AddComponent<SkillHUD>();
        var so = new SerializedObject(skillHud);
        so.FindProperty("skills").objectReferenceValue = skills;
        SerializedProperty views = so.FindProperty("views");
        views.arraySize = 3;

        for (int i = 0; i < 3; i++)
        {
            var slot = new GameObject("Slot_" + PlayerSkills.KeyLabel(i), typeof(RectTransform));
            slot.transform.SetParent(bar.transform, false);
            var slotRt = slot.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0f, 0f);
            slotRt.anchorMax = new Vector2(0f, 1f);
            slotRt.pivot = new Vector2(0f, 0.5f);
            slotRt.anchoredPosition = new Vector2(i * 90f, 0f);
            slotRt.sizeDelta = new Vector2(80f, 0f);
            Image background = slot.AddComponent<Image>();
            background.color = new Color(0.15f, 0.2f, 0.35f, 0.9f);

            var cover = new GameObject("CooldownCover", typeof(RectTransform));
            cover.transform.SetParent(slot.transform, false);
            var coverRt = cover.GetComponent<RectTransform>();
            coverRt.anchorMin = Vector2.zero;
            coverRt.anchorMax = new Vector2(1f, 0f);
            coverRt.offsetMin = Vector2.zero;
            coverRt.offsetMax = Vector2.zero;
            cover.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            TextMeshProUGUI keyText = CreateText(slot.transform, "KeyText", PlayerSkills.KeyLabel(i), 22f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -4f), new Vector2(30f, 26f), TextAlignmentOptions.TopLeft);
            TextMeshProUGUI labelText = CreateText(slot.transform, "LabelText", "", 14f,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 4f), new Vector2(0f, 22f), TextAlignmentOptions.Bottom);

            SerializedProperty view = views.GetArrayElementAtIndex(i);
            view.FindPropertyRelative("background").objectReferenceValue = background;
            view.FindPropertyRelative("cooldownCover").objectReferenceValue = coverRt;
            view.FindPropertyRelative("keyText").objectReferenceValue = keyText;
            view.FindPropertyRelative("labelText").objectReferenceValue = labelText;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (Transform t in bar.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;   // UI
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objName, string text, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ── 인벤토리 · 상점 ─────────────────────────────────────────────

    // ── 퀘스트 ─────────────────────────────────────────────────────

    private static PlayerQuests SetupPlayerQuests(GameObject player, PlayerInventory inventory, string[] chain)
    {
        PlayerQuests quests = player.GetComponent<PlayerQuests>();
        if (quests == null) quests = player.AddComponent<PlayerQuests>();

        var so = new SerializedObject(quests);
        so.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        so.FindProperty("inventory").objectReferenceValue = inventory;
        SetObjectArray(so.FindProperty("chain"), chain.Select(n => (Object)LoadData<QuestData>(n)).ToArray());
        so.ApplyModifiedPropertiesWithoutUndo();
        return quests;
    }

    private static void PlaceQuestNpc(GameObject prefab, Transform parent, string objName, float x, string npcName,
        QuestDialogWindow dialog, string[] offers, string[] completes)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = objName;
        go.transform.position = new Vector3(x, 0f, 0f);

        QuestNPC npc = go.GetComponent<QuestNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("npcName").stringValue = npcName;
        so.FindProperty("window").objectReferenceValue = dialog;
        SetObjectArray(so.FindProperty("offers"), offers.Select(n => (Object)LoadData<QuestData>(n)).ToArray());
        SetObjectArray(so.FindProperty("completes"), completes.Select(n => (Object)LoadData<QuestData>(n)).ToArray());
        so.ApplyModifiedPropertiesWithoutUndo();

        // 머리 위 이름표
        Transform nameLabel = go.transform.Find("NameLabel");
        if (nameLabel != null && nameLabel.TryGetComponent<TextMeshPro>(out var nameText)) nameText.text = $"{npcName}  [Up]";
    }

    private static void BuildQuestTracker(Transform hud, PlayerQuests quests)
    {
        DestroyChild(hud, "QuestTracker");

        RectTransform panel = CreateRect(hud, "QuestTracker", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -90f), new Vector2(360f, 130f));
        panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        TextMeshProUGUI title = CreateText(panel, "TitleText", "Quest", 19f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(-20f, 24f), TextAlignmentOptions.TopLeft);
        title.color = new Color(1f, 0.85f, 0.3f);
        TextMeshProUGUI body = CreateText(panel, "BodyText", "", 15f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -34f), new Vector2(-20f, 90f), TextAlignmentOptions.TopLeft);

        QuestTrackerHUD tracker = panel.gameObject.AddComponent<QuestTrackerHUD>();
        var so = new SerializedObject(tracker);
        so.FindProperty("quests").objectReferenceValue = quests;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.ApplyModifiedPropertiesWithoutUndo();
        SetUILayer(panel);
    }

    private static QuestDialogWindow BuildQuestDialog(Transform hud, PlayerQuests quests, PlayerController player)
    {
        DestroyChild(hud, "QuestDialog");

        RectTransform root = CreateRect(hud, "QuestDialog", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        // 화면 가운데 아래(y -260)에 뜨는 UIManager 메시지와 겹치지 않게 가운데보다 위에 둔다.
        RectTransform panel = CreateRect(root, "Content", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 140f), new Vector2(760f, 300f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 0.95f);

        TextMeshProUGUI speaker = CreateText(panel, "SpeakerText", "", 22f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(-40f, 28f), TextAlignmentOptions.TopLeft);
        speaker.color = new Color(1f, 0.85f, 0.3f);
        TextMeshProUGUI body = CreateText(panel, "BodyText", "", 17f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(-40f, 200f), TextAlignmentOptions.TopLeft);

        Button action = CreateButton(panel, "ActionButton", "OK", new Vector2(1f, 0f), new Vector2(-120f, 12f), new Vector2(120f, 36f));
        Button close = CreateButton(panel, "CloseButton", "Close", new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(100f, 36f));

        QuestDialogWindow window = root.gameObject.AddComponent<QuestDialogWindow>();
        var so = new SerializedObject(window);
        so.FindProperty("quests").objectReferenceValue = quests;
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("content").objectReferenceValue = panel.gameObject;
        so.FindProperty("speakerText").objectReferenceValue = speaker;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("actionButton").objectReferenceValue = action;
        so.FindProperty("actionText").objectReferenceValue = action.GetComponentInChildren<TextMeshProUGUI>(true);
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        SetUILayer(root);
        return window;
    }

    private static PlayerInventory SetupPlayerInventory(GameObject player)
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null) inventory = player.AddComponent<PlayerInventory>();

        var so = new SerializedObject(inventory);
        so.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        so.FindProperty("startingGold").intValue = 50;
        SerializedProperty slots = so.FindProperty("quickSlots");
        slots.arraySize = 2;
        slots.GetArrayElementAtIndex(0).objectReferenceValue = LoadData<ItemData>("Item_SmallPotion");
        slots.GetArrayElementAtIndex(1).objectReferenceValue = LoadData<ItemData>("Item_LargePotion");
        so.ApplyModifiedPropertiesWithoutUndo();
        return inventory;
    }

    private static void PlaceShop(GameObject prefab, Transform parent, string objName, float x, string shopName,
        ShopWindow window, params string[] stockAssets)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = objName;
        go.transform.position = new Vector3(x, 0f, 0f);

        ShopNPC npc = go.GetComponent<ShopNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("shopName").stringValue = shopName;
        so.FindProperty("window").objectReferenceValue = window;
        SerializedProperty stock = so.FindProperty("stock");
        stock.arraySize = stockAssets.Length;
        for (int i = 0; i < stockAssets.Length; i++)
        {
            stock.GetArrayElementAtIndex(i).objectReferenceValue = LoadData<ItemData>(stockAssets[i]);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildQuickSlotHUD(Transform hud, PlayerInventory inventory)
    {
        DestroyChild(hud, "QuickBar");

        // 스킬 칸(가운데, 폭 260) 오른쪽에 붙인다.
        RectTransform bar = CreateRect(hud, "QuickBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f),
            new Vector2(160f, 20f), new Vector2(170f, 100f));

        TextMeshProUGUI gold = CreateText(bar, "GoldText", "Gold 0", 18f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 20f), TextAlignmentOptions.Left);
        gold.color = new Color(1f, 0.85f, 0.3f);

        QuickSlotHUD quick = bar.gameObject.AddComponent<QuickSlotHUD>();
        var so = new SerializedObject(quick);
        so.FindProperty("inventory").objectReferenceValue = inventory;
        so.FindProperty("goldText").objectReferenceValue = gold;
        SerializedProperty views = so.FindProperty("views");
        views.arraySize = 2;

        for (int i = 0; i < 2; i++)
        {
            RectTransform slot = CreateRect(bar, "Slot_" + (i + 1), Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(i * 90f, 0f), new Vector2(80f, 80f));
            slot.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.15f, 0.15f, 0.9f);

            RectTransform iconRt = CreateRect(slot, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(48f, 48f));
            Image icon = iconRt.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI key = CreateText(slot, "KeyText", (i + 1).ToString(), 22f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -4f), new Vector2(30f, 26f), TextAlignmentOptions.TopLeft);
            TextMeshProUGUI count = CreateText(slot, "CountText", "0", 18f,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 4f), new Vector2(50f, 22f), TextAlignmentOptions.BottomRight);

            SerializedProperty view = views.GetArrayElementAtIndex(i);
            view.FindPropertyRelative("icon").objectReferenceValue = icon;
            view.FindPropertyRelative("keyText").objectReferenceValue = key;
            view.FindPropertyRelative("countText").objectReferenceValue = count;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        SetUILayer(bar);
    }

    private static void BuildInventoryWindow(Transform hud, PlayerInventory inventory, PlayerController player)
    {
        DestroyChild(hud, "InventoryWindow");

        // 스크립트는 항상 켜진 바깥 오브젝트에(I 키를 계속 받아야 한다), 창 본체는 Content로 켜고 끈다.
        RectTransform root = CreateRect(hud, "InventoryWindow", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        const int rowCount = 10;
        const float rowHeight = 44f;
        float height = 110f + rowCount * rowHeight + 10f;

        RectTransform panel = CreateRect(root, "Content", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-20f, 0f), new Vector2(520f, height));
        panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        CreateText(panel, "TitleText", "Inventory  [I]", 24f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 30f), TextAlignmentOptions.Center);
        TextMeshProUGUI gold = CreateText(panel, "GoldText", "Gold 0", 18f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(-24f, 22f), TextAlignmentOptions.Left);
        gold.color = new Color(1f, 0.85f, 0.3f);
        TextMeshProUGUI stats = CreateText(panel, "StatsText", "", 15f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(-24f, 44f), TextAlignmentOptions.TopLeft);

        ItemRowView[] rows = CreateRows(panel, rowCount, rowHeight, 110f);

        InventoryWindow window = root.gameObject.AddComponent<InventoryWindow>();
        var so = new SerializedObject(window);
        so.FindProperty("inventory").objectReferenceValue = inventory;
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("content").objectReferenceValue = panel.gameObject;
        so.FindProperty("goldText").objectReferenceValue = gold;
        so.FindProperty("statsText").objectReferenceValue = stats;
        SetObjectArray(so.FindProperty("rows"), rows);
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        SetUILayer(root);
    }

    private static ShopWindow BuildShopWindow(Transform hud, PlayerInventory inventory, PlayerController player)
    {
        DestroyChild(hud, "ShopWindow");

        RectTransform root = CreateRect(hud, "ShopWindow", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        const int rowCount = 6;
        const float rowHeight = 48f;
        float height = 80f + rowCount * rowHeight + 10f;

        RectTransform panel = CreateRect(root, "Content", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(560f, height));
        panel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.09f, 0.06f, 0.95f);

        TextMeshProUGUI title = CreateText(panel, "TitleText", "Shop", 24f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 30f), TextAlignmentOptions.Center);
        TextMeshProUGUI gold = CreateText(panel, "GoldText", "Gold 0", 18f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(-24f, 22f), TextAlignmentOptions.Left);
        gold.color = new Color(1f, 0.85f, 0.3f);

        Button close = CreateButton(panel, "CloseButton", "Close",
            new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(90f, 30f));

        ItemRowView[] rows = CreateRows(panel, rowCount, rowHeight, 80f);

        ShopWindow window = root.gameObject.AddComponent<ShopWindow>();
        var so = new SerializedObject(window);
        so.FindProperty("inventory").objectReferenceValue = inventory;
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("content").objectReferenceValue = panel.gameObject;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("goldText").objectReferenceValue = gold;
        so.FindProperty("closeButton").objectReferenceValue = close;
        SetObjectArray(so.FindProperty("rows"), rows);
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        SetUILayer(root);
        return window;
    }

    /// <summary>아이콘 | 이름 / 설명 | 버튼 한 줄을 rowCount개 위에서부터 쌓는다.</summary>
    private static ItemRowView[] CreateRows(RectTransform panel, int rowCount, float rowHeight, float top)
    {
        var rows = new ItemRowView[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            RectTransform row = CreateRect(panel, "Row_" + i, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -(top + i * rowHeight)), new Vector2(-20f, rowHeight - 4f));
            row.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            RectTransform iconRt = CreateRect(row, "Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(6f, 0f), new Vector2(rowHeight - 12f, rowHeight - 12f));
            Image icon = iconRt.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI name = CreateText(row, "NameText", "", 17f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(rowHeight, -2f), new Vector2(300f, 22f), TextAlignmentOptions.TopLeft);
            TextMeshProUGUI detail = CreateText(row, "DetailText", "", 13f,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(rowHeight, 2f), new Vector2(300f, 18f), TextAlignmentOptions.BottomLeft);
            detail.color = new Color(0.75f, 0.85f, 1f);

            Button button = CreateButton(row, "Button", "", new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(120f, rowHeight - 12f));

            ItemRowView view = row.gameObject.AddComponent<ItemRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("detailText").objectReferenceValue = detail;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("buttonText").objectReferenceValue = button.GetComponentInChildren<TextMeshProUGUI>(true);
            so.ApplyModifiedPropertiesWithoutUndo();

            row.gameObject.SetActive(false);
            rows[i] = view;
        }
        return rows;
    }

    /// <summary>anchor 한 점 기준으로 붙는 버튼 (pivot = anchor).</summary>
    private static Button CreateButton(Transform parent, string objName, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rt = CreateRect(parent, objName, anchor, anchor, anchor, position, size);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.3f, 0.45f, 0.75f, 1f);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        CreateText(rt, "Label", label, 16f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        return button;
    }

    private static RectTransform CreateRect(Transform parent, string objName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private static void SetObjectArray(SerializedProperty array, Object[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void DestroyChild(Transform parent, string childName)
    {
        Transform old = parent.Find(childName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
    }

    private static void SetUILayer(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;   // UI
    }

    private static Transform FindHudPanel(GameObject[] roots)
    {
        GameObject canvas = roots.FirstOrDefault(r => r.name == "Canvas");
        return canvas != null
            ? canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "HUD_Panel")
            : null;
    }

    // ── 프리팹 ─────────────────────────────────────────────────────

    private static GameObject BuildEnemyPrefab(MonsterDef m)
    {
        var go = new GameObject("Enemy_" + m.name);
        go.layer = LayerEnemy;

        SpriteAnimator animator = AddAnimator(go, m.clips, 4);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = m.colliderSize;
        col.offset = new Vector2(0f, m.colliderCenterY);

        Enemy enemy = go.AddComponent<Enemy>();
        var so = new SerializedObject(enemy);
        so.FindProperty("data").objectReferenceValue = LoadData<EnemyStatsData>("EnemyStats_" + m.name);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("playerMask").intValue = 1 << LayerPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(go, "Enemy_" + m.name);
    }

    private static GameObject BuildWormPrefab()
    {
        ClipDef[] clips =
        {
            new ClipDef(AnimState.Idle, Worm + "Worm/Idle.png", 10f, true),
            new ClipDef(AnimState.Walk, Worm + "Worm/Walk.png", 10f, true),
            new ClipDef(AnimState.Attack, Worm + "Worm/Attack.png", 14f, false),
            new ClipDef(AnimState.Hit, Worm + "Worm/Get Hit.png", 10f, false),
            new ClipDef(AnimState.Death, Worm + "Worm/Death.png", 8f, false),
        };
        return BuildBossPrefab("Boss_FireWorm", "BossStats_FireWorm", clips, 2f,
            new Vector2(1.3f, 1.3f), 0.7f, new Vector2(1.4f, 1.4f));
    }

    private static GameObject BuildGolemPrefab()
    {
        // 한 장짜리 시트를 reslice할 때 Golem_<동작>_<번호>로 이름을 붙여 두었다. 걷기 프레임이 없어서 Idle로 대신 걷는다.
        ClipDef[] clips =
        {
            new ClipDef(AnimState.Idle, GolemSheet, 8f, true, "Golem_Idle_"),
            new ClipDef(AnimState.Walk, GolemSheet, 8f, true, "Golem_Idle_"),
            new ClipDef(AnimState.Attack, GolemSheet, 12f, false, "Golem_Shoot_"),
            new ClipDef(AnimState.Hit, GolemSheet, 16f, false, "Golem_Glow_"),
            new ClipDef(AnimState.Death, GolemSheet, 10f, false, "Golem_Death_"),
        };
        return BuildBossPrefab("Boss_Golem", "BossStats_Golem", clips, 2.5f,
            new Vector2(1.3f, 1.4f), 0.72f, new Vector2(2.2f, 1.9f));
    }

    private static GameObject BuildBossPrefab(string prefabName, string dataName, ClipDef[] clips, float scale,
        Vector2 colliderSize, float colliderCenterY, Vector2 fireOffset)
    {
        var go = new GameObject(prefabName);
        go.layer = LayerEnemy;
        go.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteAnimator animator = AddAnimator(go, clips, 4);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = colliderSize;
        col.offset = new Vector2(0f, colliderCenterY);

        BossController boss = go.AddComponent<BossController>();
        var so = new SerializedObject(boss);
        so.FindProperty("data").objectReferenceValue = LoadData<BossStatsData>(dataName);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("fireOffset").vector2Value = fireOffset;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(go, prefabName);
    }

    private static GameObject BuildFireballPrefab()
    {
        var go = new GameObject("Projectile_Fireball");
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        AddAnimator(go, new[] { new ClipDef(AnimState.Idle, Worm + "Fire Ball/Move.png", 12f, true) }, 6);
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.2f;
        AddProjectile(go);
        return SavePrefab(go, "Projectile_Fireball");
    }

    private static GameObject BuildGolemArmPrefab()
    {
        var go = new GameObject("Projectile_GolemArm");
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadFrames(GolemArm, null).FirstOrDefault();
        sr.sortingOrder = 6;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 0.4f);
        AddProjectile(go);
        return SavePrefab(go, "Projectile_GolemArm");
    }

    private static void AddProjectile(GameObject go)
    {
        Projectile projectile = go.AddComponent<Projectile>();
        var so = new SerializedObject(projectile);
        so.FindProperty("targetMask").intValue = 1 << LayerPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildPortalPrefab()
    {
        var go = new GameObject("Portal");
        go.transform.localScale = new Vector3(2f, 2f, 1f);

        // Warlock_skill3_frame1..8 — 파일마다 한 프레임
        var frames = new List<Sprite>();
        for (int i = 1; i <= 8; i++)
        {
            frames.AddRange(LoadFrames(PortalFrames + "Warlock_skill3_frame" + i + ".png", null));
        }

        SpriteAnimator animator = go.AddComponent<SpriteAnimator>();
        go.GetComponent<SpriteRenderer>().sortingOrder = 3;
        go.GetComponent<SpriteRenderer>().sprite = frames.FirstOrDefault();
        var so = new SerializedObject(animator);
        SerializedProperty clips = so.FindProperty("clips");
        clips.arraySize = 1;
        FillClip(clips.GetArrayElementAtIndex(0), AnimState.Idle, frames, 10f, true);
        so.ApplyModifiedPropertiesWithoutUndo();

        go.AddComponent<Portal>();
        return SavePrefab(go, "Portal");
    }

    /// <summary>한 번 재생하고 풀로 돌아가는 이펙트. 프레임은 Death 칸에 넣는다 (SkillEffect 참고).</summary>
    private static GameObject BuildEffectPrefab(string prefabName, List<Sprite> frames, float fps, float scale)
    {
        var go = new GameObject(prefabName);
        go.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteAnimator animator = go.AddComponent<SpriteAnimator>();
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 7;
        sr.sprite = frames.FirstOrDefault();

        var so = new SerializedObject(animator);
        SerializedProperty clips = so.FindProperty("clips");
        clips.arraySize = 1;
        FillClip(clips.GetArrayElementAtIndex(0), AnimState.Death, frames, fps, false);
        so.FindProperty("defaultState").enumValueIndex = (int)AnimState.Death;
        so.FindProperty("spriteFacesRight").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        SkillEffect effect = go.AddComponent<SkillEffect>();
        var effectSo = new SerializedObject(effect);
        effectSo.FindProperty("animator").objectReferenceValue = animator;
        effectSo.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(go, prefabName);
    }

    /// <summary>상인. 에셋에 NPC 스프라이트가 없어서 Archer를 쓴다. 머리 위에 "Shop" 글자를 띄운다.</summary>
    private static GameObject BuildShopNpcPrefab()
    {
        var go = new GameObject("ShopNPC");
        AddAnimator(go, new[] { new ClipDef(AnimState.Idle, ShopkeeperIdle, 6f, true) }, 3);

        var label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);
        label.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.text = "Shop  [Up]";
        text.fontSize = 3f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.85f, 0.3f);
        text.sortingOrder = 8;
        text.rectTransform.sizeDelta = new Vector2(4f, 1f);

        go.AddComponent<ShopNPC>();
        return SavePrefab(go, "ShopNPC");
    }

    /// <summary>퀘스트 NPC. 에셋에 NPC 스프라이트가 없어서 Archer를 푸르게 칠해 상인과 구분한다.</summary>
    private static GameObject BuildQuestNpcPrefab()
    {
        var go = new GameObject("QuestNPC");
        AddAnimator(go, new[] { new ClipDef(AnimState.Idle, ShopkeeperIdle, 6f, true) }, 3);
        go.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.8f, 1f);

        TextMeshPro nameText = CreateWorldText(go.transform, "NameLabel", "NPC  [Up]", 3f, new Vector3(0f, 1.5f, 0f), new Color(0.6f, 0.85f, 1f));
        TextMeshPro marker = CreateWorldText(go.transform, "Marker", "!", 6f, new Vector3(0f, 2.2f, 0f), new Color(1f, 0.85f, 0.3f));

        QuestNPC npc = go.AddComponent<QuestNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("marker").objectReferenceValue = marker;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(go, "QuestNPC");
    }

    private static TextMeshPro CreateWorldText(Transform parent, string objName, string text, float fontSize, Vector3 localPos, Color color)
    {
        var label = new GameObject(objName);
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPos;
        TextMeshPro tmp = label.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 8;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
        return tmp;
    }

    private static GameObject BuildSwordWavePrefab()
    {
        var go = new GameObject("Projectile_SwordWave");
        go.layer = LayerDefault;
        // 파동은 256px 칸의 오른쪽 절반에 그려져 있다. 마지막 두 프레임만 반복한다.
        AddAnimator(go, new[] { new ClipDef(AnimState.Idle, null, 10f, true) }, 7, NumberedFrames(WaveFrames, 13, 14));

        // 적 콜라이더는 Rigidbody 없는 트리거라, 이쪽에 Kinematic Rigidbody가 있어야 트리거가 성립한다.
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.1f, 1.0f);
        col.offset = new Vector2(0.5f, 0f);

        Projectile projectile = go.AddComponent<Projectile>();
        var so = new SerializedObject(projectile);
        so.FindProperty("targetMask").intValue = 1 << LayerEnemy;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(go, "Projectile_SwordWave");
    }

    /// <summary>파일마다 한 프레임씩 나뉜 이펙트 (…frame1.png ~ …frameN.png).</summary>
    private static List<Sprite> NumberedFrames(string pathPrefix, int from, int to)
    {
        var frames = new List<Sprite>();
        for (int i = from; i <= to; i++) frames.AddRange(LoadFrames(pathPrefix + i + ".png", null));
        if (frames.Count == 0) Debug.LogError($"[RPGWorldBuilder] 프레임 없음: {pathPrefix}{from}~{to}");
        return frames;
    }

    // ── 공용 도우미 ─────────────────────────────────────────────────

    private static SpriteAnimator AddAnimator(GameObject go, ClipDef[] defs, int sortingOrder, List<Sprite> explicitFrames = null)
    {
        SpriteAnimator animator = go.AddComponent<SpriteAnimator>();   // SpriteRenderer는 RequireComponent로 같이 붙는다
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;

        var so = new SerializedObject(animator);
        SerializedProperty clips = so.FindProperty("clips");
        clips.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++)
        {
            List<Sprite> frames = explicitFrames ?? LoadFrames(defs[i].path, defs[i].prefix);
            if (frames.Count == 0) Debug.LogError($"[RPGWorldBuilder] 프레임 없음: {defs[i].path} {defs[i].prefix}");
            FillClip(clips.GetArrayElementAtIndex(i), defs[i].state, frames, defs[i].fps, defs[i].loop);
            if (i == 0) sr.sprite = frames.FirstOrDefault();
        }
        so.FindProperty("spriteFacesRight").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        return animator;
    }

    private static void FillClip(SerializedProperty clip, AnimState state, List<Sprite> frames, float fps, bool loop)
    {
        clip.FindPropertyRelative("state").enumValueIndex = (int)state;
        clip.FindPropertyRelative("framesPerSecond").floatValue = fps;
        clip.FindPropertyRelative("loop").boolValue = loop;
        SerializedProperty arr = clip.FindPropertyRelative("frames");
        arr.arraySize = frames.Count;
        for (int i = 0; i < frames.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
    }

    /// <summary>시트의 스프라이트를 이름 끝 숫자 순으로. 이름순이면 _10이 _2보다 앞에 온다.</summary>
    private static List<Sprite> LoadFrames(string path, string prefix)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(s => prefix == null || s.name.StartsWith(prefix))
            .OrderBy(s => TrailingNumber(s.name))
            .ToList();
    }

    private static int TrailingNumber(string name)
    {
        Match m = Regex.Match(name, @"(\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    private static T LoadData<T>(string assetName) where T : ScriptableObject
    {
        var data = AssetDatabase.LoadAssetAtPath<T>($"{DataDir}/{assetName}.asset");
        if (data == null) Debug.LogError($"[RPGWorldBuilder] 스탯 에셋 없음: {DataDir}/{assetName}.asset");
        return data;
    }

    private static GameObject SavePrefab(GameObject go, string prefabName)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{prefabName}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
