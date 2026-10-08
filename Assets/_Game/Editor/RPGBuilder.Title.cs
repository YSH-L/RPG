using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 시작 화면: 검은 화면에 제목 → 캐릭터 선택 → 조작법 → 화면이 밝아지며 시작.
/// <b>RPG &gt; Build Game</b>이 마지막에 부르고, 지금 씬에만 넣고 싶으면 <b>RPG &gt; Apply Title Intro (current scene)</b>.
/// 여러 번 돌려도 된다 — Ready 패널 안을 지우고 다시 만든다.
/// </summary>
public static partial class RPGBuilder
{
    private const string GameTitle = "DUNGEON SLAYER";

    private const string ControlsText =
        "LEFT / RIGHT\nSPACE\nZ\nA\nS\nUP\n1 / 2\nESC";

    private const string ControlsMeaning =
        "Move\nJump\nAttack\nSkill (learn from books)\nGuard / Dodge (learn from books)\nPortal / Talk to shop\nUse potion\nPause";

    [MenuItem("RPG/Apply Title Intro (current scene)")]
    public static void ApplyTitleIntroToOpenScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[RPGBuilder] 플레이 모드에서는 적용할 수 없습니다.");
            return;
        }

        GameObject systems = GameObject.Find("GameSystems");
        Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (systems == null || canvas == null)
        {
            Debug.LogError("[RPGBuilder] 씬에 GameSystems·Canvas가 있어야 합니다. RPG > Build Game을 먼저 돌리세요.");
            return;
        }

        BuildTitleIntro(systems.transform, canvas.transform);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Debug.Log("[RPGBuilder] 시작 화면을 넣었습니다.");
    }

    private static void BuildTitleIntro(Transform systems, Transform canvas)
    {
        Transform ready = FindDeep(canvas, "Ready_Panel");
        if (ready == null)
        {
            Debug.LogError("[RPGBuilder] Canvas에 Ready_Panel이 없습니다.");
            return;
        }

        // 다시 돌릴 때를 위해 Ready 패널 안과 검은 판을 비운다.
        for (int i = ready.childCount - 1; i >= 0; i--) Object.DestroyImmediate(ready.GetChild(i).gameObject);
        Transform oldOverlay = canvas.Find("FadeOverlay");
        if (oldOverlay != null) Object.DestroyImmediate(oldOverlay.gameObject);

        var stretch0 = Vector2.zero;
        var stretch1 = Vector2.one;
        var center = new Vector2(0.5f, 0.5f);

        // 검은 판: HUD와 게임 화면은 가리고, Ready 패널 글자는 그 위에 보이도록 Ready 패널 바로 앞에 둔다.
        Image overlay = Panel(canvas, "FadeOverlay", stretch0, stretch1, center, Vector2.zero, Vector2.zero, Color.black);
        overlay.raycastTarget = false;
        overlay.transform.SetSiblingIndex(ready.GetSiblingIndex());

        // 1. 제목
        RectTransform titlePage = UIObject(ready, "TitlePage", stretch0, stretch1, center, Vector2.zero, Vector2.zero);
        TextMeshProUGUI title = Label(titlePage, "TitleText", GameTitle, 96, TextAlignmentOptions.Center,
            center, center, center, new Vector2(0f, 80f), new Vector2(1600f, 220f));
        title.color = new Color(0.85f, 0.15f, 0.12f);
        Label(titlePage, "PressText", "Press SPACE", 32, TextAlignmentOptions.Center,
            center, center, center, new Vector2(0f, -160f), new Vector2(900f, 80f));

        // 2. 캐릭터 선택 (글자와 그림은 CharacterSelect가 채운다)
        RectTransform selectPage = UIObject(ready, "SelectPage", stretch0, stretch1, center, Vector2.zero, Vector2.zero);
        Label(selectPage, "HeaderText", "CHOOSE YOUR HERO", 56, TextAlignmentOptions.Center,
            center, center, center, new Vector2(0f, 300f), new Vector2(1200f, 120f));
        // Archer는 몸이 검은색이라 검은 배경에 묻힌다. 그림 뒤에 회색 판을 깐다.
        Image frame = Panel(selectPage, "PortraitFrame", center, center, center, new Vector2(0f, 60f), new Vector2(320f, 320f), new Color(0.35f, 0.35f, 0.42f));
        frame.raycastTarget = false;
        Image portrait = Panel(selectPage, "Portrait", center, center, center, new Vector2(0f, 60f), new Vector2(280f, 280f), Color.white);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        // 이름이 GuideText여야 RPG > Apply Archer가 다시 찾아 연결한다.
        TextMeshProUGUI guide = Label(selectPage, "GuideText", GuideBase, 40, TextAlignmentOptions.Center,
            center, center, center, new Vector2(0f, -230f), new Vector2(1200f, 240f));
        guide.lineSpacing = 50f;   // 픽셀 폰트는 줄 간격이 좁아 세 줄이 붙는다

        // 3. 조작법
        RectTransform controlsPage = UIObject(ready, "ControlsPage", stretch0, stretch1, center, Vector2.zero, Vector2.zero);
        Label(controlsPage, "HeaderText", "CONTROLS", 56, TextAlignmentOptions.Center,
            center, center, center, new Vector2(0f, 340f), new Vector2(1200f, 120f));
        TextMeshProUGUI keys = Label(controlsPage, "KeysText", ControlsText, 26, TextAlignmentOptions.TopRight,
            center, center, new Vector2(1f, 1f), new Vector2(-30f, 240f), new Vector2(500f, 480f));
        keys.color = new Color(1f, 0.85f, 0.3f);
        TextMeshProUGUI meaning = Label(controlsPage, "MeaningText", ControlsMeaning, 26, TextAlignmentOptions.TopLeft,
            center, center, new Vector2(0f, 1f), new Vector2(30f, 240f), new Vector2(900f, 480f));
        // 픽셀 폰트는 줄 간격이 좁다. 두 칸이 같은 줄에 맞도록 같은 값을 준다.
        keys.lineSpacing = meaning.lineSpacing = 60f;
        Label(controlsPage, "PressText", "Press SPACE to start", 32, TextAlignmentOptions.Center,
            center, center, center, new Vector2(0f, -340f), new Vector2(900f, 80f));

        // 시작 화면 글자는 전부 Press Start 2P (도트 폰트). 이 폰트는 폭이 넓어서 위 글자 크기를 작게 잡았다.
        TMP_FontAsset pixelFont = IntroFont();
        if (pixelFont != null)
        {
            foreach (TMP_Text text in ready.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = pixelFont;
                text.fontSharedMaterial = pixelFont.material;
            }
        }

        titlePage.gameObject.SetActive(true);
        selectPage.gameObject.SetActive(false);
        controlsPage.gameObject.SetActive(false);

        // 연결
        var intro = systems.GetComponent<TitleIntro>();
        if (intro == null) intro = systems.gameObject.AddComponent<TitleIntro>();
        Set(intro, ("pages", new Object[] { titlePage.gameObject, selectPage.gameObject, controlsPage.gameObject }),
            ("fadeOverlay", overlay));

        // Space는 TitleIntro가 받는다. 켜 두면 제목 화면에서 바로 시작해 버린다.
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager != null) Set(gameManager, ("startOnSpace", false));

        var select = systems.GetComponent<CharacterSelect>();
        if (select != null)
        {
            Set(select, ("guideText", guide), ("portraitImage", portrait), ("selectPage", selectPage.gameObject));
            var so = new SerializedObject(select);
            SerializedProperty profiles = so.FindProperty("profiles");
            for (int i = 0; i < profiles.arraySize; i++)
            {
                SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
                string dir = profile.FindPropertyRelative("displayName").stringValue == "Archer"
                    ? $"{ArcherDir}/Archer-Idle-spritesheet.png"
                    : $"{SwordsmanDir}/Swordsman_Idle.png";
                profile.FindPropertyRelative("portrait").objectReferenceValue = CharacterSheet(dir).FirstOrDefault();
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[RPGBuilder] GameSystems에 CharacterSelect가 없습니다. RPG > Apply Archer를 먼저 돌리세요.");
        }
    }

    /// <summary>시작 화면 글씨체. Fonts/PressStart2P-Regular.ttf (SIL OFL, PressStart2P-OFL.txt).</summary>
    private static TMP_FontAsset IntroFont()
    {
        // 8px 격자 폰트라 샘플 크기를 8의 배수로 잡아야 획이 고르게 나온다.
        return FontAssetFrom("PressStart2P-Regular.ttf", "PressStart2P SDF", 64, 8,
            GameTitle + ControlsText + ControlsMeaning + GuideBase + "Press SPACE to start CHOOSE YOUR HERO CONTROLS <> SwordsmanArcher LEFT/RIGHT choose character");
    }

    /// <summary>
    /// Fonts/ 폴더의 ttf로 TMP 폰트 에셋을 한 번 만들어 두고 다시 쓴다.
    /// Dynamic이라 미리 넣은 글자 말고도 필요한 글자는 원본 폰트에서 그때그때 채운다.
    /// </summary>
    private static TMP_FontAsset FontAssetFrom(string ttfFile, string assetName, int samplingSize, int padding, string characters)
    {
        string ttfPath = $"{Game}/Fonts/{ttfFile}";
        string assetPath = $"{Game}/Fonts/{assetName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var source = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (source == null)
        {
            Debug.LogWarning($"[RPGBuilder] {ttfPath}가 없어 기본 글씨체로 둡니다.");
            return null;
        }

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, samplingSize, padding, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            1024, 1024, AtlasPopulationMode.Dynamic);
        font.name = assetName;
        AssetDatabase.CreateAsset(font, assetPath);
        // 아틀라스와 머티리얼은 폰트 에셋 안에 같이 저장해야 씬을 다시 열어도 남는다.
        font.atlasTexture.name = $"{assetName} Atlas";
        AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
        font.material.name = $"{assetName} Material";
        AssetDatabase.AddObjectToAsset(font.material, font);
        font.TryAddCharacters(characters);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return font;
    }
}
