using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor
{
    public sealed class GhostGameController : MonoBehaviour
    {
        private const int PrototypeDays = 7;
        private const float AnswerSeconds = 10f;
        private const float ScaryThreshold = 5f;

        private static readonly string[] TitlePages =
        {
            "폐업까지 남은 시간은 단 7일.\n불행인지 다행인지 마지못해 상담해 준 귀신이 홍보 역할을 톡톡히 했나 보다.",
            "자기 얘기 좀 들어 달라고 몰려드는 귀신들. 상담을 잘하면 돈도 벌고, 귀신과 친해지면 특별한 보상도 얻는다.",
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
        private Image ledgerPanel;
        private Text ledgerText;
        private Image portraitPanel;
        private InputField answerInput;
        private GhostCounselorUIReferences editableUi;
        // Captured from the scene-authored Canvas so designer placement survives runtime state changes.
        private Vector2 portraitHomePosition;

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
            if (phase != GamePhase.CriticalAnswer || answerInput == null)
                return;

            answerTime -= Time.unscaledDeltaTime;
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
                portraitPanel.rectTransform.anchoredPosition = portraitHomePosition + new Vector2(shake, 0f);
            }

            if (answerTime <= 0f)
                ResolveAnswer("", AnswerIntent.Timeout);
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
            ledgerPanel = ui.ledgerPanel;
            ledgerText = ui.ledgerText;
            portraitHomePosition = portraitPanel.rectTransform.anchoredPosition;
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
            GhostSaveSystem.Save(save);
            ShowDayStart();
        }

        private void ContinueGame()
        {
            save = GhostSaveSystem.Load();
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
            nameText.text = $"영업 {save.day}일 차";
            titleText.text = "오늘도 신당 앞에 서늘한 기척이 느껴진다";
            dialogueText.text =
                $"현재 보유금: {save.money:N0}원\n" +
                $"남은 영업일: {PrototypeDays - save.day + 1}일\n\n" +
                "문을 열면 오늘의 손님 한 명이 들어옵니다.";
            ClearActions();
            AddButton("신당 문 열기", BeginVisit, accent);
            AddButton("저장 후 제목으로", SaveAndTitle, spirit);
        }

        private void BeginVisit()
        {
            currentGhost = PickGhost();
            currentProgress = GetProgress(currentGhost.id);
            phase = GamePhase.Visit;
            UpdateTopBar("손님 방문");
            nameText.text = currentGhost.displayName;
            titleText.text = currentGhost.title;
            ShowInitialGhostPortrait();
            SetPortraitRootTransparent();
            dialogueText.text = currentProgress.visitCount == 0
                ? currentGhost.firstGreeting
                : currentGhost.followUpGreeting;
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
                ? "어떤 이야기부터 물어볼까?"
                : "조금 더 물어보면 고민의 진짜 모양이 보일 것 같다.";
            ClearActions();

            for (int index = 0; index < currentGhost.questions.Count; index++)
            {
                if (usedQuestions.Contains(index))
                    continue;

                int captured = index;
                AddButton(currentGhost.questions[index].prompt, () => AskQuestion(captured), spirit);
            }

            if (askedQuestions >= 2)
                AddButton("핵심 고민을 듣는다", BeginCriticalAnswer, accent);
        }

        private void AskQuestion(int index)
        {
            usedQuestions.Add(index);
            askedQuestions++;
            AdvanceGhostPortrait();
            QuestionData question = currentGhost.questions[index];
            dialogueText.text = currentProgress.visitCount == 0 ? question.firstReply : question.followUpReply;
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
            dialogueText.text = currentProgress.visitCount == 0
                ? currentGhost.criticalQuestion
                : currentGhost.followUpCriticalQuestion;
            timerText.text = "10";
            ClearActions();

            answerInput = CreateInput(actionArea, "여기에 답변을 입력하세요...");
            SetRect(answerInput.GetComponent<RectTransform>(), 0f, 5f, 720f, 70f);
            AddButton("답변 전송", SubmitAnswer, accent, 740f, 260f);
            AddButton("선택지로 답하기", ShowFallbackChoices, spirit, 1010f, 110f);
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

        private void ShowFallbackChoices()
        {
            if (phase != GamePhase.CriticalAnswer)
                return;

            ClearActions();
            AddIntentButton("마음을 이해하고 공감한다", AnswerIntent.Empathy);
            AddIntentButton("지금 할 수 있는 방법을 제안한다", AnswerIntent.PracticalAdvice);
            AddIntentButton("대답을 피한다", AnswerIntent.Avoidance);
            AddIntentButton("화를 내며 몰아붙인다", AnswerIntent.Aggression);
        }

        private void AddIntentButton(string text, AnswerIntent intent)
        {
            AddButton(text, () => ResolveAnswer(text, intent), intent == AnswerIntent.Aggression ? accent : spirit);
        }

        private void ResolveAnswer(string answer, AnswerIntent intent)
        {
            if (phase != GamePhase.CriticalAnswer)
                return;

            phase = GamePhase.Result;
            portraitPanel.rectTransform.anchoredPosition = portraitHomePosition;
            CounselResult result = Evaluate(intent);
            ApplyResult(result);
            ShowResult(result);
        }

        private CounselResult Evaluate(AnswerIntent intent)
        {
            CounselOutcome outcome;
            if (intent is AnswerIntent.Timeout or AnswerIntent.OffTopic or AnswerIntent.Aggression)
                outcome = CounselOutcome.Unresolved;
            else if (intent == AnswerIntent.Avoidance)
                outcome = CounselOutcome.Partial;
            else if (intent == currentGhost.preferredIntent && askedQuestions >= 2)
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
                reaction = currentGhost.reactions[intent]
            };
        }

        private void ApplyResult(CounselResult result)
        {
            save.money += result.TotalPay;
            currentProgress.visitCount++;
            currentProgress.relationship = Mathf.Clamp(currentProgress.relationship + result.relationshipDelta, -2, 4);
            currentProgress.specialSolved |= result.outcome == CounselOutcome.SpecialSolved;
            save.lastGhostId = currentGhost.id;

            if (!string.IsNullOrEmpty(result.itemName))
                save.items.Add(result.itemName);

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
            nameText.text = OutcomeName(result.outcome);
            titleText.text = $"{IntentName(result.intent)}으로 받아들였습니다";
            dialogueText.text = $"{currentGhost.displayName}: “{result.reaction}”\n\n오늘의 상담 결과가 장부에 기록됐다.";
            timerText.text = "";
            ClearActions();
            ShowLedger(result);
            AddButton("밤 정산", ShowNight, accent);
        }

        private void ShowNight()
        {
            phase = GamePhase.Night;
            UpdateTopBar("밤 · 장부 정리");
            ResetPortrait();
            nameText.text = $"{save.day}일 차 영업 종료";
            titleText.text = "신당 장부에 오늘의 상담을 기록했다";
            dialogueText.text =
                $"누적 보유금: {save.money:N0}원\n" +
                $"만난 귀신: {save.ghosts.Count(progress => progress.visitCount > 0)}/{ghosts.Count}\n" +
                $"특별 해결: {save.ghosts.Count(progress => progress.specialSolved)}건\n" +
                $"업적: {save.achievements.Count}개";
            ClearActions();

            if (save.day >= PrototypeDays)
                AddButton("7일 결산 보기", FinishCampaign, accent);
            else
                AddButton("다음 날", NextDay, accent);
        }

        private void NextDay()
        {
            save.day++;
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
            UpdateTopBar("7일 최종 결산");
            ResetPortrait();
            string rank = save.money >= 180000 ? "S" :
                save.money >= 140000 ? "A" :
                save.money >= 100000 ? "B" :
                save.money >= 70000 ? "C" : "D";
            string ending = rank switch
            {
                "S" => "저승까지 예약이 밀렸다. 폐업 걱정은 사라졌지만 야근이 시작됐다.",
                "A" => "월세와 급한 빚을 갚았다. 신당은 정식 귀신 상담소가 되었다.",
                "B" => "폐업은 막았다. 딱지 할아버지가 홍보를 더 해보겠다고 나섰다.",
                "C" => "집주인에게 일주일만 더 시간을 얻었다. 상담은 아직 끝나지 않았다.",
                _ => "돈은 모자랐지만 귀신들이 신당을 지켜주기로 했다. 월세에는 별 도움이 안 된다."
            };
            nameText.text = $"최종 등급 {rank}";
            titleText.text = $"{save.money:N0}원 · 업적 {save.achievements.Count}개";
            dialogueText.text = ending +
                $"\n\n만난 귀신 {save.ghosts.Count(progress => progress.visitCount > 0)}/5" +
                $" · 특별 해결 {save.ghosts.Count(progress => progress.specialSolved)}건" +
                $" · 환전 물건 {save.items.Count}개";
            ClearActions();
            AddButton("처음부터 다시", NewGame, accent);
            AddButton("제목으로", ShowTitle, spirit);
        }

        private GhostDefinition PickGhost()
        {
            if (save.day == 1 && GetProgress("sticker").visitCount == 0)
                return GhostContentLibrary.Find(ghosts, "sticker");

            List<GhostDefinition> unvisited = ghosts
                .Where(ghost => GetProgress(ghost.id).visitCount == 0)
                .ToList();
            List<GhostDefinition> pool = unvisited.Count > 0
                ? unvisited
                : ghosts.Where(ghost => ghost.id != save.lastGhostId).ToList();

            int index = UnityEngine.Random.Range(0, pool.Count);
            return pool[index];
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

        private void Unlock(string achievement, bool condition)
        {
            if (condition && !save.achievements.Contains(achievement))
                save.achievements.Add(achievement);
        }

        private void SaveAndTitle()
        {
            GhostSaveSystem.Save(save);
            ShowTitle();
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

        private void ResetPortrait()
        {
            portraitPanel.rectTransform.anchoredPosition = portraitHomePosition;
            SetPortraitRootTransparent();
            HidePortrait();
            timerText.text = "";
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
            if (portraits != null && portraits.questionExpressionSequence != null &&
                portraits.questionExpressionSequence.Length > 0)
            {
                ShowGhostPortrait(portraits.questionExpressionSequence[portraits.questionExpressionSequence.Length - 1]);
                return;
            }

            HidePortrait();
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
            HideLedger();
            for (int index = actionArea.childCount - 1; index >= 0; index--)
            {
                Transform child = actionArea.GetChild(index);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void ShowLedger(CounselResult result)
        {
            if (ledgerPanel == null || ledgerText == null)
                return;

            ledgerPanel.gameObject.SetActive(true);
            ledgerText.text =
                $"기본 사례비   {result.basePay:N0}원\n" +
                $"상담 보너스   {result.bonusPay:N0}원\n" +
                (result.itemPay > 0
                    ? $"물건 환전     {result.itemPay:N0}원\n"
                    : "물건 환전     -\n") +
                "────────────\n" +
                $"오늘 수입     {result.TotalPay:N0}원";
        }

        private void HideLedger()
        {
            if (ledgerPanel != null)
                ledgerPanel.gameObject.SetActive(false);
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

                // The scene template owns the visual style.  In the authored UI every
                // selectable answer is a thin cream card; `color` remains for the
                // programmatic fallback UI below.

                LayoutElement layout = templateButton.GetComponent<LayoutElement>();
                if (layout != null)
                    layout.preferredWidth = Mathf.Max(width, actionArea.rect.width);

                Text templateLabel = templateButton.GetComponentInChildren<Text>(true);
                templateLabel.text = text;
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

            Text label = Label("Label", button.transform, text, 19, paper, TextAnchor.MiddleCenter);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 19;
            Stretch(label.rectTransform, 12f);
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
