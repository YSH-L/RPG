using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 원소 무기 스킬: 원소 에셋(Data/Elements), 베기 이펙트 프리팹(Prefabs/Effects), 원소 책, 책 상인, 플레이어의 원소 칼.
/// <b>RPG &gt; Build Game</b>이 마지막에 부르고, 지금 씬에만 따로 넣고 싶으면 <b>RPG &gt; Apply Element Skills</b>.
/// 여러 번 돌려도 된다. 원소 능력 숫자는 에셋이 처음 만들어질 때만 넣는다.
/// </summary>
public static partial class RPGBuilder
{
    private const string ElementSpriteDir = Sprites + "/Skills/Elemental Weapons Effect";
    private const string SlashFxDir = Sprites + "/Skills/Slash/128x128/Slash 1";
    private const string LightFxDir = Sprites + "/Skills/Paladin/VFX 2/Frames";
    private const float BookMerchantX = 11f;
    private static readonly Color BookMerchantTint = new Color(0.75f, 0.55f, 1f);

    private struct ElementDef
    {
        public string key;
        public Color color;
        public string blade;      // Elemental Weapons Effect 안의 칼 그림
        public string slashDir;   // 베기 이펙트 프레임 폴더
        public float slashFps;
        public string book;       // books.png 조각 이름
        public Action<ElementData> abilities;
    }

    private static readonly ElementDef[] Elements =
    {
        new ElementDef { key = "Arcane", color = new Color(0.8f, 0.35f, 1f), blade = "Arcane/01.png", slashDir = SlashFxDir + "/color3/Frames", slashFps = 18f, book = "books_25",
            abilities = e => { e.areaMultiplier = 2f; e.burnRatio = 0.1f; e.burnDuration = 3f; e.burnInterval = 1f; } },
        new ElementDef { key = "Blood", color = new Color(0.75f, 0.1f, 0.15f), blade = "Blood/13.png", slashDir = SlashFxDir + "/color2/Frames", slashFps = 18f, book = "books_9",
            abilities = e => e.lifesteal = 0.15f },
        new ElementDef { key = "Fire", color = new Color(1f, 0.45f, 0.1f), blade = "Fire/25.png", slashDir = SlashFxDir + "/color4/Frames", slashFps = 18f, book = "books_7",
            abilities = e => { e.burnRatio = 0.2f; e.burnDuration = 3f; e.burnInterval = 1f; } },
        new ElementDef { key = "Ice", color = new Color(0.35f, 0.7f, 1f), blade = "Ice/13.png", slashDir = SlashFxDir + "/color5/Frames", slashFps = 18f, book = "books_23",
            abilities = e => { e.slowFactor = 0.5f; e.slowDuration = 2f; } },
        new ElementDef { key = "Light", color = new Color(1f, 0.95f, 0.7f), blade = "Light/25.png", slashDir = LightFxDir, slashFps = 24f, book = "books_5",
            abilities = e => e.damageMultiplier = 1.3f },
        new ElementDef { key = "Nature", color = new Color(0.35f, 0.9f, 0.35f), blade = "Nature/01.png", slashDir = SlashFxDir + "/color1/Frames", slashFps = 18f, book = "books_17",
            abilities = e => e.rootDuration = 0.8f },
    };

