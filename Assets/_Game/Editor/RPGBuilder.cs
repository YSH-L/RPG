using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 메뉴 <b>RPG &gt; Build Game</b> 한 번으로 스프라이트 임포트 설정, 데이터 에셋, 프리팹, 씬 배치를 만든다.
/// 다시 돌려도 된다 — 씬에서 자기가 만든 오브젝트(World, Player, GameSystems, UI 몇 개)를 지우고 다시 만든다.
/// 데이터 에셋(Data/)은 이미 있으면 숫자를 덮어쓰지 않는다. 밸런스를 고친 것이 날아가지 않게 하기 위해서다.
/// </summary>
public static partial class RPGBuilder
{
    private const string Game = "Assets/_Game";
    private const string DataDir = Game + "/Data";
    private const string PrefabDir = Game + "/Prefabs";
    private const string ScenePath = "Assets/Scenes/RPG.unity";
    private const string Sprites = "Assets/Sprites";
    private const string Fantasy1 = Sprites + "/Enemy/Normal/Monsters Creatures Fantasy/Sprites";
    private const string Fantasy2 = Sprites + "/Enemy/Normal/Monsters Creatures Fantasy 2/Sprites";
    private const string WormDir = Sprites + "/Enemy/Boss/Fire Worm/Sprites";
    private const string GolemSheet = Sprites + "/Enemy/Boss/Mecha-stone Golem 0.1/PNG sheet/Character_sheet.png";
    private const string GolemArm = Sprites + "/Enemy/Boss/Mecha-stone Golem 0.1/weapon PNG/arm_projectile.png";
    private const string TilesDir = Sprites + "/Tiles/Tiles";
    private const string TileAtlas = TilesDir + "/Assets/Assets.png";
    private const string SwordsmanDir = Sprites + "/Character/Swordsman";
    private const string ArcherDir = Sprites + "/Character/Archer";
    private const string IconsDir = Sprites + "/16x16 Assorted RPG Icons";
    private const string PortalFrames = Sprites + "/Skills/Warlock/VFX1/Frames";
    private const string ImpactFrames = Sprites + "/Skills/Impacts/VFX3/COLOR/Frames";
    private const string FontDir = "Assets/TextMesh Pro/Resources/Fonts & Materials";

    private const int GroundLayer = 8;
    private const int PlayerLayer = 9;
    private const int EnemyLayer = 10;

    /// <summary>하늘 이미지(496px @ 32PPU) 한 장 폭. 공간 폭은 이것과 배경 반복 폭의 배수여야 이음새가 없다.</summary>
    private const float SkyTile = 15.5f;
    /// <summary>Social 장면(1984px @ 128PPU) 원본 + 좌우반전 한 쌍의 폭.</summary>
    private const float BackdropPeriod = 31f;
    private const float ViewHeight = 8.5f;
    private const float CameraHeight = 2.75f;
    private const float AreaSpacingY = 40f;

    // ───────────────────────────── 메뉴 ─────────────────────────────

