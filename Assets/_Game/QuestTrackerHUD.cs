using TMPro;
using UnityEngine;

/// <summary>
/// 화면 오른쪽 위의 퀘스트 추적 창. 지금 할 일(누구에게 가라 / 목표 진행도 / 보고하러 가라)을 보여준다.
/// PlayerQuests.OnChanged로 다시 그린다. HUD_Panel 안에 둔다.
/// </summary>
public class QuestTrackerHUD : MonoBehaviour
{
    [SerializeField] private PlayerQuests quests;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private void OnEnable()
    {
        if (quests == null) return;
        quests.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (quests != null) quests.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        QuestData quest = quests.CurrentQuest;

        if (quest == null)
        {
            SetText("Main Quest", "All quests complete!");
            return;
        }

        if (!quests.IsAccepted)
        {
            SetText(quest.title, $"Talk to {quest.giverName}  <color=#FFD24D>!</color>");
            return;
        }

        string body = "";
        for (int i = 0; i < quest.objectives.Length; i++)
        {
            var (current, required) = quests.GetProgress(i);
            string color = current >= required ? "#8CFF8C" : "#FFFFFF";
            body += $"<color={color}>- {quest.objectives[i].Label}  {current}/{required}</color>\n";
        }
        if (quests.IsReadyToReport) body += $"<color=#FFD24D>Report to {quest.reporterName}  ?</color>";

        SetText(quest.title, body.TrimEnd('\n'));
    }

    private void SetText(string title, string body)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
    }
}
