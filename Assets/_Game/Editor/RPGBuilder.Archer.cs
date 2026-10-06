using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 두 번째 플레이어 캐릭터 Archer: 수치 에셋(Data/ArcherStats), 화살 프리팹·풀, 플레이어에 붙는 두 번째 애니메이터,
/// 시작 화면의 캐릭터 선택. <b>RPG &gt; Build Game</b>이 마지막에 부르고,
/// 지금 씬에만 넣고 싶으면 <b>RPG &gt; Apply Archer (current scene)</b>. 여러 번 돌려도 된다.
/// </summary>
public static partial class RPGBuilder
{
    private const string GuideBase =
        "Press SPACE to Start\n<size=60%>LEFT/RIGHT move   SPACE jump   Z attack   A/S skill\nUP portal/talk   1,2 potion</size>";

    [MenuItem("RPG/Apply Archer (current scene)")]
    public static void ApplyArcherToOpenScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[RPGBuilder] 플레이 모드에서는 적용할 수 없습니다.");
            return;
        }

        PlayerController player = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        GameObject systems = GameObject.Find("GameSystems");
        Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (player == null || systems == null || canvas == null)
        {
            Debug.LogError("[RPGBuilder] 씬에 Player·GameSystems·Canvas가 있어야 합니다. RPG > Build Game을 먼저 돌리세요.");
            return;
        }

        BuildArcher(player, systems.transform, canvas.transform);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        Debug.Log("[RPGBuilder] Archer를 넣었습니다.");
    }

    private static void BuildArcher(PlayerController player, Transform systems, Transform canvas)
    {
        // 수치: 처음 만들 때만 Swordsman 에셋을 복사한 뒤 원거리용으로 고친다.
        var swordsmanStats = player.GetComponent<PlayerStats>().Data;
        var archerStats = LoadOrCreate<PlayerStatsData>($"{DataDir}/ArcherStats.asset", out bool created);
        if (created)
        {
            EditorUtility.CopySerialized(swordsmanStats, archerStats);
            archerStats.name = "ArcherStats";
            archerStats.baseMaxHP = 80;
            archerStats.hpPerLevel = 16;
            archerStats.baseAttack = 9;
            archerStats.attackPerLevel = 3;
            archerStats.moveSpeed = 4.3f;
            archerStats.attackCooldown = 0.5f;
            archerStats.attackHitDelay = 0.21f;      // atk 5프레임 14fps, 시위를 놓는 4번째 프레임
            archerStats.slashCooldown = 4f;
            archerStats.slashHitDelay = 0.17f;       // 난사는 18fps로 더 빨리 당긴다
            archerStats.slashDamageMultiplier = 0.8f; // 화살 한 발마다
            archerStats.guardDuration = 0.35f;        // 회피: 0.35초 동안 무적으로 뒤로 물러난다
            archerStats.guardCooldown = 3f;
            archerStats.guardDashSpeed = 9f;
            archerStats.ranged = true;
            archerStats.arrowSpeed = 14f;
            archerStats.arrowRange = 9f;
            archerStats.volleyCount = 5;
            archerStats.volleySpread = 10f;
        }
        EditorUtility.SetDirty(archerStats);

        // 화살 프리팹과 풀
        GameObject arrowPrefab = SavePlayerArrowPrefab();
        Transform poolTransform = systems.Find("Pool_PlayerArrow");
        ObjectPool arrowPool = poolTransform != null ? poolTransform.GetComponent<ObjectPool>() : MakePool(systems, "Pool_PlayerArrow", arrowPrefab, 10);
        Set(arrowPool, ("prefab", arrowPrefab), ("initialSize", 10), ("expandable", true));

        // 두 번째 애니메이터 (꺼 둔다. CharacterSelect가 고를 때 켠다)
        var controllerSo = new SerializedObject(player);
        var swordsmanAnimator = (SpriteAnimator)controllerSo.FindProperty("animator").objectReferenceValue;
        SpriteAnimator archerAnimator = player.GetComponents<SpriteAnimator>().FirstOrDefault(a => a != swordsmanAnimator);
        if (archerAnimator == null) archerAnimator = player.gameObject.AddComponent<SpriteAnimator>();
        Sprite[] walk = CharacterSheet($"{ArcherDir}/Archer-walk-spritesheet.png");
        Sprite[] atk = CharacterSheet($"{ArcherDir}/Archer-atk-spritesheet.png");
        SetClips(archerAnimator, AnimState.Idle, AnimState.Idle,
            new ClipDef(AnimState.Idle, CharacterSheet($"{ArcherDir}/Archer-Idle-spritesheet.png"), 8f, true),
            new ClipDef(AnimState.Walk, walk, 10f, true),
            new ClipDef(AnimState.Jump, new[] { walk[Mathf.Min(2, walk.Length - 1)] }, 1f, true),
            new ClipDef(AnimState.Attack, atk, 14f, false),
            new ClipDef(AnimState.Hit, CharacterSheet($"{ArcherDir}/Archer-hit-spritesheet.png"), 14f, false),
            new ClipDef(AnimState.Skill, atk, 18f, false),
            // Archer에는 방어 모션이 없다. 회피하는 동안은 March(행진) 모션을 반투명으로 보여 준다.
            new ClipDef(AnimState.Block, CharacterSheet($"{ArcherDir}/Archer-March-spritesheet.png"), 14f, true));
        archerAnimator.enabled = false;

        Set(player, ("arrowPool", arrowPool));

        // 시작 화면 안내 글과 캐릭터 선택
        TMP_Text guide = canvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "GuideText");
        if (guide != null)
        {
            guide.text = GuideBase;
            EditorUtility.SetDirty(guide);
        }

        var select = systems.GetComponent<CharacterSelect>();
        if (select == null) select = systems.gameObject.AddComponent<CharacterSelect>();
        var so = new SerializedObject(select);
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("guideText").objectReferenceValue = guide;
        SerializedProperty profiles = so.FindProperty("profiles");
        profiles.arraySize = 2;
        SetProfile(profiles.GetArrayElementAtIndex(0), "Swordsman", swordsmanStats, swordsmanAnimator);
        SetProfile(profiles.GetArrayElementAtIndex(1), "Archer", archerStats, archerAnimator);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProfile(SerializedProperty profile, string displayName, PlayerStatsData stats, SpriteAnimator animator)
    {
        profile.FindPropertyRelative("displayName").stringValue = displayName;
        profile.FindPropertyRelative("stats").objectReferenceValue = stats;
        profile.FindPropertyRelative("animator").objectReferenceValue = animator;
    }

    private static GameObject SavePlayerArrowPrefab()
    {
        Sprite sprite = SpritesIn($"{ArcherDir}/Arrow.png").OrderByDescending(s => s.rect.width).First();   // 긴 쪽이 화살 전체

        var go = new GameObject("PlayerArrow");
        go.transform.localScale = new Vector3(2.5f, 2.5f, 1f);   // 32px @ PPU100 = 0.32 → 0.8유닛
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 15;
        var loop = go.AddComponent<LoopBody>();
        go.AddComponent<WrapGhost>();
        var arrow = go.AddComponent<PlayerArrow>();
        Set(arrow, ("spriteRenderer", renderer), ("loopBody", loop), ("hitSize", new Vector2(0.5f, 0.3f)));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/PlayerArrow.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }
}
