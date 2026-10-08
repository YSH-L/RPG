using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 소리: Audio/ 폴더의 클립으로 Data/Sounds.asset을 만들고, 구역 BGM과 각 스크립트에 연결한다.
/// <b>RPG &gt; Build Game</b>이 마지막에 부르고, 지금 씬에만 넣고 싶으면 <b>RPG &gt; Apply Sounds (current scene)</b>.
/// 이미 들어 있는 칸은 덮어쓰지 않는다 — 사용자가 바꿔 끼운 소리가 날아가지 않게 하기 위해서다.
/// 출처: BGM은 Juhani Junkala "5 Chiptunes (Action)", 효과음은 Kenney 오디오 팩. 둘 다 CC0.
/// </summary>
public static partial class RPGBuilder
{
    private const string AudioDir = Game + "/Audio";

    [MenuItem("RPG/Apply Sounds (current scene)")]
    public static void ApplySoundsToOpenScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[RPGBuilder] 플레이 모드에서는 적용할 수 없습니다.");
            return;
        }

        GameObject systems = GameObject.Find("GameSystems");
        if (systems == null)
        {
            Debug.LogError("[RPGBuilder] 씬에 GameSystems가 있어야 합니다. RPG > Build Game을 먼저 돌리세요.");
            return;
        }

        BuildSounds(systems.transform);
        EditorSceneManager.MarkSceneDirty(systems.scene);
        EditorSceneManager.SaveScene(systems.scene);
        Debug.Log("[RPGBuilder] 소리를 넣었습니다.");
    }

    private static void BuildSounds(Transform systems)
    {
        ConfigureBgmImport();
        SoundSet sounds = BuildSoundSet();

        // 구역 BGM: 마을·시장 / 사냥터 / 보스방
        AudioClip town = Clip("BGM/Town.wav");
        AudioClip field = Clip("BGM/Field.wav");
        AudioClip boss = Clip("BGM/Boss.wav");
        foreach (Area area in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(area);
            SerializedProperty bgm = so.FindProperty("bgm");
            if (bgm.objectReferenceValue != null) continue;
            bool isTown = area.name == "Area_Town" || area.name == "Area_Shop";
            bool isBoss = area.name.StartsWith("Area_Boss");
            bgm.objectReferenceValue = isTown ? town : isBoss ? boss : field;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 각 스크립트에 같은 SoundSet을 물린다.
        foreach (var c in Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Set(c, ("sounds", sounds));
        foreach (var c in Object.FindObjectsByType<ShopWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Set(c, ("sounds", sounds));
        foreach (var c in Object.FindObjectsByType<TitleIntro>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Set(c, ("sounds", sounds));
        foreach (var c in Object.FindObjectsByType<CharacterSelect>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Set(c, ("sounds", sounds));

        var audio = systems.GetComponent<GameAudio>();
        if (audio == null) audio = systems.gameObject.AddComponent<GameAudio>();
        Set(audio, ("sounds", sounds),
            ("areaManager", Object.FindAnyObjectByType<AreaManager>(FindObjectsInactive.Include)),
            ("player", Object.FindAnyObjectByType<PlayerStats>(FindObjectsInactive.Include)),
            ("gameFlow", Object.FindAnyObjectByType<GameFlow>(FindObjectsInactive.Include)));
    }

    /// <summary>처음 만들 때만 칸을 채운다. 이미 있으면 비어 있는 칸만 채운다.</summary>
    private static SoundSet BuildSoundSet()
    {
        string path = $"{DataDir}/Sounds.asset";
        var sounds = AssetDatabase.LoadAssetAtPath<SoundSet>(path);
        if (sounds == null)
        {
            sounds = ScriptableObject.CreateInstance<SoundSet>();
            AssetDatabase.CreateAsset(sounds, path);
        }

        var so = new SerializedObject(sounds);
        void Fill(string field, string file)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p.objectReferenceValue == null) p.objectReferenceValue = Clip(file);
        }

        Fill("titleBgm", "BGM/Title.wav");
        Fill("victoryBgm", "BGM/Victory.wav");
        Fill("gameOver", "SFX/GameOver.ogg");
        Fill("swordSwing", "SFX/SwordSwing.ogg");
        Fill("arrowShot", "SFX/ArrowShot.ogg");
        Fill("skillSlash", "SFX/SkillSlash.ogg");
        Fill("skillVolley", "SFX/SkillVolley.ogg");
        Fill("guard", "SFX/Guard.ogg");
        Fill("dodge", "SFX/Dodge.ogg");
        Fill("jump", "SFX/Jump.ogg");
        Fill("playerHurt", "SFX/PlayerHurt.ogg");
        Fill("potion", "SFX/Potion.ogg");
        Fill("levelUp", "SFX/LevelUp.ogg");
        Fill("enemyHit", "SFX/EnemyHit.ogg");
        Fill("enemyDeath", "SFX/EnemyDeath.ogg");
        Fill("bossDeath", "SFX/BossDeath.ogg");
        Fill("portal", "SFX/Portal.ogg");
        Fill("shopOpen", "SFX/ShopOpen.ogg");
        Fill("shopClose", "SFX/ShopClose.ogg");
        Fill("shopMove", "SFX/ShopMove.ogg");
        Fill("shopBuy", "SFX/ShopBuy.ogg");
        Fill("shopFail", "SFX/ShopFail.ogg");
        Fill("menuMove", "SFX/MenuMove.ogg");
        Fill("menuConfirm", "SFX/MenuConfirm.ogg");
        Fill("gameStart", "SFX/GameStart.ogg");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sounds);
        AssetDatabase.SaveAssets();
        return sounds;
    }

    /// <summary>BGM은 길어서 통째로 메모리에 올리지 않고 흘려 읽는다. 빌드에서는 Vorbis로 압축된다.</summary>
    private static void ConfigureBgmImport()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { $"{AudioDir}/BGM" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming && settings.compressionFormat == AudioCompressionFormat.Vorbis) continue;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }

    private static AudioClip Clip(string file)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{file}");
        if (clip == null) Debug.LogWarning($"[RPGBuilder] {AudioDir}/{file}가 없어 그 소리는 비워 둡니다.");
        return clip;
    }
}
