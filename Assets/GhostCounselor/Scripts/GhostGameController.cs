/*
 * 파일 역할: GameScene에서 상담 게임의 전체 진행을 제어하는 중심 MonoBehaviour다.
 * - Bootstrap()/CreateForGameScene(): GameScene이 열릴 때 컨트롤러를 한 번 생성한다.
 * - Awake(): 귀신 콘텐츠, 저장 데이터, 의도 분류기와 Scene UI를 연결한다.
 * - Update(): 핵심 답변 10초 타이머, 5초 경고 표정, 초상화 흔들림을 처리한다.
 * - 질문/답변 처리: 선택지 또는 자연어 답변을 받아 AnswerIntent로 분류한다.
 * - 결과 처리: 상담 결과, 사례비, 보너스, 물건, 관계 변화를 계산한다.
 * - UI 갱신: 현재 날짜, 단계, 대사, 초상화, 선택지, 결과 장부를 표시한다.
 * GameScene의 배경과 수동 배치 UI는 유지하고 연결된 UI의 내용과 상태만 갱신한다.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor
{
    public sealed class GhostGameController : MonoBehaviour
    {
        private const int PrototypeDays = 7;
        private const float AnswerSeconds = 10f;
        private const float ScaryThreshold = 5f;
        private const float ActionGapBelowDialogue = 35f;
        private const int CounselsPerGhost = 4;
        // 1일 차에는 두 명, 2~7일 차에는 세 명을 상담한다. 총 20회다.
        private static readonly int[] DailyCounselTargets = { 2, 3, 3, 3, 3, 3, 3 };

        private static readonly string[] TitlePages =
        {
            "폐업까지 남은 시간, 단 7일.",
            "불행인지 다행인지 마지못해 상담해 준 귀신이 홍보 역할을 톡톡히 했나 보다.",
            "자기 얘기 좀 들어 달라고 몰려드는 귀신들 때문에 아주 난리도 아니야~",
            "상담을 잘하면 돈도 벌고, 귀신과 친해지면 특별한 보상도 얻는다.",
            "하지만 상담을 잘못하면 귀신이 화를 내고, 저주를 내리고 신당의 평판이 떨어진다.",
            "하루 한 명의 귀신을 상담하고 일주일 동안 신당의 월세와 빚을 갚아 보자."
        };

        private readonly Color ink = Hex("2A2027");
        private readonly Color paper = Hex("F2E7CF");
        private readonly Color paperDark = Hex("D9C7A5");
        private readonly Color accent = Hex("A9403A");
        private readonly Color spirit = Hex("59786F");

        private IReadOnlyList<GhostDefinition> ghosts;
        private IIntentClassifier classifier;
        private SaveData save;
        private GhostDefinition currentGhost;
        private GhostProgress currentProgress;
        // 현재 상담에 표시하는 고정 원고다. 상담 결과가 나온 뒤에도 후속 행동/성불 연출에 사용한다.
        private GhostStoryVisitData currentStoryVisit;
        private bool currentVisitCompleted;
        private bool currentVisitPurified;
        private GamePhase phase;
        private int askedQuestions;
        private int titlePage;
        private readonly HashSet<int> usedQuestions = new();
        private float answerTime;
        private bool scary;

        private Font font;
        private Canvas canvas;
        private RectTransform root;
        private Text dayText;
        private Text moneyText;
        private Text phaseText;
        private Text nameText;
        private Text titleText;
        private Image portraitImage;
        private GhostPortraitSet[] ghostPortraits = Array.Empty<GhostPortraitSet>();
        private Text dialogueText;
        private Text timerText;
        private RectTransform content;
        private RectTransform actionArea;
        private GhostLedgerPresenter ledger;
        private Image portraitPanel;
        private InputField answerInput;
        private GhostCounselorUIReferences editableUi;
        private GhostInnerThoughtModal innerThoughtModal;
        private GhostTypewriterInputUI typewriterAnswerUi;
        private GhostArchiveUI archiveUi;
        private GhostBokjumeoniInventoryUI bokjumeoniInventoryUi;
        private GhostCounselingTimerDial timerDial;
        private Button selectedActionButton;
        // 답변을 제출한 Enter가 같은 프레임에 새로 나타난 "다음" 버튼까지 누르지 않게 한다.
        private int suppressEnterActionFrame = -1;
        // Captured from the scene-authored Canvas so designer placement survives runtime state changes.
        private Vector2 portraitHomePosition;
        // The visible face image has its own editable position inside Portrait Root.
        private Vector2 portraitImageHomePosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= CreateForGameScene;
            SceneManager.sceneLoaded += CreateForGameScene;
            CreateForGameScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void CreateForGameScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "GameScene" || FindAnyObjectByType<GhostGameController>() != null)
                return;

            var host = new GameObject("Ghost Counselor Game");
            host.AddComponent<GhostGameController>();
            SceneManager.sceneLoaded -= CreateForGameScene;
        }

        private void Awake()
        {
            ghosts = GhostContentLibrary.Create();
            classifier = new LocalIntentClassifier();
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "LegacyRuntime.ttf" }, 24);

            editableUi = FindAnyObjectByType<GhostCounselorUIReferences>();
            if (editableUi != null && editableUi.IsConfigured)
                BindEditableInterface(editableUi);
            else
            {
                Debug.LogError("GameScene의 Counselor UI 연결이 불완전합니다. 임시 UI는 생성하지 않습니다.");
                enabled = false;
                return;
            }
            ShowTitle();
        }

        private void Update()
        {
            if (save != null && phase != GamePhase.Ending)
                save.elapsedPlaySeconds += Time.unscaledDeltaTime;

            Keyboard keyboard = Keyboard.current;
            bool enterPressed = keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);

            // 열린 책/태블릿은 게임 진행보다 먼저 입력을 소비한다.
            if (archiveUi != null && archiveUi.IsShowingAnyWindow)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    archiveUi.TryCloseFromKeyboard();
                return;
            }

            // 복주머니 창이 열린 동안에는 상담 버튼·타자기 입력을 막는다.
            if (bokjumeoniInventoryUi != null && bokjumeoniInventoryUi.IsShowing)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    bokjumeoniInventoryUi.TryCloseFromKeyboard();
                return;
            }

            // 안내 UI가 떠 있을 때는 안내 버튼만 입력을 받는다.
            if (innerThoughtModal != null && innerThoughtModal.IsShowing)
            {
                if (enterPressed)
                    innerThoughtModal.TryConfirmFromKeyboard();
                return;
            }

            HandleChoiceNavigation(keyboard);

            // 현재 선택지가 정확히 하나일 때에만 Enter로 그 버튼을 누른다.
            if (enterPressed && Time.frameCount != suppressEnterActionFrame && TryInvokeSingleActionButton())
                return;

            if (phase != GamePhase.CriticalAnswer || answerInput == null)
                return;

            answerTime -= Time.unscaledDeltaTime;
            if (timerDial != null && timerDial.IsConfigured)
                timerDial.SetRemaining(answerTime);
            else
                timerText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, answerTime))}";

            if (!scary && answerTime <= ScaryThreshold)
            {
                scary = true;
                ShowScaryGhostPortrait();
                SetPortraitRootTransparent();
            }

            if (scary)
            {
                float shake = Mathf.Sin(Time.unscaledTime * 35f) * 4f;
                if (portraitImage != null && portraitImage.gameObject.activeSelf)
                    portraitImage.rectTransform.anchoredPosition = portraitImageHomePosition + new Vector2(shake, 0f);
            }

            if (answerTime <= 0f)
                ResolveAnswer("", AnswerIntent.Timeout);

            // The authored typewriter handles Enter itself. This is only a functional
            // fallback when its scene object has not been created yet.
            if ((typewriterAnswerUi == null || !typewriterAnswerUi.IsShowing) &&
                HasPressedEnter() && !string.IsNullOrWhiteSpace(answerInput.text))
                SubmitAnswer();
        }

        private bool TryInvokeSingleActionButton()
        {
            Button[] visibleButtons = ActiveActionButtons();
            if (visibleButtons.Length != 1)
                return false;

            visibleButtons[0].onClick.Invoke();
            return true;
        }

        private void HandleChoiceNavigation(Keyboard keyboard)
        {
            // 타자기 입력 중 W/S는 답변 글자이므로 선택지 이동에 쓰지 않는다.
            if (keyboard == null || (typewriterAnswerUi != null && typewriterAnswerUi.IsShowing) ||
                (phase == GamePhase.CriticalAnswer && answerInput != null))
                return;

            bool moveUp = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
            bool moveDown = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
            if (!moveUp && !moveDown)
                return;

            Button[] choices = ActiveActionButtons();
            if (choices.Length <= 1)
                return;

            int currentIndex = Array.IndexOf(choices, selectedActionButton);
            if (currentIndex < 0)
                currentIndex = 0;

            int nextIndex = moveUp
                ? Mathf.Max(0, currentIndex - 1)
                : Mathf.Min(choices.Length - 1, currentIndex + 1);
            SelectActionButton(choices[nextIndex]);
        }

        private Button[] ActiveActionButtons()
        {
            if (actionArea == null)
                return Array.Empty<Button>();

            return actionArea.GetComponentsInChildren<Button>(false)
                .Where(button => button != null && button.gameObject.activeInHierarchy && button.interactable)
                .ToArray();
        }

        private void SelectActionButton(Button button)
        {
            if (button == null)
                return;

            selectedActionButton = button;
            EventSystem.current?.SetSelectedGameObject(button.gameObject);
            button.Select();
        }

        private void CreateInterface()
        {
            EnsureEventSystem();
            canvas = new GameObject("Counselor Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.transform.SetParent(transform, false);

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            // The shrine art belongs to the scene behind this overlay. Keep the canvas root
            // transparent so the background remains visible around the counseling UI.
            root = Panel("Root", canvas.transform, new Color(0f, 0f, 0f, 0f));
            Stretch(root);

            RectTransform top = Panel("Top Bar", root, Hex("171218"));
            SetRect(top, 0f, 640f, 1280f, 80f);
            dayText = Label("Day", top, "DAY 1 / 7", 28, paper, TextAnchor.MiddleLeft);
            SetRect(dayText.rectTransform, 32f, 0f, 280f, 80f);
            phaseText = Label("Phase", top, "", 22, paperDark, TextAnchor.MiddleCenter);
            SetRect(phaseText.rectTransform, 390f, 0f, 500f, 80f);
            moneyText = Label("Money", top, "0원", 28, paper, TextAnchor.MiddleRight);
            SetRect(moneyText.rectTransform, 960f, 0f, 288f, 80f);

            content = Panel("Content", root, new Color(paper.r, paper.g, paper.b, 0.9f));
            SetRect(content, 40f, 150f, 1200f, 460f);

            portraitPanel = Panel("Portrait", content, spirit).GetComponent<Image>();
            SetRect(portraitPanel.rectTransform, 40f, 80f, 300f, 330f);
            portraitHomePosition = portraitPanel.rectTransform.anchoredPosition;
            nameText = Label("Name", content, "", 34, ink, TextAnchor.MiddleLeft);
            SetRect(nameText.rectTransform, 380f, 360f, 760f, 60f);
            titleText = Label("Title", content, "", 20, accent, TextAnchor.MiddleLeft);
            SetRect(titleText.rectTransform, 380f, 325f, 760f, 40f);
            dialogueText = Label("Dialogue", content, "", 25, ink, TextAnchor.UpperLeft);
            dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(dialogueText.rectTransform, 380f, 105f, 760f, 205f);

            timerText = Label("Timer", content, "", 54, accent, TextAnchor.MiddleCenter);
            SetRect(timerText.rectTransform, 1015f, 20f, 130f, 80f);

            actionArea = Panel("Actions", root, new Color(0f, 0f, 0f, 0f));
            SetRect(actionArea, 80f, 20f, 1120f, 80f);
        }

        private void BindEditableInterface(GhostCounselorUIReferences ui)
        {
            EnsureEventSystem();
            canvas = ui.canvas;
            root = ui.root;
            dayText = ui.dayText;
            phaseText = ui.phaseText;
            moneyText = ui.moneyText;
            content = ui.content;
            portraitPanel = ui.portraitPanel;
            portraitImage = ui.portraitImage;
            ghostPortraits = ui.ghostPortraits ?? Array.Empty<GhostPortraitSet>();
            nameText = ui.nameText;
            titleText = ui.titleText;
            dialogueText = ui.dialogueText;
            timerText = ui.timerText;
            actionArea = ui.actionArea;
            ledger = new GhostLedgerPresenter(ui.ledgerPanel, ui.ledgerText);
            innerThoughtModal = ui.GetComponent<GhostInnerThoughtModal>();
            typewriterAnswerUi = ui.GetComponent<GhostTypewriterInputUI>();
            archiveUi = ui.GetComponent<GhostArchiveUI>();
            bokjumeoniInventoryUi = ui.GetComponent<GhostBokjumeoniInventoryUI>();
            timerDial = ui.GetComponent<GhostCounselingTimerDial>();
            // 책과 태블릿은 타이틀 화면에서도 눌릴 수 있다. 새 게임/이어하기 전에는
            // 저장하지 않는 빈 데이터로 먼저 버튼을 연결하고, 게임 시작 시 실제 저장본으로 교체한다.
            archiveUi?.Bind(new SaveData(), ghosts, ui);
            bokjumeoniInventoryUi?.Bind(new SaveData());
            portraitHomePosition = portraitPanel.rectTransform.anchoredPosition;
            portraitImageHomePosition = portraitImage != null
                ? portraitImage.rectTransform.anchoredPosition
                : Vector2.zero;
            ui.ApplyRuntimeFont(font);
        }

        private void ShowTitle()
        {
            phase = GamePhase.DayStart;
            UpdateTopBar("문을 열기 전");
            nameText.text = "귀신 상담소";
            titleText.text = "7일 영업 프로토타입";
            HidePortrait();
            SetPortraitRootTransparent();
            titlePage = 0;
            ShowTitlePage();
        }

        private void ShowTitlePage()
        {
            dialogueText.text = TitlePages[titlePage];
            timerText.text = "";
            ClearActions();

            if (titlePage < TitlePages.Length - 1)
            {
                AddButton("다음 >", ShowNextTitlePage, spirit);
                return;
            }

            AddButton("새로 시작", NewGame, accent);
            if (GhostSaveSystem.HasSave)
                AddButton("이어하기", ContinueGame, spirit);
        }

        private void ShowNextTitlePage()
        {
            titlePage = Mathf.Min(titlePage + 1, TitlePages.Length - 1);
            ShowTitlePage();
        }

        private void NewGame()
        {
            GhostSaveSystem.Delete();
            save = new SaveData();
            archiveUi?.Bind(save, ghosts, editableUi);
            bokjumeoniInventoryUi?.Bind(save);
            GhostSaveSystem.Save(save);
            ShowDayStart();
        }

        private void ContinueGame()
        {
            save = GhostSaveSystem.Load();
            archiveUi?.Bind(save, ghosts, editableUi);
            bokjumeoniInventoryUi?.Bind(save);
            if (save.day > PrototypeDays)
                ShowEnding();
            else
                ShowDayStart();
        }

        private void ShowDayStart()
        {
            phase = GamePhase.DayStart;
            UpdateTopBar("아침 · 영업 준비");
            ResetPortrait();
            ClearActions();

            string message =
                $"영업 {save.day}일 차\n\n" +
                $"오늘은 귀신 {TodayCounselTarget()}명을\n상담해야 한다.";
            if (!ShowInnerThought(message, "영업 시작", ShowDayStartDetails))
                ShowDayStartDetails();
        }

        private void ShowDayStartDetails()
        {
            nameText.text = "귀신 상담소";
            titleText.text = "오늘의 영업 준비";
            dialogueText.text = $"문을 열면 오늘의 첫 손님이 들어옵니다.\n오늘 상담: 0 / {TodayCounselTarget()}명";
            ClearActions();
            ledger?.ShowDayStart(save.money, PrototypeDays - save.day + 1);
            AddButton("신당 문 열기", BeginVisit, accent);
        }

        private void BeginVisit()
        {
            EnsureDailyCounselState();
            currentGhost = PickGhost();
            currentProgress = GetProgress(currentGhost.id);
            currentStoryVisit = GetCurrentStoryVisit(currentGhost, currentProgress);
            currentVisitCompleted = false;
            currentVisitPurified = false;
            phase = GamePhase.Visit;
            UpdateTopBar("손님 방문");
            nameText.text = currentGhost.displayName;
            titleText.text = currentStoryVisit != null
                ? $"{currentStoryVisit.stage}회차 · {currentStoryVisit.stageTitle}"
                : currentGhost.title;
            ShowInitialGhostPortrait();
            SetPortraitRootTransparent();
            dialogueText.text = currentStoryVisit != null
                ? (currentProgress.visitCount > currentProgress.storyStage &&
                   !string.IsNullOrEmpty(currentStoryVisit.retryGreeting)
                    ? currentStoryVisit.retryGreeting
                    : currentStoryVisit.greeting)
                : (currentProgress.visitCount == 0 ? currentGhost.firstGreeting : currentGhost.followUpGreeting);
            timerText.text = "";
            ClearActions();
            AddButton("상담 시작", BeginCounseling, accent);
        }

        private void BeginCounseling()
        {
            phase = GamePhase.Counseling;
            askedQuestions = 0;
            usedQuestions.Clear();
            ShowQuestions();
        }

        private void ShowQuestions()
        {
            UpdateTopBar("상담 · 질문 선택");
            dialogueText.text = askedQuestions == 0
                ? currentStoryVisit?.questionGuide ?? "어떤 이야기부터 물어볼까?"
                : "조금 더 물어보면 고민의 진짜 모양이 보일 것 같다.";
            ClearActions();

            List<QuestionData> questions = CurrentQuestions();
            for (int index = 0; index < questions.Count; index++)
            {
                if (usedQuestions.Contains(index))
                    continue;

                int captured = index;
                AddButton(questions[index].prompt, () => AskQuestion(captured), spirit);
            }

            if (askedQuestions >= 2)
                AddButton("핵심 고민을 듣는다", BeginCriticalAnswer, accent);
        }

        private void AskQuestion(int index)
        {
            usedQuestions.Add(index);
            askedQuestions++;
            AdvanceGhostPortrait();
            QuestionData question = CurrentQuestions()[index];
            dialogueText.text = currentStoryVisit != null ? question.firstReply :
                (currentProgress.visitCount == 0 ? question.firstReply : question.followUpReply);
            ClearActions();
            AddButton("다음 질문", ShowQuestions, spirit);
            if (askedQuestions >= 2)
                AddButton("핵심 고민으로", BeginCriticalAnswer, accent);
        }

        private void BeginCriticalAnswer()
        {
            phase = GamePhase.CriticalAnswer;
            scary = false;
            answerTime = AnswerSeconds;
            UpdateTopBar("핵심 상담 · 10초");
            dialogueText.text = currentStoryVisit?.criticalQuestion ??
                (currentProgress.visitCount == 0 ? currentGhost.criticalQuestion : currentGhost.followUpCriticalQuestion);
            if (timerDial != null && timerDial.IsConfigured)
            {
                timerText.gameObject.SetActive(false);
                timerText.text = "";
                timerDial.Show(answerTime);
            }
            else
            {
                timerText.gameObject.SetActive(true);
                timerText.text = "10";
            }
            ClearActions();

            if (typewriterAnswerUi != null && typewriterAnswerUi.IsConfigured)
            {
                answerInput = typewriterAnswerUi.Show(
                    $"{currentGhost.displayName}에게 적절한 답변을 제시해주세요!",
                    SubmitAnswer);
                return;
            }

            // The GameScene stays playable before the designer creates the new UI from
            // the Ghost Counselor menu. No answer-choice route is kept here.
            Debug.LogWarning("[귀신 상담소] Typewriter Answer UI가 없습니다. 메뉴에서 새 타자기 입력 UI를 생성해 주세요.");
            answerInput = CreateInput(actionArea, "여기에 답변을 입력하세요...");
            SetRect(answerInput.GetComponent<RectTransform>(), 0f, 5f, 720f, 70f);
            answerInput.ActivateInputField();
        }

        private void SubmitAnswer()
        {
            if (phase != GamePhase.CriticalAnswer)
                return;

            string answer = answerInput != null ? answerInput.text : "";
            AnswerIntent intent = classifier.IsAvailable
                ? classifier.Classify(answer)
                : AnswerIntent.OffTopic;
            ResolveAnswer(answer, intent);
        }

        private void ResolveAnswer(string answer, AnswerIntent intent)
        {
            if (phase != GamePhase.CriticalAnswer)
                return;

            // Typewriter UI와 이 컨트롤러의 Update가 같은 프레임에 실행될 수 있다.
            // 이 Enter는 답변 제출에만 쓰고, 결과 화면의 "다음"은 다음 입력에서만 진행한다.
            suppressEnterActionFrame = Time.frameCount;
            phase = GamePhase.Result;
            typewriterAnswerUi?.Hide();
            ResetPortraitPositions();
            CounselResult result = Evaluate(intent);
            ApplyResult(result, answer);
            ShowResult(result);
        }

        private CounselResult Evaluate(AnswerIntent intent)
        {
            CounselOutcome outcome;
            if (intent is AnswerIntent.Timeout or AnswerIntent.OffTopic or AnswerIntent.Aggression)
                outcome = CounselOutcome.Unresolved;
            else if (intent == AnswerIntent.Avoidance)
                outcome = CounselOutcome.Partial;
            else if (intent == (currentStoryVisit?.preferredIntent ?? currentGhost.preferredIntent) && askedQuestions >= 2)
                outcome = CounselOutcome.SpecialSolved;
            else
                outcome = CounselOutcome.Solved;

            int basePay = outcome == CounselOutcome.Unresolved
                ? Mathf.RoundToInt(currentGhost.baseFee * 0.5f)
                : currentGhost.baseFee;
            int bonus = outcome switch
            {
                CounselOutcome.Partial => Mathf.RoundToInt(currentGhost.bonusFee * 0.25f),
                CounselOutcome.Solved => Mathf.RoundToInt(currentGhost.bonusFee * 0.65f),
                CounselOutcome.SpecialSolved => currentGhost.bonusFee,
                _ => 0
            };
            int itemPay = outcome == CounselOutcome.SpecialSolved ? currentGhost.rewardValue : 0;

            return new CounselResult
            {
                intent = intent,
                outcome = outcome,
                basePay = basePay,
                bonusPay = bonus,
                itemPay = itemPay,
                itemName = itemPay > 0 ? currentGhost.rewardItem : "",
                relationshipDelta = outcome switch
                {
                    CounselOutcome.Unresolved => -1,
                    CounselOutcome.Partial => 0,
                    CounselOutcome.Solved => 1,
                    CounselOutcome.SpecialSolved => 2,
                    _ => 0
                },
                reaction = CurrentReactions()[intent]
            };
        }

        private void ApplyResult(CounselResult result, string answer)
        {
            EnsureDailyCounselState();
            save.money += result.TotalPay;
            currentProgress.visitCount++;
            currentProgress.relationship = Mathf.Clamp(currentProgress.relationship + result.relationshipDelta, -2, 4);
            currentProgress.specialSolved |= result.outcome == CounselOutcome.SpecialSolved;
            currentVisitCompleted = result.outcome is CounselOutcome.Solved or CounselOutcome.SpecialSolved;
            currentVisitPurified = false;
            if (currentVisitCompleted && currentStoryVisit != null)
            {
                AddStoryFlag(currentStoryVisit.successFlag);
                if (currentProgress.storyStage >= CounselsPerGhost - 1)
                {
                    currentProgress.purified = true;
                    currentVisitPurified = true;
                    AddStoryFlag($"{currentGhost.id}_purified");
                    Unlock("성불 상담사", save.ghosts.Count(progress => progress.purified) >= ghosts.Count);
                }
                else
                {
                    currentProgress.storyStage++;
                }
            }
            save.lastGhostId = currentGhost.id;
            archiveUi?.RecordCounsel(save.day, currentGhost, result, answer);
            save.counselsCompletedToday++;
            if (!save.ghostsMetToday.Contains(currentGhost.id))
                save.ghostsMetToday.Add(currentGhost.id);

            if (!string.IsNullOrEmpty(result.itemName))
                save.items.Add(result.itemName);
            bokjumeoniInventoryUi?.Refresh();

            Unlock("첫 상담", save.ghosts.Sum(progress => progress.visitCount) >= 1);
            Unlock("오방색 명상담", result.outcome == CounselOutcome.SpecialSolved);
            Unlock("저승 소문 완료", save.ghosts.Count(progress => progress.visitCount > 0) == ghosts.Count);
            Unlock("십만 원의 무게", save.money >= 100000);
            GhostSaveSystem.Save(save);
        }

        private void ShowResult(CounselResult result)
        {
            UpdateTopBar("상담 결과");
            if (!UsesGhostPortrait())
                HidePortrait();
            SetPortraitRootTransparent();
            nameText.text = currentGhost.displayName;
            titleText.text = $"{OutcomeName(result.outcome)} · {IntentName(result.intent)}으로 받아들였습니다";
            dialogueText.text = $"“{result.reaction}”";
            timerText.text = "";
            timerDial?.Hide();
            ClearActions();
            AddButton(currentVisitCompleted ? "상담 후 이야기" : "다음", () =>
            {
                if (currentVisitCompleted)
                    ShowStoryAction(result);
                else
                    ShowRewardNotice(result);
            }, accent);
        }

        private void ShowStoryAction(CounselResult result)
        {
            string message = currentStoryVisit?.successAction;
            if (string.IsNullOrWhiteSpace(message))
            {
                ShowRewardNotice(result);
                return;
            }

            Action nextAction = currentVisitPurified
                ? () => ShowPurificationMoment(result)
                : () => ShowRewardNotice(result);
            string nextLabel = currentVisitPurified ? "마지막 인사" : "다음 손님";
            if (!ShowInnerThought(message, nextLabel, nextAction))
            {
                dialogueText.text = message;
                ClearActions();
                AddButton(nextLabel, nextAction, accent);
            }
        }

        private void ShowPurificationMoment(CounselResult result)
        {
            if (currentStoryVisit == null)
            {
                ShowRewardNotice(result);
                return;
            }

            string message = $"{currentGhost.displayName}\n\n“{currentStoryVisit.purificationLine}”\n\n{currentStoryVisit.cinematicSummary}";
            currentProgress.cinematicSeen = true;
            GhostSaveSystem.Save(save);
            if (!ShowInnerThought(message, "성불을 배웅한다", () => ShowRewardNotice(result)))
            {
                dialogueText.text = message;
                ClearActions();
                AddButton("성불을 배웅한다", () => ShowRewardNotice(result), accent);
            }
        }

        private void ShowRewardNotice(CounselResult result)
        {
            ClearActions();
            string payer = $"{currentGhost.displayName}{SubjectParticle(currentGhost.displayName)}";
            string message = !string.IsNullOrEmpty(result.itemName)
                ? $"{payer}\n{result.itemName}을 주고 갔습니다."
                : $"{payer}\n{result.TotalPay:N0}원을 복비로 주고 갔습니다.";

            bool hasMoreCounselsToday = save.counselsCompletedToday < TodayCounselTarget();
            string nextLabel = hasMoreCounselsToday ? "다음 손님" : "밤 정산";
            Action nextAction = hasMoreCounselsToday ? BeginVisit : ShowNight;
            if (!ShowInnerThought(message, nextLabel, nextAction))
            {
                nameText.text = currentGhost.displayName;
                titleText.text = "상담 보상";
                dialogueText.text = message;
                AddButton(nextLabel, nextAction, accent);
            }
        }

        private void ShowNight()
        {
            phase = GamePhase.Night;
            UpdateTopBar("밤 · 장부 정리");
            ResetPortrait();
            ClearActions();

            string message =
                $"{save.day}일 차 영업 종료\n\n" +
                "오늘의 상담을\n장부에 정리합니다.";
            if (!ShowInnerThought(message, "장부 확인", ShowNightDetails))
                ShowNightDetails();
        }

        private void ShowNightDetails()
        {
            nameText.text = "귀신 상담소";
            titleText.text = "오늘의 장부";
            dialogueText.text = "";
            ClearActions();
            ledger?.ShowNight(
                save.money,
                save.ghosts.Count(progress => progress.visitCount > 0),
                ghosts.Count,
                save.ghosts.Count(progress => progress.specialSolved),
                save.achievements.Count);

            if (save.day >= PrototypeDays)
                AddButton("7일 결산 보기", FinishCampaign, accent);
            else
                AddButton("다음 날", NextDay, accent);
        }

        private void NextDay()
        {
            EnsureDailyCounselState();
            save.day++;
            save.counselsCompletedToday = 0;
            save.ghostsMetToday.Clear();
            GhostSaveSystem.Save(save);
            ShowDayStart();
        }

        private void FinishCampaign()
        {
            save.day = PrototypeDays + 1;
            Unlock("칠일 신당", true);
            GhostSaveSystem.Save(save);
            ShowEnding();
        }

        private void ShowEnding()
        {
            phase = GamePhase.Ending;
            save ??= GhostSaveSystem.Load();
            GhostSaveSystem.Save(save);
            // 프롤로그와 같은 카메라·배경·타이핑 UI를 결말 모드로 재사용한다.
            PrologueController.RequestEnding();
        }

        private GhostDefinition PickGhost()
        {
            if (save.day == 1 && save.counselsCompletedToday == 0 && GetProgress("sticker").visitCount == 0)
                return GhostContentLibrary.Find(ghosts, "sticker");

            List<GhostDefinition> candidates = ghosts
                .Where(ghost =>
                    !save.ghostsMetToday.Contains(ghost.id) &&
                    ghost.id != save.lastGhostId &&
                    IsStoryAvailable(ghost, GetProgress(ghost.id)))
                .ToList();

            // 모든 귀신의 상담 횟수를 가장 낮은 쪽부터 채운다. 20회가 끝나면
            // 다섯 귀신 모두 정확히 네 번씩 상담한 상태가 된다.
            if (candidates.Count == 0)
            {
                candidates = ghosts
                    .Where(ghost =>
                        !save.ghostsMetToday.Contains(ghost.id) &&
                        IsStoryAvailable(ghost, GetProgress(ghost.id)))
                    .ToList();
            }

            int lowestVisitCount = candidates.Min(ghost => GetProgress(ghost.id).storyStage);
            List<GhostDefinition> pool = candidates
                .Where(ghost => GetProgress(ghost.id).storyStage == lowestVisitCount)
                .ToList();

            int index = UnityEngine.Random.Range(0, pool.Count);
            return pool[index];
        }

        private int TodayCounselTarget()
        {
            int index = Mathf.Clamp(save.day - 1, 0, DailyCounselTargets.Length - 1);
            return DailyCounselTargets[index];
        }

        private void EnsureDailyCounselState()
        {
            save.ghostsMetToday ??= new List<string>();
        }

        private GhostProgress GetProgress(string ghostId)
        {
            GhostProgress progress = save.ghosts.FirstOrDefault(item => item.ghostId == ghostId);
            if (progress != null)
                return progress;

            progress = new GhostProgress { ghostId = ghostId };
            save.ghosts.Add(progress);
            return progress;
        }

        private List<QuestionData> CurrentQuestions()
        {
            return currentStoryVisit?.questions ?? currentGhost.questions;
        }

        private Dictionary<AnswerIntent, string> CurrentReactions()
        {
            return currentStoryVisit?.reactions ?? currentGhost.reactions;
        }

        private static GhostStoryVisitData GetCurrentStoryVisit(GhostDefinition ghost, GhostProgress progress)
        {
            if (ghost?.storyVisits == null || ghost.storyVisits.Count == 0 || progress == null)
                return null;

            int index = Mathf.Clamp(progress.storyStage, 0, ghost.storyVisits.Count - 1);
            return ghost.storyVisits[index];
        }

        private bool IsStoryAvailable(GhostDefinition ghost, GhostProgress progress)
        {
            if (progress.purified || progress.storyStage >= CounselsPerGhost)
                return false;

            // 해주는 주인공의 성장 사건이므로 다른 손님의 진도가 쌓인 뒤에 방문한다.
            if (ghost.id != "bell")
                return true;

            int solvedStages = save.ghosts.Sum(item => item.storyStage);
            return progress.storyStage switch
            {
                0 => solvedStages >= 3,
                1 => save.ghosts.Any(item => item.purified),
                2 => solvedStages >= 10,
                3 => solvedStages >= 15,
                _ => false
            };
        }

        private void AddStoryFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
                return;

            save.storyFlags ??= new List<string>();
            if (!save.storyFlags.Contains(flag))
                save.storyFlags.Add(flag);
        }

        private void Unlock(string achievement, bool condition)
        {
            if (condition && !save.achievements.Contains(achievement))
                save.achievements.Add(achievement);
        }

        private void UpdateTopBar(string phaseName)
        {
            int day = save?.day ?? 1;
            int money = save?.money ?? 0;
            dayText.text = $"DAY {Mathf.Clamp(day, 1, PrototypeDays)} / {PrototypeDays}";
            moneyText.text = $"{money:N0}원";
            if (phaseText != null)
                phaseText.text = phaseName;
        }

        private bool ShowInnerThought(string message, string confirmLabel, Action confirmedAction)
        {
            if (innerThoughtModal == null)
                innerThoughtModal = FindAnyObjectByType<GhostInnerThoughtModal>();
            if (innerThoughtModal == null)
                return false;

            innerThoughtModal.Show(message, confirmLabel, confirmedAction);
            return true;
        }

        private static string SubjectParticle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "이";

            char lastCharacter = name[name.Length - 1];
            int hangulOffset = lastCharacter - 0xAC00;
            bool hasFinalConsonant = hangulOffset >= 0 && hangulOffset <= 11171 && hangulOffset % 28 != 0;
            return hasFinalConsonant ? "이" : "가";
        }

        private void ResetPortrait()
        {
            ResetPortraitPositions();
            SetPortraitRootTransparent();
            HidePortrait();
            timerText.text = "";
            timerDial?.Hide();
        }

        private void ResetPortraitPositions()
        {
            if (portraitPanel != null)
                portraitPanel.rectTransform.anchoredPosition = portraitHomePosition;

            if (portraitImage != null)
                portraitImage.rectTransform.anchoredPosition = portraitImageHomePosition;
        }

        private GhostPortraitSet CurrentPortraitSet()
        {
            if (currentGhost == null || portraitImage == null)
                return null;

            return ghostPortraits.FirstOrDefault(set =>
                set != null && set.ghostId == currentGhost.id && set.defaultPortrait != null);
        }

        private bool UsesGhostPortrait()
        {
            return CurrentPortraitSet() != null;
        }

        private void ShowInitialGhostPortrait()
        {
            GhostPortraitSet portraits = CurrentPortraitSet();
            if (portraits != null)
            {
                ShowGhostPortrait(portraits.defaultPortrait);
                return;
            }

            HidePortrait();
        }

        private void AdvanceGhostPortrait()
        {
            GhostPortraitSet portraits = CurrentPortraitSet();
            if (portraits == null || portraits.questionExpressionSequence == null ||
                portraits.questionExpressionSequence.Length == 0)
                return;

            // 거울각시는 방문 시 기본 표정이고, 선택지를 누를 때마다 다음 표정으로 진행한다.
            int index = Mathf.Min(askedQuestions, portraits.questionExpressionSequence.Length - 1);
            ShowGhostPortrait(portraits.questionExpressionSequence[index]);
        }

        private void ShowScaryGhostPortrait()
        {
            GhostPortraitSet portraits = CurrentPortraitSet();
            if (portraits == null)
                return;

            // 거울각시는 마지막 표정(scary)을 사용한다. 다른 귀신은 표정 목록이 비어 있어도
            // 기본 초상을 숨기지 않고 그대로 흔들어 공포 타이머를 표현한다.
            Sprite scaryPortrait = portraits.questionExpressionSequence?
                .LastOrDefault(sprite => sprite != null);
            if (scaryPortrait != null)
            {
                ShowGhostPortrait(scaryPortrait);
                return;
            }

            if (portraitImage == null || !portraitImage.gameObject.activeSelf || portraitImage.sprite == null)
                ShowGhostPortrait(portraits.defaultPortrait);
        }

        private void ShowGhostPortrait(Sprite sprite)
        {
            if (portraitImage == null)
                return;

            if (sprite == null)
            {
                HidePortrait();
                return;
            }

            portraitImage.gameObject.SetActive(true);
            portraitImage.sprite = sprite;
            portraitImage.color = Color.white;
            portraitImage.enabled = true;
        }

        private void HidePortrait()
        {
            if (portraitImage != null)
            {
                portraitImage.gameObject.SetActive(false);
                portraitImage.sprite = null;
            }
        }

        private void SetPortraitRootTransparent()
        {
            if (portraitPanel == null)
                return;

            Color rootColor = portraitPanel.color;
            rootColor.a = 0f;
            portraitPanel.color = rootColor;
        }

        private void ClearActions()
        {
            answerInput = null;
            typewriterAnswerUi?.Hide();
            ledger?.Hide();
            if (selectedActionButton != null && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == selectedActionButton.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
            selectedActionButton = null;
            for (int index = actionArea.childCount - 1; index >= 0; index--)
            {
                Transform child = actionArea.GetChild(index);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void AddButton(string text, Action action, Color color, float x = -1f, float width = 250f)
        {
            if (editableUi != null)
            {
                Button templateButton = Instantiate(editableUi.actionButtonTemplate, actionArea);
                templateButton.gameObject.name = text;
                templateButton.gameObject.SetActive(true);
                templateButton.onClick.RemoveAllListeners();
                templateButton.onClick.AddListener(() => action());
                DisableBuiltInNavigation(templateButton);

                // The scene template owns the visual style.  In the authored UI every
                // selectable answer is a thin cream card; `color` remains for the
                // programmatic fallback UI below.

                LayoutElement layout = templateButton.GetComponent<LayoutElement>();
                if (layout != null)
                    layout.preferredWidth = Mathf.Max(width, actionArea.rect.width);

                Text templateLabel = templateButton.GetComponentInChildren<Text>(true);
                templateLabel.text = text;
                PositionActionAreaBelowDialogue(actionArea.childCount);
                if (selectedActionButton == null)
                    SelectActionButton(templateButton);
                return;
            }

            int childIndex = actionArea.childCount;
            if (x < 0f)
                x = childIndex * 270f;

            Button button = new GameObject($"Button {childIndex}", typeof(RectTransform), typeof(Image), typeof(Button))
                .GetComponent<Button>();
            button.transform.SetParent(actionArea, false);
            button.GetComponent<Image>().color = color;
            SetRect(button.GetComponent<RectTransform>(), x, 5f, width, 70f);
            button.onClick.AddListener(() => action());
            DisableBuiltInNavigation(button);

            Text label = Label("Label", button.transform, text, 19, paper, TextAnchor.MiddleCenter);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 19;
            Stretch(label.rectTransform, 12f);
            if (selectedActionButton == null)
                SelectActionButton(button);
        }

        private static void DisableBuiltInNavigation(Button button)
        {
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        private void PositionActionAreaBelowDialogue(int buttonCount)
        {
            if (actionArea == null || dialogueText == null || actionArea.parent is not RectTransform parent)
                return;

            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            dialogueText.rectTransform.GetWorldCorners(corners);
            Vector3 bottomLeft = parent.InverseTransformPoint(corners[0]);
            Vector3 topRight = parent.InverseTransformPoint(corners[2]);

            // Convert the parent's centered local coordinate into its bottom-left
            // anchored coordinate. The action area's top then begins just beneath Dialogue.
            float x = bottomLeft.x + parent.rect.width * parent.pivot.x;
            float y = bottomLeft.y + parent.rect.height * parent.pivot.y - ActionGapBelowDialogue;
            float width = Mathf.Max(1f, topRight.x - bottomLeft.x);
            float height = Mathf.Max(70f, buttonCount * 70f + Mathf.Max(0, buttonCount - 1) * 8f);

            actionArea.anchorMin = Vector2.zero;
            actionArea.anchorMax = Vector2.zero;
            actionArea.pivot = new Vector2(0f, 1f);
            actionArea.anchoredPosition = new Vector2(x, y);
            actionArea.sizeDelta = new Vector2(width, height);

            VerticalLayoutGroup layout = actionArea.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
            }
        }

        private InputField CreateInput(Transform parent, string placeholder)
        {
            if (editableUi != null)
            {
                InputField templateInput = Instantiate(editableUi.answerInputTemplate, parent);
                templateInput.gameObject.name = "Answer Input";
                templateInput.gameObject.SetActive(true);
                templateInput.text = "";
                Text placeholderText = templateInput.placeholder as Text;
                if (placeholderText != null)
                    placeholderText.text = placeholder;
                return templateInput;
            }

            InputField input = new GameObject("Answer Input", typeof(RectTransform), typeof(Image), typeof(InputField))
                .GetComponent<InputField>();
            input.transform.SetParent(parent, false);
            input.GetComponent<Image>().color = Color.white;

            Text typed = Label("Text", input.transform, "", 22, ink, TextAnchor.MiddleLeft);
            Stretch(typed.rectTransform, 18f);
            Text hint = Label("Placeholder", input.transform, placeholder, 20, Hex("887A72"), TextAnchor.MiddleLeft);
            Stretch(hint.rectTransform, 18f);
            input.textComponent = typed;
            input.placeholder = hint;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 80;
            return input;
        }

        private static bool HasPressedEnter()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        private Text Label(string objectName, Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            Text label = new GameObject(objectName, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.text = text;
            label.supportRichText = true;
            return label;
        }

        private static RectTransform Panel(string objectName, Transform parent, Color color)
        {
            Image image = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image.rectTransform;
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString($"#{value}", out Color color) ? color : Color.white;
        }

        private static string OutcomeName(CounselOutcome outcome)
        {
            return outcome switch
            {
                CounselOutcome.Unresolved => "미해결",
                CounselOutcome.Partial => "부분 해결",
                CounselOutcome.Solved => "해결",
                CounselOutcome.SpecialSolved => "특별 해결",
                _ => outcome.ToString()
            };
        }

        private static string IntentName(AnswerIntent intent)
        {
            return intent switch
            {
                AnswerIntent.Empathy => "공감",
                AnswerIntent.PracticalAdvice => "현실적인 조언",
                AnswerIntent.Avoidance => "회피",
                AnswerIntent.Aggression => "공격",
                AnswerIntent.OffTopic => "문맥 이탈",
                AnswerIntent.Timeout => "시간 초과",
                _ => intent.ToString()
            };
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventObject = new GameObject("EventSystem", typeof(EventSystem));
            Type inputModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                eventObject.AddComponent(inputModuleType);
            else
                eventObject.AddComponent<StandaloneInputModule>();
        }
    }
}
