using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GhostCounselor
{
    /// <summary>Runs the opening story one streaming paragraph at a time.</summary>
    public sealed class PrologueController : MonoBehaviour
    {
        private const string StoryResourcePath = "Stories/Prologue";

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

            TextAsset story = Resources.Load<TextAsset>(StoryResourcePath);
            storyParagraphs = story != null
                ? story.text.Replace("\r", string.Empty).Trim()
                    .Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(paragraph => paragraph.Trim())
                    .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                    .ToArray()
                : new[] { "프롤로그 원고를 불러오지 못했습니다." };

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
            currentParagraph = storyParagraphs[paragraphIndex];
            ui.storyText.text = string.Empty;
            ui.storyText.alignment = TextAnchor.MiddleCenter;
            ui.storyText.fontSize = isFinalParagraph ? 44 : 31;
            ui.continueButtonText.text = isFinalParagraph ? "시작하기" : "다음  >";

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
            PrologueScreenFade.FadeFromBlackAndLoadScene("GameScene", blackHoldDuration, revealDuration);
        }

        private void SkipPrologue()
        {
            ClosePrologue();
        }
    }
}
