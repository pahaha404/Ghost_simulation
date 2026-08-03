using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GhostCounselor
{
    /// <summary>
    /// Runtime behavior for the two desk interactions authored by ArchiveInteractionUIBuilder.
    /// The book shows one durable ledger list; the tablet groups those same records per ghost.
    /// </summary>
    public sealed class GhostArchiveUI : MonoBehaviour
    {
        [Header("Desk click areas")]
        public Button ledgerHotspot;
        public Button tabletHotspot;

        [Header("Ledger book")]
        public GameObject ledgerWindow;
        public Text ledgerListText;
        public Button ledgerCloseButton;

        [Header("Tablet codex")]
        public GameObject tabletWindow;
        public Button tabletCloseButton;
        public RectTransform codexGrid;
        public Button ghostCardTemplate;
        public Text emptyCodexText;

        [Header("Tablet detail")]
        public GameObject detailWindow;
        public Button detailCloseButton;
        public Image detailPortrait;
        public Text detailNameText;
        public Text detailStatusText;
        public Text detailBodyText;

        private SaveData save;
        private IReadOnlyList<GhostDefinition> ghosts = Array.Empty<GhostDefinition>();
        private GhostCounselorUIReferences ui;
        private bool wired;

        public bool IsShowingAnyWindow =>
            (ledgerWindow != null && ledgerWindow.activeSelf) ||
            (tabletWindow != null && tabletWindow.activeSelf) ||
            (detailWindow != null && detailWindow.activeSelf);

        private void Awake()
        {
            // "Archive Interactions - Edit Here" is a transparent full-screen holder.
            // Only its child hotspots (the desk book/tablet) should receive clicks;
            // otherwise it sits above the runtime "Next" button and blocks progress.
            Transform holder = transform.Find("Root/Archive Interactions - Edit Here");
            Image holderImage = holder != null ? holder.GetComponent<Image>() : null;
            if (holderImage != null)
                holderImage.raycastTarget = false;
        }

        public void Bind(SaveData gameSave, IReadOnlyList<GhostDefinition> definitions, GhostCounselorUIReferences uiReferences)
        {
            save = gameSave;
            ghosts = definitions ?? Array.Empty<GhostDefinition>();
            ui = uiReferences;
            save.ledgerRecords ??= new List<LedgerRecord>();
            if (ui != null && ui.ledgerPanel != null)
                ui.ledgerPanel.gameObject.SetActive(false);
            WireButtons();
            HideAll();
        }

        public void RecordCounsel(int day, GhostDefinition ghost, CounselResult result, string answer)
        {
            if (save == null || ghost == null || result == null)
                return;

            save.ledgerRecords ??= new List<LedgerRecord>();
            save.ledgerRecords.Add(new LedgerRecord
            {
                day = day,
                ghostId = ghost.id,
                ghostName = ghost.displayName,
                money = result.TotalPay,
                summary = SummaryFor(result.intent),
                outcome = result.outcome
            });

            if (ledgerWindow != null && ledgerWindow.activeSelf)
                RefreshLedger();
            if (tabletWindow != null && tabletWindow.activeSelf)
                RefreshCodex();
        }

        private void WireButtons()
        {
            if (wired)
                return;

            wired = true;
            Wire(ledgerHotspot, OpenLedger);
            Wire(tabletHotspot, OpenTablet);
            Wire(ledgerCloseButton, HideAll);
            Wire(tabletCloseButton, HideAll);
            Wire(detailCloseButton, CloseDetail);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void OpenLedger()
        {
            if (save == null || ledgerWindow == null)
                return;

            HideAll();
            ledgerWindow.SetActive(true);
            RefreshLedger();
        }

        private void OpenTablet()
        {
            if (save == null || tabletWindow == null)
                return;

            HideAll();
            tabletWindow.SetActive(true);
            RefreshCodex();
        }

        private void HideAll()
        {
            if (ledgerWindow != null)
                ledgerWindow.SetActive(false);
            if (tabletWindow != null)
                tabletWindow.SetActive(false);
            if (detailWindow != null)
                detailWindow.SetActive(false);
        }

        private void CloseDetail()
        {
            if (detailWindow != null)
                detailWindow.SetActive(false);
        }

        /// <summary>
        /// Esc와 화면의 닫기 버튼이 반드시 같은 동작을 실행하도록 한다.
        /// 상세 창이 먼저 열려 있으면 상세 창만 닫고, 그 외에는 장부/태블릿 전체를 닫는다.
        /// </summary>
        public bool TryCloseFromKeyboard()
        {
            if (detailWindow != null && detailWindow.activeSelf)
            {
                InvokeClose(detailCloseButton, CloseDetail);
                return true;
            }

            if (ledgerWindow != null && ledgerWindow.activeSelf)
            {
                InvokeClose(ledgerCloseButton, HideAll);
                return true;
            }

            if (tabletWindow != null && tabletWindow.activeSelf)
            {
                InvokeClose(tabletCloseButton, HideAll);
                return true;
            }

            return false;
        }

        private static void InvokeClose(Button button, Action fallback)
        {
            if (button != null && button.interactable)
                button.onClick.Invoke();
            else
                fallback?.Invoke();
        }

        private void RefreshLedger()
        {
            if (ledgerListText == null)
                return;

            IReadOnlyList<LedgerRecord> records = save.ledgerRecords ?? new List<LedgerRecord>();
            if (records.Count == 0)
            {
                ledgerListText.text = "아직 적힌 상담 기록이 없습니다.";
                return;
            }

            ledgerListText.text = string.Join("\n\n", records
                .OrderBy(record => record.day)
                .Select(record =>
                    $"{record.day}일차  ·  {record.ghostName}  ·  +{record.money:N0}원  ·  {record.summary}  {StatusIcon(record.outcome)}"));
        }

        private void RefreshCodex()
        {
            if (codexGrid == null || ghostCardTemplate == null || save == null)
                return;

            for (int index = codexGrid.childCount - 1; index >= 0; index--)
            {
                Transform child = codexGrid.GetChild(index);
                if (child != ghostCardTemplate.transform)
                    Destroy(child.gameObject);
            }

            List<GhostDefinition> metGhosts = ghosts.Where(ghost =>
                save.ghosts.Any(progress => progress.ghostId == ghost.id && progress.visitCount > 0)).ToList();
            if (emptyCodexText != null)
                emptyCodexText.gameObject.SetActive(metGhosts.Count == 0);

            foreach (GhostDefinition ghost in metGhosts)
            {
                Button card = Instantiate(ghostCardTemplate, codexGrid);
                card.gameObject.SetActive(true);
                card.gameObject.name = $"{ghost.displayName} Card";

                Image portrait = card.transform.Find("Portrait")?.GetComponent<Image>();
                if (portrait != null)
                {
                    portrait.sprite = PortraitFor(ghost.id);
                    portrait.preserveAspect = true;
                }

                Text name = card.transform.Find("Name")?.GetComponent<Text>();
                GhostProgress progress = ProgressFor(ghost.id);
                if (name != null)
                    name.text = $"{ghost.displayName}\n{StatusIcon(LatestOutcomeFor(ghost.id))}";

                card.onClick.RemoveAllListeners();
                card.onClick.AddListener(() => ShowDetail(ghost, progress));
            }
        }

        private void ShowDetail(GhostDefinition ghost, GhostProgress progress)
        {
            if (detailWindow == null || ghost == null)
                return;

            detailWindow.SetActive(true);
            if (detailPortrait != null)
            {
                detailPortrait.sprite = PortraitFor(ghost.id);
                detailPortrait.preserveAspect = true;
            }

            List<LedgerRecord> records = (save.ledgerRecords ?? new List<LedgerRecord>())
                .Where(record => record.ghostId == ghost.id)
                .OrderBy(record => record.day)
                .ToList();
            CounselOutcome latest = records.Count > 0 ? records[^1].outcome : CounselOutcome.Unresolved;

            if (detailNameText != null)
                detailNameText.text = $"{ghost.displayName}\n{ghost.title}";
            if (detailStatusText != null)
                detailStatusText.text = StatusIcon(latest);
            if (detailBodyText != null)
            {
                string history = records.Count == 0
                    ? "아직 남은 상담 기록이 없습니다."
                    : string.Join("\n", records.Select(record =>
                        $"{record.day}일차 · +{record.money:N0}원 · {record.summary} {StatusIcon(record.outcome)}"));
                detailBodyText.text =
                    $"방문 {progress?.visitCount ?? 0}회  ·  관계 {RelationshipIcon(progress?.relationship ?? 0)}\n\n" +
                    $"상담 기록\n{history}";
            }
        }

        private GhostProgress ProgressFor(string ghostId) => save.ghosts.FirstOrDefault(progress => progress.ghostId == ghostId);

        private CounselOutcome LatestOutcomeFor(string ghostId)
        {
            LedgerRecord record = (save.ledgerRecords ?? new List<LedgerRecord>())
                .LastOrDefault(item => item.ghostId == ghostId);
            return record != null ? record.outcome : CounselOutcome.Unresolved;
        }

        private Sprite PortraitFor(string ghostId)
        {
            GhostPortraitSet set = ui?.ghostPortraits?.FirstOrDefault(item => item != null && item.ghostId == ghostId);
            return set?.defaultPortrait;
        }

        private static string SummaryFor(AnswerIntent intent) => intent switch
        {
            AnswerIntent.Empathy => "마음을 들어 주었다",
            AnswerIntent.PracticalAdvice => "현실적인 길을 함께 찾았다",
            AnswerIntent.Avoidance => "마음을 정리할 시간을 주었다",
            AnswerIntent.Aggression => "마음을 상하게 했다",
            AnswerIntent.Timeout => "대답을 마치지 못했다",
            _ => "대화가 닿지 않았다"
        };

        private static string StatusIcon(CounselOutcome outcome) => outcome switch
        {
            CounselOutcome.Partial => "😐",
            CounselOutcome.Solved => "😊",
            CounselOutcome.SpecialSolved => "✨",
            _ => "😞"
        };

        private static string RelationshipIcon(int value) => value switch
        {
            >= 3 => "💗",
            >= 1 => "🙂",
            <= -1 => "💢",
            _ => "·"
        };
    }
}