    [MenuItem("RPG/Build Game")]
    public static void BuildGame()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[RPGBuilder] 플레이 모드에서는 빌드할 수 없습니다.");
            return;
        }

        ConfigureSprites();
        BuildPrefabs(BuildData());

        // 씬을 열면 쓰지 않는 에셋이 언로드되어 앞에서 들고 있던 참조가 죽는다. 씬을 연 뒤 경로로 다시 읽는다.
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildScene(scene, BuildData(), LoadPrefabs());
        AssetDatabase.SaveAssets();
        Debug.Log("[RPGBuilder] 완료. RPG.unity를 저장했습니다.");
    }

    [MenuItem("RPG/Configure Sprites Only")]
    public static void ConfigureSpritesMenu()
    {
        ConfigureSprites();
        AssetDatabase.SaveAssets();
    }

    // ───────────────────────────── 몬스터 표 ─────────────────────────────

    private class MonsterDef
    {
        public string key, display, dir, idle, walk, attack, hit, death;
        public bool flying;
        public int dungeonLevel;
        public string backdrop;
        public Color tint = Color.white;
        // hp, contact, attack, range, speed, exp, goldMin, goldMax
        public float[] stats;
    }

    private static readonly MonsterDef[] Monsters =
    {
        new MonsterDef { key = "Slime", display = "Slime", dir = Fantasy2 + "/Slime", idle = "idle", walk = "walk", attack = "attack", hit = "hurt", death = "death",
            dungeonLevel = 1, backdrop = "trees", stats = new float[] { 30, 5, 7, 0.9f, 1.0f, 5, 3, 6 } },
        new MonsterDef { key = "Rat", display = "Rat", dir = Fantasy2 + "/Rat", idle = "idle", walk = "run", attack = "attack_bite", hit = "hurt", death = "rat-death",
            dungeonLevel = 3, backdrop = "trees", tint = new Color(0.95f, 0.9f, 0.8f), stats = new float[] { 55, 9, 12, 0.8f, 1.8f, 9, 5, 10 } },
        new MonsterDef { key = "Bat", display = "Bat", dir = Fantasy2 + "/Bat", idle = "fly", walk = "fly", attack = "attack", hit = "hurt", death = "death", flying = true,
            dungeonLevel = 5, backdrop = "Mine", stats = new float[] { 80, 16, 20, 0.9f, 1.6f, 15, 8, 14 } },
        new MonsterDef { key = "Mushroom", display = "Mushroom", dir = Fantasy1 + "/Mushroom", idle = "Idle", walk = "Run", attack = "Attack1", hit = "Take Hit", death = "Death",
            dungeonLevel = 7, backdrop = "trees", tint = new Color(0.85f, 0.95f, 0.85f), stats = new float[] { 120, 22, 28, 0.9f, 1.2f, 24, 12, 20 } },
        new MonsterDef { key = "Goblin", display = "Goblin", dir = Fantasy1 + "/Goblin", idle = "Idle", walk = "Run", attack = "Attack1", hit = "Take Hit", death = "Death",
            dungeonLevel = 9, backdrop = "trees", tint = new Color(1f, 0.85f, 0.7f), stats = new float[] { 170, 30, 38, 1.0f, 2.0f, 38, 18, 30 } },
        new MonsterDef { key = "FlyingEye", display = "Flying Eye", dir = Fantasy1 + "/Flying eye", idle = "Flight", walk = "Flight", attack = "Attack1", hit = "Take Hit", death = "Death", flying = true,
            dungeonLevel = 11, backdrop = "Mine", tint = new Color(0.85f, 0.8f, 1f), stats = new float[] { 230, 38, 46, 1.0f, 1.8f, 60, 25, 40 } },
        new MonsterDef { key = "Skeleton", display = "Skeleton", dir = Fantasy1 + "/Skeleton", idle = "Idle", walk = "Walk", attack = "Attack1", hit = "Take Hit", death = "Death",
            dungeonLevel = 13, backdrop = "Mine", tint = new Color(0.8f, 0.85f, 0.95f), stats = new float[] { 320, 48, 58, 1.1f, 1.2f, 95, 35, 55 } },
        new MonsterDef { key = "Mimic", display = "Mimic", dir = Fantasy2 + "/Mimic", idle = "idle_transformed", walk = "walk", attack = "attack_1", hit = "hurt", death = "death",
            dungeonLevel = 15, backdrop = "Mine", tint = new Color(1f, 0.8f, 0.8f), stats = new float[] { 420, 60, 72, 1.1f, 1.4f, 150, 50, 80 } },
    };

    private static string Sheet(MonsterDef m, string anim) => $"{m.dir}/{anim}.png";

    // ───────────────────────────── 스프라이트 임포트 ─────────────────────────────

    private static void ConfigureSprites()
    {
        // 일반 몬스터: 격자는 이미 맞다. PPU 32와 발바닥 피벗만 맞춘다.
        foreach (MonsterDef m in Monsters)
        {
            string[] sheets = Directory.GetFiles(m.dir, "*.png").Select(p => p.Replace('\\', '/')).ToArray();
            Vector2 pivot = m.flying ? new Vector2(0.5f, 0.5f) : FootPivot(Sheet(m, m.idle), null);
            foreach (string sheet in sheets) SetPivots(sheet, 32f, pivot);
        }

        // Fire Worm: 자동 슬라이스가 들쭉날쭉해서 90px 격자로 다시 자른다.
        foreach (string anim in new[] { "Idle", "Walk", "Attack", "Get Hit", "Death" })
        {
            GridSlice($"{WormDir}/Worm/{anim}.png", 90, 90, "Worm_" + anim.Replace(" ", ""), 16f, null);
        }
        Vector2 wormPivot = FootPivot($"{WormDir}/Worm/Idle.png", null);
        foreach (string anim in new[] { "Idle", "Walk", "Attack", "Get Hit", "Death" }) SetPivots($"{WormDir}/Worm/{anim}.png", 16f, wormPivot);
        GridSlice($"{WormDir}/Fire Ball/Move.png", 46, 46, "FireBall_Move", 32f, new Vector2(0.5f, 0.5f));
        GridSlice($"{WormDir}/Fire Ball/Explosion.png", 46, 46, "FireBall_Explosion", 32f, new Vector2(0.5f, 0.5f));

        // Golem: 한 장에 동작이 줄마다 뭉쳐 있다. 100px 격자로 자르고 이름에 줄·칸 번호를 넣는다.
        GolemSlice();
        SetImportBasics(GolemArm, 40f);

        // 방어구 아이콘은 통짜 한 장이라 16px 격자로 자른다.
        GridSliceNamed($"{IconsDir}/armours.png", 16, 16, (r, c) => $"armours_r{r}_c{c}", 100f, new Vector2(0.5f, 0.5f));

        // 배경: 지면 띠와 흙 채움 조각을 기존 조각 옆에 덧붙인다(기존 30개는 그대로 둔다).
        AddTileSlices();
        foreach (string bg in new[] { "Background_1", "Background_2" })
        {
            SetImportBasics($"{TilesDir}/Assets/{bg}.png", 32f, fullRect: true);
        }
        foreach (string scene in new[] { "entrance", "Mine", "trees" })
        {
            SetImportBasics($"{TilesDir}/Social/{scene}.png", 128f, fullRect: true);
        }

        AssetDatabase.Refresh();
    }

    private static TextureImporter Importer(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception($"[RPGBuilder] 텍스처를 찾을 수 없습니다: {path}");
        return importer;
    }

    private static ISpriteEditorDataProvider Provider(TextureImporter importer)
    {
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        return provider;
    }

    private static void SetImportBasics(string path, float ppu, bool fullRect = false)
    {
        TextureImporter importer = Importer(path);
        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, ppu)) { importer.spritePixelsPerUnit = ppu; dirty = true; }
        if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }

        if (fullRect)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                dirty = true;
            }
        }

        if (dirty) importer.SaveAndReimport();
    }

    private static void EnsureMultiple(string path)
    {
        TextureImporter importer = Importer(path);
        if (importer.spriteImportMode == SpriteImportMode.Multiple) return;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();
    }

    /// <summary>기존 조각은 그대로 두고 PPU와 피벗만 바꾼다. 조각 ID가 유지되어 다른 참조가 깨지지 않는다.</summary>
    private static void SetPivots(string path, float ppu, Vector2 pivot)
    {
        SetImportBasics(path, ppu);
        TextureImporter importer = Importer(path);
        ISpriteEditorDataProvider provider = Provider(importer);
        SpriteRect[] rects = provider.GetSpriteRects();

        bool changed = false;
        foreach (SpriteRect rect in rects)
        {
            if (rect.alignment == SpriteAlignment.Custom && Vector2.Distance(rect.pivot, pivot) < 0.0001f) continue;
            rect.alignment = SpriteAlignment.Custom;
            rect.pivot = pivot;
            changed = true;
        }
        if (!changed) return;

        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void GridSlice(string path, int cellW, int cellH, string prefix, float ppu, Vector2? pivot)
    {
        GridSliceNamed(path, cellW, cellH, (r, c) => $"{prefix}_{c}", ppu, pivot ?? new Vector2(0.5f, 0f));
    }

    /// <summary>
    /// 빈 칸은 건너뛰고 격자로 다시 자른다. 이름이 같은 조각이 이미 있으면 그 ID를 이어서 쓴다.
    /// </summary>
    private static void GridSliceNamed(string path, int cellW, int cellH, Func<int, int, string> nameOf, float ppu, Vector2 pivot)
    {
        EnsureMultiple(path);
        SetImportBasics(path, ppu);

        Texture2D pixels = LoadPixels(path);
        int cols = pixels.width / cellW;
        int rows = pixels.height / cellH;
        Color32[] block = pixels.GetPixels32();

        TextureImporter importer = Importer(path);
        ISpriteEditorDataProvider provider = Provider(importer);
        Dictionary<string, SpriteRect> existing = provider.GetSpriteRects().GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First());

        var rects = new List<SpriteRect>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var cell = new RectInt(col * cellW, pixels.height - (row + 1) * cellH, cellW, cellH);
                if (IsEmpty(block, pixels.width, cell)) continue;

                string name = nameOf(row, col);
                var rect = new SpriteRect
                {
                    name = name,
                    rect = new Rect(cell.x, cell.y, cell.width, cell.height),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = existing.TryGetValue(name, out SpriteRect old) ? old.spriteID : GUID.Generate()
                };
                rects.Add(rect);
            }
        }
        Object.DestroyImmediate(pixels);

        bool same = existing.Count == rects.Count && rects.All(r => existing.TryGetValue(r.name, out SpriteRect o) && o.rect == r.rect);
        if (same) return;

        provider.SetSpriteRects(rects.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void GolemSlice()
    {
        GridSliceNamed(GolemSheet, 100, 100, (r, c) => $"Golem_{r}_{c}", 20f, new Vector2(0.5f, 0f));
        Vector2 pivot = FootPivot(GolemSheet, s => s.name.StartsWith("Golem_0_"));
        SetPivots(GolemSheet, 20f, pivot);
    }

    private static void AddTileSlices()
    {
        EnsureMultiple(TileAtlas);
        SetImportBasics(TileAtlas, 32f, fullRect: true);

        TextureImporter importer = Importer(TileAtlas);
        ISpriteEditorDataProvider provider = Provider(importer);
        List<SpriteRect> rects = provider.GetSpriteRects().ToList();

        // 400px 텍스처, 좌하단 원점. 잔디 띠: 위에서 0~32px, 좌우 이음새 없이 반복되는 16px 폭.
        var wanted = new[]
        {
            ("Ground_Top", new Rect(47, 400 - 32, 16, 32)),
            ("Ground_Fill", new Rect(27, 400 - 48, 16, 16)),
        };

        bool changed = false;
        foreach ((string name, Rect area) in wanted)
        {
            if (rects.Any(r => r.name == name)) continue;
            rects.Add(new SpriteRect { name = name, rect = area, alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f), spriteID = GUID.Generate() });
            changed = true;
        }
        if (!changed) return;

        provider.SetSpriteRects(rects.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Texture2D LoadPixels(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }

    private static bool IsEmpty(Color32[] block, int width, RectInt cell)
    {
        for (int y = cell.y; y < cell.yMax; y++)
        {
            for (int x = cell.x; x < cell.xMax; x++)
            {
                if (block[y * width + x].a > 10) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 시트의 모든 조각에서 가장 낮은 불투명 픽셀 높이를 찾아 그 높이를 피벗 y로 쓴다.
    /// 이렇게 해야 transform.position이 발바닥이 되고, 동작이 바뀌어도 발이 뜨지 않는다.
    /// </summary>
    private static Vector2 FootPivot(string path, Func<SpriteRect, bool> filter)
    {
        TextureImporter importer = Importer(path);
        SpriteRect[] rects = Provider(importer).GetSpriteRects();
        if (filter != null) rects = rects.Where(filter).ToArray();
        if (rects.Length == 0) return new Vector2(0.5f, 0f);

        Texture2D pixels = LoadPixels(path);
        Color32[] data = pixels.GetPixels32();
        int width = pixels.width;

        float best = 1f;
        foreach (SpriteRect sprite in rects)
        {
            Rect r = sprite.rect;
            for (int y = (int)r.y; y < (int)r.yMax; y++)
            {
                bool found = false;
                for (int x = (int)r.x; x < (int)r.xMax; x++)
                {
                    if (data[y * width + x].a > 10) { found = true; break; }
                }
                if (!found) continue;
                best = Mathf.Min(best, (y - r.y) / r.height);
                break;
            }
        }
        Object.DestroyImmediate(pixels);
        return new Vector2(0.5f, best);
    }

    // ───────────────────────────── 스프라이트 읽기 ─────────────────────────────

    private static Sprite[] SpritesIn(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
    }

    /// <summary>가로 한 줄짜리 시트. 왼쪽부터.</summary>
    private static Sprite[] Strip(string path)
    {
        Sprite[] sprites = SpritesIn(path).OrderBy(s => s.rect.x).ToArray();
        if (sprites.Length == 0) throw new Exception($"[RPGBuilder] 스프라이트가 없습니다: {path}");
        return sprites;
    }

    /// <summary>캐릭터 시트(38px 줄). 위 줄부터, 줄 안에서는 왼쪽부터. 이름순 정렬은 순서가 틀린다.</summary>
    private static Sprite[] CharacterSheet(string path)
    {
        return SpritesIn(path)
            .OrderByDescending(s => Mathf.FloorToInt((s.rect.y + s.rect.height * 0.5f) / 38f))
            .ThenBy(s => s.rect.x)
            .ToArray();
    }

    private static Sprite[] GolemRow(int row, int from = 0, int count = 99)
    {
        return SpritesIn(GolemSheet)
            .Where(s => s.name.StartsWith($"Golem_{row}_"))
            .OrderBy(s => s.rect.x)
            .Skip(from).Take(count).ToArray();
    }

    /// <summary>개별 PNG로 나뉜 프레임 폴더. 파일 이름 끝 숫자 순서로.</summary>
    private static Sprite[] FrameFolder(string dir)
    {
        return Directory.GetFiles(dir, "*.png")
            .Select(p => p.Replace('\\', '/'))
            .OrderBy(p => TrailingNumber(Path.GetFileNameWithoutExtension(p)))
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
            .Where(s => s != null)
            .ToArray();
    }

    private static int TrailingNumber(string name)
    {
        int end = name.Length;
        int start = end;
        while (start > 0 && char.IsDigit(name[start - 1])) start--;
        return start < end ? int.Parse(name.Substring(start, end - start)) : 0;
    }

    private static Sprite NamedSprite(string path, string name)
    {
        Sprite sprite = SpritesIn(path).FirstOrDefault(s => s.name == name);
        if (sprite == null) Debug.LogWarning($"[RPGBuilder] {path}에 '{name}' 조각이 없습니다.");
        return sprite;
    }

    // ───────────────────────────── 데이터 에셋 ─────────────────────────────

    private class Catalog
    {
        public PlayerStatsData player;
        public readonly Dictionary<string, EnemyStatsData> enemies = new Dictionary<string, EnemyStatsData>();
        public BossStatsData worm, golem;
        public ItemData[] equipment;
        public ItemData[] potions;
        public ItemData[] books;
    }

    private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        created = asset == null;
        if (created)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static Catalog BuildData()
    {
        EnsureFolder(DataDir + "/Enemies");
        EnsureFolder(DataDir + "/Items");
        var catalog = new Catalog();

        catalog.player = LoadOrCreate<PlayerStatsData>(DataDir + "/PlayerStats.asset", out bool newPlayer);
        if (newPlayer)
        {
            catalog.player.jumpVelocity = 10f;
            EditorUtility.SetDirty(catalog.player);
        }

        foreach (MonsterDef m in Monsters)
        {
            var data = LoadOrCreate<EnemyStatsData>($"{DataDir}/Enemies/{m.key}.asset", out bool created);
            if (created)
            {
                float[] s = m.stats;
                data.displayName = m.display;
                data.maxHP = (int)s[0];
                data.contactDamage = (int)s[1];
                data.attackDamage = (int)s[2];
                data.attackRange = s[3];
                data.moveSpeed = s[4];
                data.expReward = (int)s[5];
                data.goldMin = (int)s[6];
                data.goldMax = (int)s[7];
                data.flying = m.flying;
                data.hoverHeight = m.key == "Bat" ? 1.0f : 1.1f;
                EditorUtility.SetDirty(data);
            }
            catalog.enemies[m.key] = data;
        }

        catalog.worm = LoadOrCreate<BossStatsData>($"{DataDir}/Enemies/Boss_FireWorm.asset", out bool newWorm);
        if (newWorm)
        {
            SetBoss(catalog.worm, "Fire Worm", 2500, 60, 90, 1.8f, 0.9f, 800, 500, 700, 70, 6f, 2.5f, 0.9f);
        }
        catalog.golem = LoadOrCreate<BossStatsData>($"{DataDir}/Enemies/Boss_Golem.asset", out bool newGolem);
        if (newGolem)
        {
            SetBoss(catalog.golem, "Mecha-stone Golem", 4000, 75, 110, 1.8f, 0.7f, 1500, 1000, 1400, 90, 7f, 2.2f, 1.4f);
        }

        Sprite weapon(int index) => NamedSprite($"{IconsDir}/weapons.png", $"weapons_{index}");
        Sprite armour(int row, int col) => NamedSprite($"{IconsDir}/armours.png", $"armours_r{row}_c{col}");
        Sprite potion(int index) => NamedSprite($"{IconsDir}/potions.png", $"potions_{index}");

        catalog.equipment = new[]
        {
            Item("Weapon_Bronze", "Bronze Sword", ItemKind.Weapon, weapon(0), 100, 1, atk: 5),
            Item("Weapon_Iron", "Iron Sword", ItemKind.Weapon, weapon(4), 400, 5, atk: 12),
            Item("Weapon_Crystal", "Crystal Sword", ItemKind.Weapon, weapon(6), 1200, 10, atk: 22),
            Item("Weapon_Golden", "Golden Sword", ItemKind.Weapon, weapon(10), 3000, 15, atk: 35),
            Item("Armor_Leather", "Leather Armor", ItemKind.Armor, armour(0, 0), 80, 1, def: 3, hp: 20),
            Item("Armor_Chain", "Chainmail", ItemKind.Armor, armour(8, 0), 350, 5, def: 7, hp: 50),
            Item("Armor_Steel", "Steel Plate", ItemKind.Armor, armour(13, 0), 1000, 10, def: 12, hp: 100),
            Item("Armor_Golden", "Golden Plate", ItemKind.Armor, armour(15, 8), 2500, 15, def: 18, hp: 180),
        };
        catalog.potions = new[]
        {
            Item("Potion_Red", "Red Potion", ItemKind.Potion, potion(7), 25, 1, heal: 60),
            Item("Potion_Elixir", "Elixir", ItemKind.Potion, potion(27), 150, 5, heal: 0),
        };
        Sprite book(string name) => NamedSprite($"{IconsDir}/books.png", name);
        // 범위베기는 원소 책으로 배운다 (RPGBuilder.Elements.cs). 여기에는 방어 책만.
        catalog.books = new[]
        {
            Item("Book_Guard", "Guard Book", ItemKind.SkillBook, book("books_116"), 150, 3, skill: SkillType.Guard),
        };

        AssetDatabase.SaveAssets();
        return catalog;
    }

    private static void SetBoss(BossStatsData b, string name, int hp, int contact, int attack, float range, float speed,
        int exp, int goldMin, int goldMax, int projectile, float projectileSpeed, float cooldown, float muzzle)
    {
        b.displayName = name;
        b.maxHP = hp;
        b.contactDamage = contact;
        b.attackDamage = attack;
        b.attackRange = range;
        b.attackCooldown = 2f;
        b.moveSpeed = speed;
        b.detectRange = 14f;
        b.wanderRange = 4f;
        b.superArmor = true;
        b.expReward = exp;
        b.goldMin = goldMin;
        b.goldMax = goldMax;
        b.projectileDamage = projectile;
        b.projectileSpeed = projectileSpeed;
        b.rangedRange = 11f;
        b.rangedCooldown = cooldown;
        b.muzzleHeight = muzzle;
        EditorUtility.SetDirty(b);
    }

    private static ItemData Item(string file, string name, ItemKind kind, Sprite icon, int price, int level,
        int atk = 0, int def = 0, int hp = 0, int heal = 50, SkillType skill = SkillType.Slash)
    {
        var item = LoadOrCreate<ItemData>($"{DataDir}/Items/{file}.asset", out bool created);
        if (created)
        {
            item.itemName = name;
            item.kind = kind;
            item.price = price;
            item.requiredLevel = level;
            item.attackBonus = atk;
            item.defenseBonus = def;
            item.hpBonus = hp;
            item.healAmount = heal;
            item.skill = skill;
        }
        if (item.icon == null) item.icon = icon;
        EditorUtility.SetDirty(item);
        return item;
    }

    // ───────────────────────────── 직렬화 도우미 ─────────────────────────────

    /// <summary>[SerializeField] private 필드까지 SerializedObject로 채운다.</summary>
    private static void Set(Object target, params (string name, object value)[] fields)
    {
        var so = new SerializedObject(target);
        foreach ((string name, object value) in fields)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null) throw new Exception($"[RPGBuilder] {target.GetType().Name}.{name} 필드가 없습니다.");
            Assign(property, value);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Assign(SerializedProperty p, object value)
    {
        switch (value)
        {
            case null: p.objectReferenceValue = null; break;
            case Object o: p.objectReferenceValue = o; break;
            case Enum e: p.intValue = Convert.ToInt32(e); break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case string s: p.stringValue = s; break;
            case Vector2 v: p.vector2Value = v; break;
            case Color c: p.colorValue = c; break;
            case Array array:
                p.arraySize = array.Length;
                for (int k = 0; k < array.Length; k++) Assign(p.GetArrayElementAtIndex(k), array.GetValue(k));
                break;
            default: throw new Exception($"[RPGBuilder] {p.name}에 넣을 수 없는 값: {value.GetType()}");
        }
    }

    private struct ClipDef
    {
        public AnimState state;
        public Sprite[] frames;
        public float fps;
        public bool loop;

        public ClipDef(AnimState state, Sprite[] frames, float fps, bool loop)
        {
            this.state = state;
            this.frames = frames;
            this.fps = fps;
            this.loop = loop;
        }
    }

    private static void SetClips(SpriteAnimator animator, AnimState defaultState, AnimState returnState, params ClipDef[] clips)
    {
        var so = new SerializedObject(animator);
        SerializedProperty list = so.FindProperty("clips");
        list.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("state").intValue = (int)clips[i].state;
            element.FindPropertyRelative("framesPerSecond").floatValue = clips[i].fps;
            element.FindPropertyRelative("loop").boolValue = clips[i].loop;
            Assign(element.FindPropertyRelative("frames"), clips[i].frames);
        }
        so.FindProperty("defaultState").intValue = (int)defaultState;
        so.FindProperty("returnState").intValue = (int)returnState;
        so.FindProperty("spriteFacesRight").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>첫 프레임의 불투명 영역에 맞춘 판정 상자. 피벗 기준 로컬 좌표.</summary>
    private static void FitCollider(BoxCollider2D box, Sprite sprite, float shrinkX = 0.7f, float shrinkY = 0.85f)
    {
        string path = AssetDatabase.GetAssetPath(sprite.texture);
        Texture2D pixels = LoadPixels(path);
        Color32[] data = pixels.GetPixels32();
        int width = pixels.width;
        Rect r = sprite.rect;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        for (int y = (int)r.y; y < (int)r.yMax; y++)
        {
            for (int x = (int)r.x; x < (int)r.xMax; x++)
            {
                if (data[y * width + x].a <= 10) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
        }
        Object.DestroyImmediate(pixels);
        if (minX == int.MaxValue) return;

        float ppu = sprite.pixelsPerUnit;
        float w = (maxX - minX + 1) / ppu;
        float h = (maxY - minY + 1) / ppu;
        float cx = ((minX + maxX + 1) * 0.5f - r.x - sprite.pivot.x) / ppu;
        float cy = ((minY + maxY + 1) * 0.5f - r.y - sprite.pivot.y) / ppu;
        box.size = new Vector2(w * shrinkX, h * shrinkY);
        box.offset = new Vector2(cx, cy - h * (1f - shrinkY) * 0.5f);
    }

    // ───────────────────────────── 프리팹 ─────────────────────────────

    private class PrefabSet
    {
        public readonly Dictionary<string, GameObject> enemies = new Dictionary<string, GameObject>();
        public GameObject worm, golem, fireball, golemArm, hitEffect, damagePopup;
    }

    private static PrefabSet BuildPrefabs(Catalog catalog)
    {
        EnsureFolder(PrefabDir);
        var set = new PrefabSet();

        foreach (MonsterDef m in Monsters)
        {
            var clips = new[]
            {
                new ClipDef(AnimState.Idle, Strip(Sheet(m, m.idle)), 8f, true),
                new ClipDef(AnimState.Walk, Strip(Sheet(m, m.walk)), 10f, true),
                new ClipDef(AnimState.Attack, Strip(Sheet(m, m.attack)), 12f, false),
                new ClipDef(AnimState.Hit, Strip(Sheet(m, m.hit)), 12f, false),
                new ClipDef(AnimState.Death, Strip(Sheet(m, m.death)), 10f, false),
            };
            set.enemies[m.key] = SaveEnemyPrefab("Enemy_" + m.key, catalog.enemies[m.key], clips, null, false);
        }

        set.fireball = SaveProjectilePrefab("Projectile_FireBall", Strip($"{WormDir}/Fire Ball/Move.png"), new Vector2(0.6f, 0.4f));
        set.golemArm = SaveProjectilePrefab("Projectile_GolemArm", SpritesIn(GolemArm), new Vector2(0.9f, 0.4f));

        set.worm = SaveEnemyPrefab("Boss_FireWorm", catalog.worm, new[]
        {
            new ClipDef(AnimState.Idle, Strip($"{WormDir}/Worm/Idle.png"), 8f, true),
            new ClipDef(AnimState.Walk, Strip($"{WormDir}/Worm/Walk.png"), 10f, true),
            new ClipDef(AnimState.Attack, Strip($"{WormDir}/Worm/Attack.png"), 14f, false),
            new ClipDef(AnimState.Hit, Strip($"{WormDir}/Worm/Get Hit.png"), 10f, false),
            new ClipDef(AnimState.Death, Strip($"{WormDir}/Worm/Death.png"), 8f, false),
        }, set.fireball, true);

        // Golem 시트 줄: 0 대기, 2 팔 발사, 1 빛남(피격으로 사용), 7~8 사망. 걷는 줄이 없어서 대기 프레임으로 움직인다.
        set.golem = SaveEnemyPrefab("Boss_Golem", catalog.golem, new[]
        {
            new ClipDef(AnimState.Idle, GolemRow(0), 6f, true),
            new ClipDef(AnimState.Walk, GolemRow(0), 6f, true),
            new ClipDef(AnimState.Attack, GolemRow(2), 12f, false),
            new ClipDef(AnimState.Hit, GolemRow(1, 0, 4), 12f, false),
            new ClipDef(AnimState.Death, GolemRow(7).Concat(GolemRow(8)).ToArray(), 10f, false),
        }, set.golemArm, true);

        set.hitEffect = SaveHitEffectPrefab();
        set.damagePopup = SaveDamagePopupPrefab();

        AssetDatabase.SaveAssets();
        return set;
    }

    private static PrefabSet LoadPrefabs()
    {
        GameObject load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
        var set = new PrefabSet();
        foreach (MonsterDef m in Monsters) set.enemies[m.key] = load("Enemy_" + m.key);
        set.worm = load("Boss_FireWorm");
        set.golem = load("Boss_Golem");
        set.fireball = load("Projectile_FireBall");
        set.golemArm = load("Projectile_GolemArm");
        set.hitEffect = load("Effect_Hit");
        set.damagePopup = load("DamagePopup");
        return set;
    }

    private static GameObject SaveEnemyPrefab(string name, EnemyStatsData data, ClipDef[] clips, GameObject projectile, bool boss)
    {
        var go = new GameObject(name) { layer = EnemyLayer };
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = clips[0].frames[0];
        renderer.sortingOrder = boss ? 4 : 5;
        var animator = go.AddComponent<SpriteAnimator>();   // Enemy보다 먼저 붙여야 Awake 순서가 맞는다
        SetClips(animator, AnimState.Idle, AnimState.Idle, clips);

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        FitCollider(box, clips[0].frames[0], boss ? 0.6f : 0.7f);
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        var loop = go.AddComponent<LoopBody>();
        go.AddComponent<WrapGhost>();

        Enemy enemy = boss ? go.AddComponent<BossEnemy>() : go.AddComponent<Enemy>();
        Set(enemy, ("data", data), ("animator", animator), ("spriteRenderer", renderer), ("hitbox", box),
            ("loopBody", loop), ("playerMask", (int)(1 << PlayerLayer)));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(go);

        // 투사체 풀은 씬에 있어서 프리팹이 참조할 수 없다. 스포너가 꺼낼 때 넣어 준다(EnemySpawner.projectilePool).
        return prefab;
    }

    private static GameObject SaveProjectilePrefab(string name, Sprite[] frames, Vector2 hitSize)
    {
        var go = new GameObject(name);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.sortingOrder = 15;
        var animator = go.AddComponent<SpriteAnimator>();
        SetClips(animator, AnimState.Idle, AnimState.Idle, new ClipDef(AnimState.Idle, frames, 12f, true));
        var loop = go.AddComponent<LoopBody>();
        go.AddComponent<WrapGhost>();
        var projectile = go.AddComponent<Projectile>();
        Set(projectile, ("spriteRenderer", renderer), ("loopBody", loop), ("hitSize", hitSize));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject SaveHitEffectPrefab()
    {
        Sprite[] frames = FrameFolder(ImpactFrames);
        var go = new GameObject("Effect_Hit");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.sortingOrder = 20;
        var animator = go.AddComponent<SpriteAnimator>();
        // 끝나면 풀로 돌아간다. 돌아갈 동작도 Attack으로 둬서 Idle이 없다는 경고가 안 나게 한다.
        SetClips(animator, AnimState.Attack, AnimState.Attack, new ClipDef(AnimState.Attack, frames, 20f, false));
        go.AddComponent<WrapGhost>();
        go.AddComponent<OneShotEffect>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/Effect_Hit.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject SaveDamagePopupPrefab()
    {
        var go = new GameObject("DamagePopup");
        var text = go.AddComponent<TextMeshPro>();
        ApplyWorldFont(text, 4f);
        text.fontStyle = FontStyles.Bold;
        text.rectTransform.sizeDelta = new Vector2(3f, 1f);
        text.sortingOrder = 30;
        text.text = "0";
        var popup = go.AddComponent<DamagePopup>();
        Set(popup, ("text", text));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/DamagePopup.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static TMP_FontAsset Font() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/LiberationSans SDF.asset");
    private static Material OutlineMaterial() => AssetDatabase.LoadAssetAtPath<Material>($"{FontDir}/LiberationSans SDF - Outline.mat");

    private static void ApplyWorldFont(TextMeshPro text, float size)
    {
        text.font = Font();
        Material outline = OutlineMaterial();
        if (outline != null) text.fontSharedMaterial = outline;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    // ───────────────────────────── 씬 ─────────────────────────────

    private class AreaDef
    {
        public string key, display, backdrop;
        public float width;
        public int level;
        public Color skyTint = Color.white, backdropTint = Color.white;
        public Area area;
    }

    private static void BuildScene(UnityEngine.SceneManagement.Scene scene, Catalog catalog, PrefabSet prefabs)
    {

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "World" || root.name == "Player" || root.name == "GameSystems") Object.DestroyImmediate(root);
        }

        GameObject canvas = scene.GetRootGameObjects().First(g => g.name == "Canvas");
        Transform hud = FindDeep(canvas.transform, "HUD_Panel");
        foreach (string old in new[] { "PlayerHUD", "BossBar", "AreaName", "Potions" })
        {
            Transform t = hud.Find(old);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        Transform oldShop = canvas.transform.Find("ShopWindow");
        if (oldShop != null) Object.DestroyImmediate(oldShop.gameObject);

        // 카메라
        Camera camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = ViewHeight * 0.5f;
        camera.backgroundColor = new Color(0.55f, 0.8f, 1f);
        var follow = camera.GetComponent<CameraFollow2D>();
        if (follow == null) follow = camera.gameObject.AddComponent<CameraFollow2D>();
        Set(follow, ("smoothTime", 0.12f), ("offset", Vector2.zero));

        var world = new GameObject("World").transform;
        var systems = new GameObject("GameSystems").transform;

        // 공간 정의. 순서가 곧 세로 배치 순서다.
        var town = new AreaDef { key = "Town", display = "Town", backdrop = "entrance", width = 62f, level = 1 };
        var shop = new AreaDef { key = "Shop", display = "Market", backdrop = "entrance", width = 31f, level = 1, backdropTint = new Color(1f, 0.9f, 0.75f) };
        var dungeons = Monsters.Select(m => new AreaDef
        {
            key = m.key, display = $"{m.display} Field", backdrop = m.backdrop, width = 62f, level = m.dungeonLevel,
            backdropTint = Color.Lerp(m.tint, new Color(0.55f, 0.55f, 0.65f), 0.25f), skyTint = m.backdrop == "Mine" ? new Color(0.75f, 0.75f, 0.9f) : Color.white
        }).ToArray();
        var wormLair = new AreaDef { key = "BossWorm", display = "Fire Worm's Nest", backdrop = "Mine", width = 31f, level = 18,
            skyTint = new Color(1f, 0.6f, 0.45f), backdropTint = new Color(0.8f, 0.45f, 0.4f) };
        var golemLair = new AreaDef { key = "BossGolem", display = "Golem's Ruins", backdrop = "Mine", width = 31f, level = 22,
            skyTint = new Color(0.55f, 0.6f, 0.8f), backdropTint = new Color(0.5f, 0.55f, 0.7f) };

        var all = new List<AreaDef> { town, shop };
        all.AddRange(dungeons);
        all.Add(wormLair);
        all.Add(golemLair);

        for (int i = 0; i < all.Count; i++)
        {
            all[i].area = BuildArea(world, all[i], i);
        }

        // 공용 풀
        ObjectPool hitPool = MakePool(systems, "Pool_HitEffect", prefabs.hitEffect, 6);
        ObjectPool popupPool = MakePool(systems, "Pool_DamagePopup", prefabs.damagePopup, 10);

        // 던전 몬스터
        for (int i = 0; i < dungeons.Length; i++)
        {
            MonsterDef m = Monsters[i];
            ObjectPool pool = MakePool(dungeons[i].area.transform, "Pool_" + m.key, prefabs.enemies[m.key], 8);
            var spawner = dungeons[i].area.gameObject.AddComponent<EnemySpawner>();
            Set(spawner, ("area", dungeons[i].area), ("pool", pool), ("maxAlive", 8), ("respawnDelay", 5f), ("respawn", true), ("safeRadius", 5f));
            Set(dungeons[i].area, ("spawner", spawner));
        }

        // 보스
        BuildBoss(wormLair.area, prefabs.worm, prefabs.fireball, "Fire");
        BuildBoss(golemLair.area, prefabs.golem, prefabs.golemArm, "Arm");

        // 마을 포탈: 오른쪽으로 걸어가면 순서대로 → 한 바퀴 돌아 상점과 출발점으로.
        var ring = new List<(AreaDef def, float x, Color color)>();
        for (int i = 0; i < dungeons.Length; i++) ring.Add((dungeons[i], 4f + 5f * i, Color.white));
        ring.Add((wormLair, 45f, new Color(1f, 0.55f, 0.5f)));
        ring.Add((golemLair, 51f, new Color(1f, 0.55f, 0.5f)));
        ring.Add((shop, 57f, new Color(1f, 0.9f, 0.5f)));

        foreach ((AreaDef def, float rawX, Color color) in ring)
        {
            float x = Mathf.Repeat(rawX + town.width * 0.5f, town.width) - town.width * 0.5f;
            string label = def == shop ? "Market" : $"{def.display}\n<size=70%>Lv.{def.level}+</size>";
            MakePortal(town.area, x, def.area, 0f, label, color);

            // 돌아오는 포탈은 도착 지점(중심)에 두고, 마을의 같은 포탈 앞으로 돌려보낸다.
            float returnArrival = x;
            float returnX = def == wormLair || def == golemLair ? -8f : 0f;
            MakePortal(def.area, returnX, town.area, returnArrival, "Town", new Color(0.6f, 1f, 1f));
            if (returnX != 0f) SetArrival(town.area, def.area, returnX);
        }

        // 상점 NPC
        ShopWindow shopWindow = BuildShopWindow(canvas.transform);
        MakeMerchant(shop.area, -5f, "Weapon & Armor", catalog.equipment, shopWindow, CharacterSheet($"{ArcherDir}/Archer-Idle-spritesheet.png"), false);
        MakeMerchant(shop.area, 5f, "Potions", catalog.potions, shopWindow, new[] { NamedSpriteOrFirst($"{Fantasy2}/Mimic/Idle_closed.png") }, true);

        // 플레이어
        PlayerController player = BuildPlayer(catalog, follow, hitPool, town.area);

        // 원소 책 상인 + 원소 칼 (책 상인은 Guard Book도 판다)
        BuildElementSkills(shop.area, shopWindow, player, catalog.books[0], catalog.potions);

        // 시스템
        var areaManager = systems.gameObject.AddComponent<AreaManager>();
        Set(areaManager, ("startArea", town.area), ("player", player), ("cameraFollow", follow));

        var popups = systems.gameObject.AddComponent<DamagePopupSpawner>();
        Set(popups, ("pool", popupPool));

        Transform gameOverPanel = FindDeep(canvas.transform, "GameOver_Panel");
        TMP_Text gameOverTitle = gameOverPanel != null ? gameOverPanel.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "GameOverText") : null;
        var flow = systems.gameObject.AddComponent<GameFlow>();
        Set(flow, ("player", player.GetComponent<PlayerStats>()), ("bossesToWin", new Object[] { catalog.worm, catalog.golem }), ("gameOverTitle", gameOverTitle));

        // UI
        BuildHUD(hud, player.GetComponent<PlayerStats>());
        SetTitleTexts(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        throw new Exception($"[RPGBuilder] 씬에서 '{name}'을 찾을 수 없습니다.");
    }

    private static Sprite NamedSpriteOrFirst(string path) => SpritesIn(path).OrderBy(s => s.rect.x).First();

    /// <summary>돌아오는 포탈이 중심이 아닌 곳에 있으면, 그 공간으로 가는 포탈의 도착 지점을 거기로 맞춘다.</summary>
    private static void SetArrival(Area from, Area destination, float offset)
    {
        foreach (Portal portal in from.GetComponentsInChildren<Portal>())
        {
            if (portal.Destination == destination) Set(portal, ("arrivalOffsetX", offset));
        }
    }

    private static Area BuildArea(Transform world, AreaDef def, int index)
    {
        var go = new GameObject("Area_" + def.key);
        go.transform.SetParent(world, false);
        go.transform.position = new Vector3(0f, index * AreaSpacingY, 0f);
        var area = go.AddComponent<Area>();
        Set(area, ("displayName", def.display), ("requiredLevel", def.level), ("loopWidth", def.width), ("cameraHeight", CameraHeight));

        float span = def.width * 3f + BackdropPeriod * 2f;
        float viewCenter = CameraHeight;
        var visual = new GameObject("Visual").transform;
        visual.SetParent(go.transform, false);

        // 하늘 두 겹
        Sprite skyBack = AssetDatabase.LoadAssetAtPath<Sprite>($"{TilesDir}/Assets/Background_2.png");
        Sprite skyFront = AssetDatabase.LoadAssetAtPath<Sprite>($"{TilesDir}/Assets/Background_1.png");
        MakeTiled(visual, "Sky_Back", skyBack, new Vector2(0f, viewCenter), new Vector2(span, ViewHeight), -50, def.skyTint);
        MakeTiled(visual, "Sky_Front", skyFront, new Vector2(0f, viewCenter), new Vector2(span, ViewHeight), -49, def.skyTint);

        // 먼 배경: Social 장면을 좌우반전으로 번갈아 붙여 이음새를 없앤다.
        Sprite backdrop = AssetDatabase.LoadAssetAtPath<Sprite>($"{TilesDir}/Social/{def.backdrop}.png");
        var backdropRoot = new GameObject("Backdrop").transform;
        backdropRoot.SetParent(visual, false);
        float half = BackdropPeriod * 0.5f;
        int count = Mathf.CeilToInt(span / half) + 1;
        if (count % 2 == 1) count++;
        for (int k = -count / 2; k < count / 2; k++)
        {
            var piece = new GameObject($"Backdrop_{k}");
            piece.transform.SetParent(backdropRoot, false);
            piece.transform.localPosition = new Vector3(k * half + half * 0.5f, viewCenter, 0f);
            var r = piece.AddComponent<SpriteRenderer>();
            r.sprite = backdrop;
            r.flipX = (k & 1) != 0;
            r.sortingOrder = -40;
            r.color = def.backdropTint * new Color(0.8f, 0.8f, 0.8f, 1f);
        }

        // 지면: 잔디 띠 + 흙 채움. 지면 y가 걷는 선이다.
        Sprite top = NamedSprite(TileAtlas, "Ground_Top");
        Sprite fill = NamedSprite(TileAtlas, "Ground_Fill");
        float bottom = viewCenter - ViewHeight * 0.5f - 1f;
        MakeTiled(visual, "Ground_Fill", fill, new Vector2(0f, (-0.85f + bottom) * 0.5f), new Vector2(span, -0.85f - bottom), -21, Color.white);
        MakeTiled(visual, "Ground_Top", top, new Vector2(0f, 0.15f - 0.5f), new Vector2(span, 1f), -20, Color.white);

        var ground = new GameObject("Ground") { layer = GroundLayer };
        ground.transform.SetParent(go.transform, false);
        var box = ground.AddComponent<BoxCollider2D>();
        box.size = new Vector2(span, 2f);
        box.offset = new Vector2(0f, -1f);

        // 이름표(허공에 떠 있는 공간 이름)는 두지 않는다 — 도착할 때 메시지로 보여준다.
        return area;
    }

    private static SpriteRenderer MakeTiled(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, int order, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.drawMode = SpriteDrawMode.Tiled;
        r.tileMode = SpriteTileMode.Continuous;
        r.size = size;
        r.sortingOrder = order;
        r.color = color;
        return r;
    }

    private static ObjectPool MakePool(Transform parent, string name, GameObject prefab, int size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var pool = go.AddComponent<ObjectPool>();
        Set(pool, ("prefab", prefab), ("initialSize", size), ("expandable", true));
        return pool;
    }

    private static void BuildBoss(Area area, GameObject bossPrefab, GameObject projectilePrefab, string label)
    {
        ObjectPool projectiles = MakePool(area.transform, "Pool_Projectile_" + label, projectilePrefab, 4);

        ObjectPool bossPool = MakePool(area.transform, "Pool_Boss", bossPrefab, 1);
        var spawner = area.gameObject.AddComponent<EnemySpawner>();
        Set(spawner, ("area", area), ("pool", bossPool), ("maxAlive", 1), ("respawn", false), ("useFixedX", true), ("fixedX", 7f), ("projectilePool", projectiles));
        Set(area, ("spawner", spawner));
    }

    private static void MakePortal(Area area, float x, Area destination, float arrivalX, string label, Color color)
    {
        Sprite[] frames = FrameFolder(PortalFrames);
        var go = new GameObject("Portal_" + destination.name.Replace("Area_", ""));
        go.transform.SetParent(area.transform, false);
        go.transform.localPosition = new Vector3(x, 0.9f, 0f);
        go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = frames[0];
        r.sortingOrder = -5;
        r.color = color;
        var animator = go.AddComponent<SpriteAnimator>();
        SetClips(animator, AnimState.Idle, AnimState.Idle, new ClipDef(AnimState.Idle, frames, 10f, true));
        var ghost = go.AddComponent<WrapGhost>();
        Set(ghost, ("area", area));

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.7f, 1.1f);
        var portal = go.AddComponent<Portal>();
        Set(portal, ("destination", destination), ("arrivalOffsetX", arrivalX));

        MakeLabel(area, "Label_" + go.name, label, new Vector3(x, 2.25f, 0f));
    }

    /// <summary>글자는 스프라이트가 아니라 WrapGhost가 따라 그리지 못한다. 경계 너머에서도 보이게 한 바퀴 폭 양옆에 하나씩 더 둔다.</summary>
    private static void MakeLabel(Area area, string name, string text, Vector3 localPosition)
    {
        float width = area.LoopWidth;
        foreach (float shift in new[] { 0f, width, -width })
        {
            var go = new GameObject(shift == 0f ? name : name + " (wrap)");
            go.transform.SetParent(area.transform, false);
            go.transform.localPosition = localPosition + new Vector3(shift, 0f, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            ApplyWorldFont(tmp, 2.6f);
            tmp.rectTransform.sizeDelta = new Vector2(5f, 1.2f);
            tmp.sortingOrder = 25;
            tmp.text = text;
        }
    }

    private static void MakeMerchant(Area area, float x, string title, ItemData[] items, ShopWindow window, Sprite[] frames, bool faceLeft)
    {
        var go = new GameObject("Merchant_" + title.Replace(" ", "").Replace("&", ""));
        go.transform.SetParent(area.transform, false);
        go.transform.localPosition = new Vector3(x, 0f, 0f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = frames[0];
        r.sortingOrder = 0;
        r.flipX = faceLeft;
        if (frames.Length > 1)
        {
            var animator = go.AddComponent<SpriteAnimator>();
            SetClips(animator, AnimState.Idle, AnimState.Idle, new ClipDef(AnimState.Idle, frames, 6f, true));
        }
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 1.2f);
        box.offset = new Vector2(0f, 0.6f);
        var npc = go.AddComponent<ShopNPC>();
        Set(npc, ("shopTitle", title), ("items", items), ("window", window));

        MakeLabel(area, "Label_" + go.name, $"{title}\n<size=70%>[UP] Shop</size>", new Vector3(x, 1.9f, 0f));
    }

    private static PlayerController BuildPlayer(Catalog catalog, CameraFollow2D follow, ObjectPool hitPool, Area town)
    {
        var go = new GameObject("Player") { layer = PlayerLayer };
        go.transform.position = new Vector3(town.CenterX, town.GroundY, 0f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 10;
        var animator = go.AddComponent<SpriteAnimator>();
        Sprite[] idle = CharacterSheet($"{SwordsmanDir}/Swordsman_Idle.png");
        Sprite[] walk = CharacterSheet($"{SwordsmanDir}/Swordsman_Walk.png");
        renderer.sprite = idle[0];
        // Swordsman에는 점프 프레임이 없어서 걷기 중 발이 가장 벌어진 한 장을 쓴다.
        SetClips(animator, AnimState.Idle, AnimState.Idle,
            new ClipDef(AnimState.Idle, idle, 8f, true),
            new ClipDef(AnimState.Walk, walk, 12f, true),
            new ClipDef(AnimState.Jump, new[] { walk[Mathf.Min(2, walk.Length - 1)] }, 1f, true),
            new ClipDef(AnimState.Attack, CharacterSheet($"{SwordsmanDir}/Swordsman_Atk1.png"), 20f, false),
            new ClipDef(AnimState.Hit, CharacterSheet($"{SwordsmanDir}/Swordsman_Hit.png"), 14f, false),
            new ClipDef(AnimState.Skill, CharacterSheet($"{SwordsmanDir}/Swordsman_Atk2.png"), 16f, false),
            new ClipDef(AnimState.Block, CharacterSheet($"{SwordsmanDir}/Swordsman_Block.png"), 8f, true));

        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;   // 가만히 서 있어도 접촉 판정이 멈추지 않게
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.5f, 0.9f);
        box.offset = new Vector2(0f, 0.45f);

        var loop = go.AddComponent<LoopBody>();
        Set(loop, ("area", town), ("followingCamera", follow));

        var stats = go.AddComponent<PlayerStats>();
        Set(stats, ("data", catalog.player), ("potionSlots", catalog.potions));

        var controller = go.AddComponent<PlayerController>();
        Set(controller, ("animator", animator), ("spriteRenderer", renderer), ("loopBody", loop), ("hitEffectPool", hitPool),
            ("groundMask", (int)(1 << GroundLayer)), ("enemyMask", (int)(1 << EnemyLayer)));

        Set(follow, ("target", go.transform));
        return controller;
    }

    // ───────────────────────────── UI ─────────────────────────────
    // 위치·크기는 기본값일 뿐이다. 보기 좋게 옮기는 것은 인스펙터에서 한다.

    private static RectTransform UIObject(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rt = UIObject(parent, name, anchorMin, anchorMax, pivot, position, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text, float size, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
    {
        RectTransform rt = UIObject(parent, name, anchorMin, anchorMax, pivot, position, sizeDelta);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = Font();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.text = text;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>배경 + 채움 막대. 채움은 오른쪽 앵커로 비율을 표현한다.</summary>
    private static RectTransform Bar(Transform parent, string name, Vector2 position, Vector2 size, Color fillColor)
    {
        Image back = Panel(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, position, size, new Color(0f, 0f, 0f, 0.6f));
        Image fill = Panel(back.transform, "Fill", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, fillColor);
        RectTransform rt = fill.rectTransform;
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);
        return rt;
    }

    private static void BuildHUD(Transform hud, PlayerStats stats)
    {
        Image root = Panel(hud, "PlayerHUD", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20f, 20f), new Vector2(560f, 190f), new Color(0f, 0f, 0f, 0.45f));
        Transform t = root.transform;
        Vector2 bl = Vector2.zero;

        TextMeshProUGUI level = Label(t, "Level", "Lv.1", 44f, TextAlignmentOptions.MidlineLeft, bl, bl, bl, new Vector2(16f, 128f), new Vector2(160f, 54f));
        TextMeshProUGUI gold = Label(t, "Gold", "Gold 0", 28f, TextAlignmentOptions.MidlineRight, bl, bl, bl, new Vector2(300f, 134f), new Vector2(244f, 40f));
        gold.color = new Color(1f, 0.85f, 0.3f);

        RectTransform hpFill = Bar(t, "HPBar", new Vector2(16f, 92f), new Vector2(528f, 30f), new Color(0.9f, 0.2f, 0.25f));
        TextMeshProUGUI hp = Label(hpFill.parent, "HPText", "HP", 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform expFill = Bar(t, "EXPBar", new Vector2(16f, 58f), new Vector2(528f, 26f), new Color(0.95f, 0.8f, 0.2f));
        TextMeshProUGUI exp = Label(expFill.parent, "EXPText", "EXP", 20f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI stat = Label(t, "Stats", "ATK", 22f, TextAlignmentOptions.TopLeft, bl, bl, bl, new Vector2(16f, 4f), new Vector2(528f, 52f));

        TextMeshProUGUI potions = Label(hud, "Potions", "", 26f, TextAlignmentOptions.Bottom,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(800f, 40f));

        TextMeshProUGUI areaName = Label(hud, "AreaName", "", 32f, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(700f, 50f));

        var hudScript = root.gameObject.AddComponent<PlayerHUD>();
        Set(hudScript, ("stats", stats), ("levelText", level), ("hpFill", hpFill), ("hpText", hp), ("expFill", expFill), ("expText", exp),
            ("goldText", gold), ("statText", stat), ("potionText", potions), ("areaText", areaName));

        // 보스 체력바 — 화면 위쪽. 점수 숫자(ScoreText)와 겹치면 인스펙터에서 옮긴다.
        RectTransform bossRoot = UIObject(hud, "BossBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(900f, 36f));
        RectTransform bossFill = Bar(bossRoot, "Bar", Vector2.zero, new Vector2(900f, 36f), new Color(0.75f, 0.15f, 0.9f));
        TextMeshProUGUI bossName = Label(bossFill.parent, "Name", "Boss", 24f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var bossBar = hud.gameObject.GetComponent<BossHealthBar>();
        if (bossBar == null) bossBar = hud.gameObject.AddComponent<BossHealthBar>();
        Set(bossBar, ("root", bossRoot.gameObject), ("fill", bossFill), ("nameText", bossName));
        bossRoot.gameObject.SetActive(false);
    }

    private static ShopWindow BuildShopWindow(Transform canvas)
    {
        RectTransform holder = UIObject(canvas, "ShopWindow", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        holder.SetAsLastSibling();
        Vector2 mid = new Vector2(0.5f, 0.5f);
        Image root = Panel(holder, "Window", mid, mid, mid, Vector2.zero, new Vector2(760f, 560f), new Color(0.08f, 0.06f, 0.12f, 0.92f));
        Transform t = root.transform;
        Vector2 top = new Vector2(0.5f, 1f);

        TextMeshProUGUI title = Label(t, "Title", "Shop", 40f, TextAlignmentOptions.Center, top, top, top, new Vector2(0f, -16f), new Vector2(700f, 54f));
        TextMeshProUGUI gold = Label(t, "Gold", "Gold", 28f, TextAlignmentOptions.MidlineRight, top, top, top, new Vector2(0f, -70f), new Vector2(700f, 36f));
        gold.color = new Color(1f, 0.85f, 0.3f);

        const int rowsCount = 8;
        var backgrounds = new Image[rowsCount];
        var icons = new Image[rowsCount];
        var texts = new TMP_Text[rowsCount];
        for (int i = 0; i < rowsCount; i++)
        {
            Image row = Panel(t, $"Row{i}", top, top, top, new Vector2(0f, -112f - i * 46f), new Vector2(720f, 42f), new Color(0f, 0f, 0f, 0.35f));
            Vector2 left = new Vector2(0f, 0.5f);
            Image icon = Panel(row.transform, "Icon", left, left, left, new Vector2(8f, 0f), new Vector2(34f, 34f), Color.white);
            icon.preserveAspect = true;
            TextMeshProUGUI text = Label(row.transform, "Text", "", 24f, TextAlignmentOptions.MidlineLeft, left, left, left, new Vector2(54f, 0f), new Vector2(660f, 40f));
            backgrounds[i] = row;
            icons[i] = icon;
            texts[i] = text;
        }

        Vector2 bottom = new Vector2(0.5f, 0f);
        TextMeshProUGUI info = Label(t, "Info", "", 24f, TextAlignmentOptions.Center, bottom, bottom, bottom, new Vector2(0f, 14f), new Vector2(720f, 36f));
        info.color = new Color(0.8f, 0.9f, 1f);

        var window = holder.gameObject.AddComponent<ShopWindow>();
        Set(window, ("root", root.gameObject), ("titleText", title), ("goldText", gold), ("infoText", info),
            ("rowBackgrounds", backgrounds), ("rowIcons", icons), ("rowTexts", texts));
        root.gameObject.SetActive(false);
        return window;
    }

    private static void SetTitleTexts(Transform canvas)
    {
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "TitleText") text.text = "RPG";
            if (text.name == "GuideText")
            {
                text.text = "Press SPACE to Start\n<size=60%>LEFT/RIGHT move   SPACE jump   Z attack   UP portal/talk   1,2 potion</size>";
            }
            EditorUtility.SetDirty(text);
        }
    }
}
