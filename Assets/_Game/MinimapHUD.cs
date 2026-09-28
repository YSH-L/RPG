using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 왼쪽 위의 단순화 미니맵 (메이플식). 지금 있는 맵의 땅을 막대로, 플레이어·몬스터·보스·NPC·포탈을 색 점으로 보여준다.
/// M 키로 켜고 끈다. HUD_Panel 안에 둔다.
/// <para>
/// 지금 맵은 이벤트가 아니라 플레이어 x좌표로 정한다 — HUD가 꺼져 있는 Ready 동안
/// 저장 불러오기로 맵이 바뀌어도 놓치지 않기 위해서다.
/// </para>
/// </summary>
public class MinimapHUD : MonoBehaviour
{
    [Tooltip("켜고 끌 미니맵 본체. 이 스크립트가 붙은 오브젝트 자신이 아니라 자식이어야 M 키를 계속 받는다.")]
    [SerializeField] private GameObject content;
    [Tooltip("점과 땅 막대를 그리는 영역.")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform groundBar;
    [SerializeField] private TMP_Text mapNameText;
    [Tooltip("점 원본. 비활성으로 두면 여기서 복제해 쓴다.")]
    [SerializeField] private Image markerTemplate;
    [SerializeField] private MapArea[] areas;

    [Header("색·크기")]
    [SerializeField] private Color playerColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color bossColor = new Color(1f, 0.2f, 1f);
    [SerializeField] private Color npcColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField] private Color portalColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private Color lockedPortalColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField, Min(0.1f)] private float enemyRefreshInterval = 0.5f;

    private readonly List<Image> markers = new List<Image>();
    private readonly List<EnemyBase> enemies = new List<EnemyBase>();
    private ShopNPC[] shops;
    private QuestNPC[] questNpcs;
    private Portal[] portals;
    private float enemyRefreshTimer;
    private MapArea shownArea;
    private int used;

    private void Awake()
    {
        if (markerTemplate != null) markerTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // NPC와 포탈은 움직이거나 늘지 않으니 한 번만 찾는다.
        shops = FindObjectsByType<ShopNPC>(FindObjectsSortMode.None);
        questNpcs = FindObjectsByType<QuestNPC>(FindObjectsSortMode.None);
        portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        enemyRefreshTimer = 0f;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame && content != null) content.SetActive(!content.activeSelf);
        }

        if (content == null || !content.activeSelf) return;

        PlayerController player = PlayerController.Instance;
        if (player == null) return;

        MapArea area = FindArea(player.transform.position.x);
        if (area == null) return;
        if (area != shownArea) ShowArea(area);

        // 몬스터는 풀에서 켜졌다 꺼졌다 하므로 주기적으로 다시 모은다 (매 프레임 찾으면 비싸다).
        enemyRefreshTimer -= Time.unscaledDeltaTime;
        if (enemyRefreshTimer <= 0f)
        {
            enemyRefreshTimer = enemyRefreshInterval;
            enemies.Clear();
            enemies.AddRange(FindObjectsByType<EnemyBase>(FindObjectsSortMode.None));
        }

        used = 0;
        foreach (Portal p in portals)
        {
            if (p != null && p.isActiveAndEnabled) Place(area, p.transform.position, p.IsLocked ? lockedPortalColor : portalColor, 10f);
        }
        foreach (ShopNPC npc in shops)
        {
            if (npc != null && npc.isActiveAndEnabled) Place(area, npc.transform.position, npcColor, 9f);
        }
        foreach (QuestNPC npc in questNpcs)
        {
            if (npc != null && npc.isActiveAndEnabled) Place(area, npc.transform.position, npcColor, 9f);
        }
        foreach (EnemyBase e in enemies)
        {
            if (e == null || !e.isActiveAndEnabled || e.IsDead) continue;
            bool isBoss = e is BossController;
            Place(area, e.transform.position, isBoss ? bossColor : enemyColor, isBoss ? 14f : 7f);
        }
        // 플레이어는 다른 점에 가리지 않도록 맨 위로 올린다.
        Image me = Place(area, player.transform.position, playerColor, 11f);
        if (me != null) me.transform.SetAsLastSibling();

        for (int i = used; i < markers.Count; i++) markers[i].gameObject.SetActive(false);
    }

    private MapArea FindArea(float x)
    {
        if (shownArea != null && shownArea.ContainsX(x)) return shownArea;
        if (areas == null) return null;
        foreach (MapArea a in areas)
        {
            if (a != null && a.ContainsX(x)) return a;
        }
        return shownArea;
    }

    private void ShowArea(MapArea area)
    {
        shownArea = area;
        if (mapNameText != null) mapNameText.text = area.DisplayName;

        // 땅(y=0)을 가로지르는 막대
        if (groundBar != null)
        {
            float y = ToViewport(area, new Vector2(area.WorldMin.x, 0f)).y;
            groundBar.anchoredPosition = new Vector2(0f, y);
        }
    }

    private Image Place(MapArea area, Vector3 worldPos, Color color, float size)
    {
        if (!area.ContainsX(worldPos.x)) return null;

        Image marker = GetMarker();
        marker.color = color;
        marker.rectTransform.sizeDelta = new Vector2(size, size);
        marker.rectTransform.anchoredPosition = ToViewport(area, worldPos);
        return marker;
    }

    /// <summary>월드 좌표 → viewport 왼쪽 아래 기준 좌표.</summary>
    private Vector2 ToViewport(MapArea area, Vector2 worldPos)
    {
        Vector2 size = viewport.rect.size;
        Vector2 min = area.WorldMin;
        Vector2 max = area.WorldMax;
        float nx = Mathf.InverseLerp(min.x, max.x, worldPos.x);
        float ny = Mathf.InverseLerp(min.y, max.y, worldPos.y);
        return new Vector2(nx * size.x, ny * size.y);
    }

    private Image GetMarker()
    {
        if (used >= markers.Count)
        {
            // UI 점이라 필요할 때 늘린다. 한 번 만든 것은 끄고 켜며 계속 쓴다.
            Image created = Instantiate(markerTemplate, viewport);
            created.name = "Marker";
            created.rectTransform.anchorMin = Vector2.zero;
            created.rectTransform.anchorMax = Vector2.zero;
            created.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            markers.Add(created);
        }

        Image marker = markers[used++];
        if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
        return marker;
    }
}
