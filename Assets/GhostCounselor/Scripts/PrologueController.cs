/*
 * 파일 역할: PrologueScene의 프롤로그 진행과 타이핑 연출을 담당한다.
 * - Awake(): UI 참조를 확인하고 Resources/Stories/Prologue 원고를 읽는다.
 * - ShowCurrentLine(): 현재 문단과 버튼 문구를 화면에 준비한다.
 * - TypeCurrentLine(): 글자를 한 자씩 출력하고 문장부호에서 잠시 멈춘다.
 * - ShowNextLine(): 타이핑 중이면 즉시 완성하고, 다음 문단 또는 GameScene으로 이동한다.
 * - SkipPrologue(): 프롤로그를 건너뛰고 화면 전환 후 GameScene을 연다.
 */
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor
{
    /// <summary>Runs the opening story one streaming paragraph at a time.</summary>
    public sealed class PrologueController : MonoBehaviour
    {
        private const string StoryResourcePath = "Stories/Prologue";
        private static bool endingModeRequested;

        [SerializeField] private PrologueUIReferences ui;
        [Header("Streaming text")]
        [SerializeField, Range(0.01f, 0.15f)] private float letterDelay = 0.035f;
        [SerializeField, Range(0.05f, 0.5f)] private float punctuationPause = 0.16f;
        [Header("Start transition")]
        [SerializeField, Range(0f, 1f)] private float blackHoldDuration = 0.18f;
        [SerializeField, Range(0.3f, 3f)] private float revealDuration = 1.15f;

        private string[] storyParagraphs;
        private int paragraphIndex;
        private string currentParagraph;
        private Color storyColor;
        private Coroutine typeRoutine;
        private bool isTyping;
        private bool isClosing;
        private bool isEnding;

        public static void RequestEnding()
        {
            endingModeRequested = true;
            PrologueScreenFade.FadeFromBlackAndLoadScene("PrologueScene", 0.18f, 1.15f);
        }

        private void Awake()
        {
            ui ??= GetComponent<PrologueUIReferences>();
            if (ui == null || !ui.IsConfigured)
            {
                Debug.LogError("[귀신 상담소] 프롤로그 UI 참조가 연결되지 않았습니다.");
                return;
            }

            Font koreanFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Pretendard", "Noto Serif KR", "NanumMyeongjo", "Malgun Gothic", "맑은 고딕", "LegacyRuntime.ttf" }, 24);
            ui.storyText.font = koreanFont;
            ui.storyText.fontStyle = FontStyle.Bold;
            ui.continueButtonText.font = koreanFont;
            if (ui.gameCamera != null)
                ui.gameCamera.enabled = true;
            ui.prologueCamera.enabled = true;

            isEnding = endingModeRequested;
            if (isEnding)
            {
                storyParagraphs = BuildEndingParagraphs();
            }
            else
            {
                TextAsset story = Resources.Load<TextAsset>(StoryResourcePath);
                storyParagraphs = story != null
                    ? story.text.Replace("\r", string.Empty).Trim()
                        .Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(paragraph => paragraph.Trim())
                        .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                        .ToArray()
                    : new[] { "프롤로그 원고를 불러오지 못했습니다." };
            }

            storyColor = ui.storyText.color;
            ui.continueButton.onClick.RemoveAllListeners();
            ui.continueButton.onClick.AddListener(ShowNextLine);
            if (ui.skipButton != null)
            {
                ui.skipButton.onClick.RemoveAllListeners();
                ui.skipButton.onClick.AddListener(SkipPrologue);
                if (ui.skipButtonText != null)
                    ui.skipButtonText.font = koreanFont;
            }
            ShowCurrentLine();
        }

        private void ShowNextLine()
        {
            if (isTyping)
            {
                CompleteCurrentLine();
                return;
            }

            if (paragraphIndex >= storyParagraphs.Length - 1)
            {
                ClosePrologue();
                return;
            }

            paragraphIndex++;
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            bool isFinalParagraph = paragraphIndex == storyParagraphs.Length - 1;
            bool isEndingCredits = isEnding && isFinalParagraph;
            currentParagraph = storyParagraphs[paragraphIndex];
            ui.storyText.text = string.Empty;
            ui.storyText.alignment = TextAnchor.MiddleCenter;
            ui.storyText.fontSize = isEndingCredits ? 25 : isFinalParagraph ? 44 : 31;
            ui.continueButtonText.text = isFinalParagraph
                ? (isEnding ? "처음부터 다시" : "시작하기")
                : "다음  >";

            if (typeRoutine != null)
                StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypeCurrentLine());
        }

        private IEnumerator TypeCurrentLine()
        {
            isTyping = true;
            for (int index = 0; index < currentParagraph.Length; index++)
            {
                ui.storyText.text = currentParagraph.Substring(0, index + 1) + "<color=#CBAF7A>▌</color>";
                float pause = IsPunctuation(currentParagraph[index]) ? punctuationPause : letterDelay;
                yield return new WaitForSecondsRealtime(pause);
            }

            ui.storyText.text = currentParagraph;
            isTyping = false;
            typeRoutine = null;
        }

        private void CompleteCurrentLine()
        {
            if (typeRoutine != null)
                StopCoroutine(typeRoutine);

            ui.storyText.text = currentParagraph;
            isTyping = false;
            typeRoutine = null;
        }

        private static bool IsPunctuation(char character)
        {
            return character == '.' || character == '…' || character == ',' ||
                   character == '?' || character == '!' || character == '。';
        }

        private void ClosePrologue()
        {
            if (isClosing)
                return;

            isClosing = true;
            if (isEnding)
            {
                endingModeRequested = false;
                GhostSaveSystem.Delete();
                PrologueScreenFade.FadeFromBlackAndLoadScene("PrologueScene", blackHoldDuration, revealDuration);
                return;
            }

            PrologueScreenFade.FadeFromBlackAndLoadScene("GameScene", blackHoldDuration, revealDuration);
        }

        private void SkipPrologue()
        {
            ClosePrologue();
        }

        private static string[] BuildEndingParagraphs()
        {
            SaveData save = GhostSaveSystem.Load();
            int totalReceived = (save.ledgerRecords ?? new System.Collections.Generic.List<LedgerRecord>())
                .Sum(record => record.money);
            int counselCount = save.ledgerRecords?.Count ?? 0;
            int purifiedCount = save.ghosts?.Count(progress => progress.purified) ?? 0;
            int specialCount = save.ghosts?.Count(progress => progress.specialSolved) ?? 0;

            return new[]
            {
                "일주일 동안의 귀신 상담소가 끝났다…",
                "정말 다행히도 잔금과 빚을 모두 갚았다.\n신당을 지킬 수 있게 되었다.",
                "처음에는 귀신들이 무섭기만 했다.\n하지만 이야기를 나눌수록, 귀신들에게도 각자의 아픔과 고통이 있다는 걸 알게 되었다.",
                "그들의 마지막 이야기를 들어주고 성불을 배웅할 수 있어서 뿌듯했다.\n앞으로도 이곳에서 귀신 상담소를 계속해 나가기로 했다.",
                "플레이해 주셔서 감사합니다.",
                $"— 플레이 기록 —\n\n플레이 시간  {FormatPlayTime(save.elapsedPlaySeconds)}\n상담 횟수  {counselCount}회\n귀신 성불  {purifiedCount}/5명\n귀신에게 받은 돈  {totalReceived:N0}원\n특별 해결  {specialCount}건\n\n귀신 상담소 제작진"
            };
        }

        private static string FormatPlayTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}분 {remainingSeconds:00}초";
        }
    }
}
