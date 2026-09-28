using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 메이플식 포탈. 플레이어가 위에 서서 ↑(또는 W)를 누르면 다른 맵으로 순간이동한다.
/// requiredLevel 미만이면 막고, unlockOnDeath에 보스를 넣으면 그 보스가 죽을 때까지 잠겨 있다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Portal : MonoBehaviour
{
    [Header("목적지")]
    [SerializeField] private MapArea destination;
    [Tooltip("도착 지점. 발 위치 기준.")]
    [SerializeField] private Transform arrivalPoint;
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("입장 조건")]
    [SerializeField, Min(0)] private int requiredLevel = 0;
    [Tooltip("비워두면 처음부터 열려 있다. 넣으면 이 보스가 죽어야 열린다.")]
    [SerializeField] private BossController unlockOnDeath;

    [Header("판정")]
    [Tooltip("포탈 중심에서 이 가로 거리 안에 플레이어 발이 있으면 입장할 수 있다.")]
    [SerializeField, Min(0.1f)] private float useRadius = 0.7f;
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);

    private SpriteRenderer spriteRenderer;
    private Color openColor;
    private bool locked;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        openColor = spriteRenderer.color;
        SetLocked(unlockOnDeath != null);
    }

    private void OnEnable()
    {
        CombatEvents.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        CombatEvents.OnDied -= HandleDied;
    }

    private void HandleDied(GameObject target)
    {
        if (!locked || unlockOnDeath == null || target != unlockOnDeath.gameObject) return;

        SetLocked(false);
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"{unlockOnDeath.DisplayName} defeated! Portal opened", 2.5f);
    }

    private void SetLocked(bool value)
    {
        locked = value;
        spriteRenderer.color = value ? lockedColor : openColor;
    }

    private void Update()
    {
        // 저장을 불러와 보스가 처음부터 꺼져 있으면 (이미 처치함) 조용히 연다.
        if (locked && unlockOnDeath != null && !unlockOnDeath.gameObject.activeInHierarchy) SetLocked(false);

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        PlayerController player = PlayerController.Instance;
        // 상점 창이 열려 있거나 돌진 중일 때는 들어가지 않는다.
        if (player == null || player.IsDead || player.ControlLocked) return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !(kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return;

        Vector2 offset = player.transform.position - transform.position;
        if (Mathf.Abs(offset.x) > useRadius || Mathf.Abs(offset.y) > 2f) return;

        TryEnter(player);
    }

    private void TryEnter(PlayerController player)
    {
        if (locked)
        {
            ShowMessage("Defeat the boss to open this portal");
            return;
        }

        if (player.CurrentLevel < requiredLevel)
        {
            ShowMessage($"Requires Lv.{requiredLevel} (now Lv.{player.CurrentLevel})");
            return;
        }

        if (destination == null || arrivalPoint == null) return;

        // Rigidbody2D는 다음 물리 스텝에 transform을 덮어쓰므로 둘 다 옮긴다.
        if (player.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.position = arrivalPoint.position;
            rb.linearVelocity = Vector2.zero;
        }
        player.transform.position = arrivalPoint.position;

        destination.Enter(cameraFollow);
        ShowMessage(destination.DisplayName);
    }

    // UI 폰트(LiberationSans)에 한글 글리프가 없어서 화면 문구는 영어로 쓴다.
    private static void ShowMessage(string text)
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(text);
    }
}
