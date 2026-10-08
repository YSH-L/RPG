using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 승리 연출: 보스를 전부 잡으면 5초 동안 화면이 하얘진 뒤 결과 화면이 뜬다.
/// <b>RPG &gt; Build Game</b>이 마지막에 부르고, 지금 씬에만 넣고 싶으면 <b>RPG &gt; Apply Victory Fade (current scene)</b>.
/// </summary>
public static partial class RPGBuilder
{
    [MenuItem("RPG/Apply Victory Fade (current scene)")]
    public static void ApplyVictoryFadeToOpenScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[RPGBuilder] 플레이 모드에서는 적용할 수 없습니다.");
            return;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        GameFlow flow = Object.FindAnyObjectByType<GameFlow>(FindObjectsInactive.Include);
        if (canvas == null || flow == null)
        {
            Debug.LogError("[RPGBuilder] 씬에 Canvas·GameFlow가 있어야 합니다. RPG > Build Game을 먼저 돌리세요.");
            return;
        }

        BuildVictoryFade(canvas.transform, flow);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Debug.Log("[RPGBuilder] 승리 연출을 넣었습니다.");
    }

    private static void BuildVictoryFade(Transform canvas, GameFlow flow)
    {
        Transform old = canvas.Find("WhiteOverlay");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // 게임 화면과 HUD는 덮고, 결과 화면 글자는 그 위에 보이도록 GameOver 패널 바로 앞에 둔다.
        Image white = Panel(canvas, "WhiteOverlay", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            new Color(1f, 1f, 1f, 0f));
        white.raycastTarget = false;
        Transform gameOver = canvas.Find("GameOver_Panel");
        if (gameOver != null) white.transform.SetSiblingIndex(gameOver.GetSiblingIndex());
        white.gameObject.SetActive(false);

        Set(flow, ("whiteOverlay", white), ("whiteFadeDuration", 5f), ("victoryDelay", 0f),
            ("victoryTitleFont", VictoryFont()));
    }

    /// <summary>승리 제목 글씨체. Fonts/Metamorphous-Regular.ttf (SIL OFL, Metamorphous-OFL.txt).</summary>
    private static TMP_FontAsset VictoryFont()
    {
        return FontAssetFrom("Metamorphous-Regular.ttf", "Metamorphous SDF", 90, 9, "VICTORY! Victory");
    }
}
