using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// QuestNPC와의 대화창. 지금 퀘스트 상태에 따라 [Accept] / [Complete] / [OK] 중 하나를 보여준다.
/// 열려 있는 동안 플레이어는 잠기고 무적이다 (ShopWindow와 같은 이유로 게임을 멈추지 않는다).
/// 닫기: [Close] 버튼, 또는 ↑/W를 한 번 더. HUD_Panel 안에 둔다.
/// </summary>
public class QuestDialogWindow : MonoBehaviour
{
    [SerializeField] private PlayerQuests quests;
    [SerializeField] private PlayerController player;
    [Tooltip("켜고 끌 창 본체. 이 스크립트가 붙은 오브젝트 자신이 아니라 자식이어야 한다.")]
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Button closeButton;

    private enum Mode { Talk, Offer, Report }

    private QuestNPC npc;
    private Mode mode;
    private int openedFrame;

    public PlayerQuests Quests => quests;
    public bool IsOpen => content != null && content.activeSelf;

    private void Awake()
    {
        if (content != null) content.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (actionButton != null) actionButton.onClick.AddListener(HandleAction);
    }

    private void OnDisable()
    {
        // HUD가 통째로 꺼지면(GameOver 등) 플레이어 잠금이 남지 않게 한다.
        if (IsOpen) Close();
    }

    public void Open(QuestNPC speaker)
    {
        if (content == null || player == null || quests == null) return;

        npc = speaker;
        openedFrame = Time.frameCount;
        content.SetActive(true);

        player.ControlLocked = true;
        if (player.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        Refresh();
    }

    public void Close()
    {
        if (content != null) content.SetActive(false);
        npc = null;
        if (player != null) player.ControlLocked = false;
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (player != null) player.GrantInvulnerability(0.2f);

        // 여는 데 쓴 ↑ 입력으로 바로 닫히지 않게, 연 프레임은 건너뛴다.
        Keyboard kb = Keyboard.current;
        if (kb != null && Time.frameCount > openedFrame &&
            (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
        {
            Close();
        }
    }

    private void HandleAction()
    {
        switch (mode)
        {
            case Mode.Offer:
                quests.Accept();
                Close();
                break;
            case Mode.Report:
                quests.TryReport();
                // 같은 NPC가 다음 퀘스트도 준다면 이어서 보여준다.
                if (npc != null && npc.Offers(quests.CurrentQuest)) Refresh();
                else Close();
                break;
            default:
                Close();
                break;
        }
    }

    private void Refresh()
    {
        if (npc == null) return;

        QuestData quest = quests.CurrentQuest;
        if (speakerText != null) speakerText.text = npc.NpcName;

        if (quest != null && quests.IsReadyToReport && npc.Completes(quest))
        {
            mode = Mode.Report;
            SetBody($"<b>{quest.title}</b>\n\n{quest.completionText}\n\n<color=#FFD24D>{quest.RewardText}</color>", "Complete");
        }
        else if (quest != null && !quests.IsAccepted && npc.Offers(quest))
        {
            mode = Mode.Offer;
            SetBody($"<b>{quest.title}</b>\n\n{quest.description}\n\n{ObjectiveLines(quest)}\n<color=#FFD24D>{quest.RewardText}</color>", "Accept");
        }
        else if (quest != null && quests.IsAccepted && (npc.Offers(quest) || npc.Completes(quest)))
        {
            mode = Mode.Talk;
            string where = quests.IsReadyToReport ? $"\n\nReport to {quest.reporterName}." : "";
            SetBody($"<b>{quest.title}</b>\n\nHow is it going?\n\n{ObjectiveLines(quest)}{where}", "OK");
        }
        else
        {
            mode = Mode.Talk;
            string hint = quest == null ? "You have done everything I could ask. Thank you, hero."
                        : !quests.IsAccepted ? $"{quest.giverName} is looking for you."
                        : "Good luck on your journey.";
            SetBody(hint, "OK");
        }
    }

    private string ObjectiveLines(QuestData quest)
    {
        string text = "";
        for (int i = 0; i < quest.objectives.Length; i++)
        {
            var (current, required) = quests.GetProgress(i);
            text += $"- {quest.objectives[i].Label}  {current}/{required}\n";
        }
        return text;
    }

    private void SetBody(string body, string action)
    {
        if (bodyText != null) bodyText.text = body;
        if (actionText != null) actionText.text = action;
    }
}