    [MenuItem("RPG/Apply Element Skills (current scene)")]
    public static void ApplyElementSkillsToOpenScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[RPGBuilder] 플레이 모드에서는 적용할 수 없습니다.");
            return;
        }

        Area market = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.DisplayName == "Market");
        ShopWindow window = Object.FindAnyObjectByType<ShopWindow>(FindObjectsInactive.Include);
        PlayerController player = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (market == null || window == null || player == null)
        {
            Debug.LogError("[RPGBuilder] 씬에 Market 공간·ShopWindow·Player가 있어야 합니다. RPG > Build Game을 먼저 돌리세요.");
            return;
        }

        ItemData guard = AssetDatabase.LoadAssetAtPath<ItemData>($"{DataDir}/Items/Book_Guard.asset");
        ItemData[] potions =
        {
            AssetDatabase.LoadAssetAtPath<ItemData>($"{DataDir}/Items/Potion_Red.asset"),
            AssetDatabase.LoadAssetAtPath<ItemData>($"{DataDir}/Items/Potion_Elixir.asset"),
        };
        BuildElementSkills(market, window, player, guard, potions.Where(p => p != null).ToArray());

        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        Debug.Log("[RPGBuilder] 원소 스킬을 넣었습니다.");
    }

    private static void BuildElementSkills(Area market, ShopWindow window, PlayerController player, ItemData guardBook, ItemData[] potions)
    {
        EnsureFolder(DataDir + "/Elements");
        EnsureFolder(PrefabDir + "/Effects");

        // 원소 에셋 · 이펙트 프리팹 · 원소 책
        var books = new System.Collections.Generic.List<ItemData>();
        if (guardBook != null)
        {
            // 파란 책은 Ice가 쓰므로 Guard는 금테 은색 책으로 둔다.
            guardBook.icon = NamedSprite($"{IconsDir}/books.png", "books_116") ?? guardBook.icon;
            EditorUtility.SetDirty(guardBook);
            books.Add(guardBook);
        }

        foreach (ElementDef def in Elements)
        {
            var element = LoadOrCreate<ElementData>($"{DataDir}/Elements/{def.key}.asset", out bool created);
            if (created)
            {
                element.displayName = def.key;
                element.color = def.color;
                def.abilities(element);
            }
            element.blade = SpritesIn($"{ElementSpriteDir}/{def.blade}").FirstOrDefault();
            element.slashEffect = SaveSlashEffectPrefab(def);
            EditorUtility.SetDirty(element);

            ItemData book = Item($"Book_{def.key}", $"{def.key} Book", ItemKind.SkillBook,
                NamedSprite($"{IconsDir}/books.png", def.book), 300, 5, skill: SkillType.Slash);
            book.element = element;
            EditorUtility.SetDirty(book);
            books.Add(book);
        }
        AssetDatabase.SaveAssets();

        // 물약 상인은 물약만 판다.
        foreach (ShopNPC npc in market.GetComponentsInChildren<ShopNPC>(true))
        {
            if (!npc.name.StartsWith("Merchant_Potions")) continue;
            Set(npc, ("shopTitle", "Potions"), ("items", potions));
        }
        foreach (TMP_Text label in market.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!label.name.StartsWith("Label_Merchant_Potions")) continue;
            label.text = "Potions\n<size=70%>[UP] Shop</size>";
            EditorUtility.SetDirty(label);
        }

        // 책 상인 (다시 돌리면 지우고 새로 만든다)
        foreach (Transform child in market.transform.Cast<Transform>().ToArray())
        {
            if (child.name.StartsWith("Merchant_Books") || child.name.StartsWith("Label_Merchant_Books")) Object.DestroyImmediate(child.gameObject);
        }
        // 사람 형태는 Swordsman·Archer뿐이다. 무기 상인과 같은 Archer를 쓰되 보라(마법사) 톤으로 구분한다.
        MakeMerchant(market, BookMerchantX, "Books", books.ToArray(), window, CharacterSheet($"{ArcherDir}/Archer-Idle-spritesheet.png"), true);
        Transform bookMerchant = market.transform.Find("Merchant_Books");
        if (bookMerchant != null) bookMerchant.GetComponent<SpriteRenderer>().color = BookMerchantTint;

        // 플레이어의 원소 칼
        Transform bladeTransform = player.transform.Find("ElementBlade");
        if (bladeTransform == null)
        {
            bladeTransform = new GameObject("ElementBlade").transform;
            bladeTransform.SetParent(player.transform, false);
        }
        var blade = bladeTransform.GetComponent<SpriteRenderer>();
        if (blade == null) blade = bladeTransform.gameObject.AddComponent<SpriteRenderer>();
        blade.sortingOrder = 11;
        blade.enabled = false;

        var visual = player.GetComponent<SlashVisual>();
        if (visual == null) visual = player.gameObject.AddComponent<SlashVisual>();
        Set(visual, ("blade", blade));
        Set(player, ("slashVisual", visual));
    }

    private static GameObject SaveSlashEffectPrefab(ElementDef def)
    {
        string[] files = Directory.GetFiles(def.slashDir, "*.png")
            .Select(p => p.Replace('\\', '/'))
            .OrderBy(p => TrailingNumber(Path.GetFileNameWithoutExtension(p)))
            .ToArray();
        // 이 프레임들은 자동 슬라이스로 한 장이 여러 조각으로 잘려 있다. 프레임 전체를 한 장으로 쓴다.
        foreach (string file in files) EnsureSingleSprite(file);

        Sprite[] frames = files
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
            .Where(s => s != null)
            .ToArray();

        var go = new GameObject($"Effect_Slash_{def.key}");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.sortingOrder = 18;
        var animator = go.AddComponent<SpriteAnimator>();
        // 끝나면 꺼진다. 돌아갈 동작도 Attack으로 둬서 Idle이 없다는 경고가 안 나게 한다.
        SetClips(animator, AnimState.Attack, AnimState.Attack, new ClipDef(AnimState.Attack, frames, def.slashFps, false));
        go.AddComponent<SlashEffect>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/Effects/Effect_Slash_{def.key}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>Sprite Mode를 Single(가운데 피벗)로. 필터·압축 같은 나머지 설정은 그대로 둔다.</summary>
    private static void EnsureSingleSprite(string path)
    {
        TextureImporter importer = Importer(path);
        if (importer.spriteImportMode == SpriteImportMode.Single) return;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SaveAndReimport();
    }
}
