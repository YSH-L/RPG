using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 퀘스트를 주고받는 NPC. 앞에 서서 ↑(또는 W)를 누르면 대화창이 열린다.
/// 머리 위 표시: <b>!</b> 줄 퀘스트가 있음, <b>?</b> 보고받을 퀘스트가 완료됨, <b>…</b> 진행 중.
/// </summary>
public class QuestNPC : MonoBehaviour
{
    [SerializeField] private string npcName = "Chief";
    [Tooltip("이 NPC가 주는 퀘스트.")]
    [SerializeField] private QuestData[] offers;
    [Tooltip("이 NPC에게 보고하는 퀘스트. 주는 NPC와 달라도 된다.")]
    [SerializeField] private QuestData[] completes;
    [SerializeField] private QuestDialogWindow window;
    [SerializeField] private TMP_Text marker;
    [SerializeField, Min(0.1f)] private float useRadius = 0.9f;

    public string NpcName => npcName;
    public bool Offers(QuestData quest) => quest != null && Array.IndexOf(offers, quest) >= 0;
    public bool Completes(QuestData quest) => quest != null && Array.IndexOf(completes, quest) >= 0;

    private void Update()
    {
        UpdateMarker();

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (window == null || window.IsOpen) return;

        PlayerController player = PlayerController.Instance;
        if (player == null || player.IsDead || player.ControlLocked) return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !(kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return;

        Vector2 offset = player.transform.position - transform.position;
        if (Mathf.Abs(offset.x) > useRadius || Mathf.Abs(offset.y) > 2f) return;

        window.Open(this);
    }

    private void UpdateMarker()
    {
        if (marker == null || window == null || window.Quests == null) return;

        PlayerQuests quests = window.Quests;
        QuestData current = quests.CurrentQuest;
        string text = "";

        if (current != null)
        {
            if (!quests.IsAccepted && Offers(current)) text = "!";
            else if (quests.IsReadyToReport && Completes(current)) text = "?";
            else if (quests.IsAccepted && (Offers(current) || Completes(current))) text = "...";
        }

        if (marker.text != text) marker.text = text;
    }
}
