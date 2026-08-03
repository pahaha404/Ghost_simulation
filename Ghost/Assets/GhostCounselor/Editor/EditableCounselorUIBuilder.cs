using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    /// <summary>
    /// Creates the first editable UI hierarchy once. Afterwards artists can move and restyle
    /// its objects directly; this tool never replaces an existing hierarchy without consent.
    /// </summary>
    public static class EditableCounselorUIBuilder
    {
        private const int ReferenceLayoutVersion = 1;
        private const string ReferenceLayoutPreference = "GhostCounselor.ReferenceLayoutVersion";
        private static readonly Color Ink = Hex("2A2027");
        private static readonly Color Paper = Hex("F2E7CF");
        private static readonly Color PaperDark = Hex("D9C7A5");
        private static readonly Color Accent = Hex("A9403A");
        private static readonly Color Spirit = Hex("59786F");

        private static void ApplyReferenceLayoutOnce()
        {
            if (EditorPrefs.GetInt(ReferenceLayoutPreference, 0) >= ReferenceLayoutVersion ||
                !string.Equals(EditorSceneManager.GetActiveScene().name, "SampleScene", StringComparison.Ordinal) ||
                FindExisting() == null)
                return;

            ApplyReferenceLayout();
            EditorPrefs.SetInt(ReferenceLayoutPreference, ReferenceLayoutVersion);
        }

        private static void BuildWhenEditorIsReady()
        {
            if (FindExisting() != null)
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += BuildWhenEditorIsReady;
                return;
            }

            if (!string.Equals(EditorSceneManager.GetActiveScene().name, "SampleScene", StringComparison.Ordinal))
                return;

            BuildEditableUI();
        }

        [MenuItem("Ghost Counselor/Create Editable UI Layout")]
        public static void BuildEditableUI()
        {
            GhostCounselorUIReferences existing = FindExisting();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[귀신 상담소] 편집 가능한 UI가 이미 있습니다. Hierarchy에서 ‘Counselor UI’를 수정하세요.");
                return;
            }

            EnsureEventSystem();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject("Counselor UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            GhostCounselorUIReferences ui = canvasObject.AddComponent<GhostCounselorUIReferences>();
            ui.canvas = canvas;
            ui.root = ImagePanel("Root", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
            Stretch(ui.root);

            RectTransform top = ImagePanel("Top Bar", ui.root, new Color(0f, 0f, 0f, 0f));
            SetRect(top, 0f, 620f, 1280f, 100f);
            ui.phaseText = TextLabel("Phase", top, "문을 열기 전", 20, PaperDark, TextAnchor.MiddleCenter, font);
            SetRect(ui.phaseText.rectTransform, 440f, 8f, 330f, 42f);
            ui.moneyText = TextLabel("Money", top, "0원", 30, Ink, TextAnchor.MiddleCenter, font);
            SetRect(ui.moneyText.rectTransform, 900f, 22f, 190f, 58f);
            ui.dayText = TextLabel("Day", top, "DAY 1 / 7", 25, Paper, TextAnchor.MiddleCenter, font);
            SetRect(ui.dayText.rectTransform, 1100f, 12f, 160f, 72f);

            // Reference composition: dialogue on the left, character in the centre, HUD on the right.
            ui.content = ImagePanel("Content Panel", ui.root, new Color(Paper.r, Paper.g, Paper.b, 0.94f));
            SetRect(ui.content, 20f, 265f, 360f, 300f);
            ui.nameText = TextLabel("Name", ui.content, "귀신 상담소", 31, Ink, TextAnchor.MiddleCenter, font);
            SetRect(ui.nameText.rectTransform, 35f, 278f, 290f, 58f);
            ui.titleText = TextLabel("Title", ui.content, "7일 영업 프로토타입", 18, Accent, TextAnchor.MiddleCenter, font);
            SetRect(ui.titleText.rectTransform, 25f, 224f, 310f, 38f);
            ui.dialogueText = TextLabel("Dialogue", ui.content, "", 23, Ink, TextAnchor.UpperLeft, font);
            ui.dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(ui.dialogueText.rectTransform, 26f, 30f, 308f, 185f);

            ui.portraitPanel = ImagePanel("Portrait Root", ui.root, new Color(Spirit.r, Spirit.g, Spirit.b, 0f)).GetComponent<Image>();
            SetRect(ui.portraitPanel.rectTransform, 420f, 70f, 440f, 500f);
            ui.portraitPanel.raycastTarget = false;

            ui.ledgerPanel = ImagePanel("Ledger Notebook - Edit Position Here", ui.root, new Color(Paper.r, Paper.g, Paper.b, 0.97f)).GetComponent<Image>();
            SetRect(ui.ledgerPanel.rectTransform, 920f, 230f, 300f, 330f);
            ui.ledgerPanel.raycastTarget = false;
            RectTransform ledgerHeader = ImagePanel("Ledger Header", ui.ledgerPanel.transform, Accent);
            SetRect(ledgerHeader, 0f, 280f, 300f, 50f);
            Text ledgerTitle = TextLabel("Ledger Title", ledgerHeader, "오늘의 장부", 25, Paper, TextAnchor.MiddleCenter, font);
            Stretch(ledgerTitle.rectTransform, 8f);
            ui.ledgerText = TextLabel("Ledger Entries - Edit Text Style Here", ui.ledgerPanel.transform,
                "기본 사례비   0원\n상담 보너스   0원\n물건 환전     -\n────────────\n오늘 수입     0원",
                20, Ink, TextAnchor.UpperLeft, font);
            ui.ledgerText.verticalOverflow = VerticalWrapMode.Overflow;
            ui.ledgerText.lineSpacing = 1.25f;
            SetRect(ui.ledgerText.rectTransform, 30f, 38f, 245f, 225f);
            RectTransform ledgerMargin = ImagePanel("Ledger Red Margin", ui.ledgerPanel.transform, new Color(Accent.r, Accent.g, Accent.b, 0.55f));
            SetRect(ledgerMargin, 15f, 28f, 3f, 238f);

            ui.timerText = TextLabel("Timer", ui.root, "", 48, Accent, TextAnchor.MiddleCenter, font);
            SetRect(ui.timerText.rectTransform, 700f, 92f, 130f, 70f);

            ui.actionArea = EmptyRect("Actions - Runtime Buttons", ui.root);
            SetRect(ui.actionArea, 20f, 22f, 360f, 205f);
            VerticalLayoutGroup actionsLayout = ui.actionArea.gameObject.AddComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 10f;
            actionsLayout.childAlignment = TextAnchor.LowerLeft;
            actionsLayout.childControlWidth = false;
            actionsLayout.childControlHeight = false;
            actionsLayout.childForceExpandWidth = false;
            actionsLayout.childForceExpandHeight = false;

            RectTransform templates = EmptyRect("UI Templates - Edit Style Here", ui.root);
            SetRect(templates, -2000f, -2000f, 1f, 1f);
            ui.actionButtonTemplate = CreateButtonTemplate(templates, font);
            ui.answerInputTemplate = CreateInputTemplate(templates, font);

            canvasObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasObject;
            Debug.Log("[귀신 상담소] 편집 가능한 Canvas UI를 만들었습니다. Hierarchy에서 ‘Counselor UI’를 펼쳐 직접 수정하세요.");
        }

        [MenuItem("Ghost Counselor/Apply Reference UI Layout")]
        public static void ApplyReferenceLayout()
        {
            GhostCounselorUIReferences ui = FindExisting();
            if (ui == null || !ui.IsConfigured)
            {
                Debug.LogWarning("[귀신 상담소] 적용할 Counselor UI를 찾지 못했습니다.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Apply Reference UI Layout");
            Transform root = ui.root;
            RectTransform top = ui.dayText.transform.parent as RectTransform;
            Image topImage = top != null ? top.GetComponent<Image>() : null;
            if (topImage != null)
                topImage.color = new Color(0f, 0f, 0f, 0f);
            SetRect(top, 0f, 620f, 1280f, 100f);
            SetRect(ui.phaseText.rectTransform, 440f, 8f, 330f, 42f);
            SetRect(ui.moneyText.rectTransform, 900f, 22f, 190f, 58f);
            SetRect(ui.dayText.rectTransform, 1100f, 12f, 160f, 72f);

            SetRect(ui.content, 20f, 265f, 360f, 300f);
            Image contentImage = ui.content.GetComponent<Image>();
            if (contentImage != null)
                contentImage.color = new Color(Paper.r, Paper.g, Paper.b, 0.94f);
            SetRect(ui.nameText.rectTransform, 35f, 278f, 290f, 58f);
            SetRect(ui.titleText.rectTransform, 25f, 224f, 310f, 38f);
            SetRect(ui.dialogueText.rectTransform, 26f, 30f, 308f, 185f);

            ui.portraitPanel.transform.SetParent(root, false);
            ui.portraitPanel.transform.SetAsLastSibling();
            SetRect(ui.portraitPanel.rectTransform, 420f, 70f, 440f, 500f);
            ui.portraitPanel.raycastTarget = false;
            if (ui.ledgerPanel != null)
            {
                SetRect(ui.ledgerPanel.rectTransform, 920f, 230f, 300f, 330f);
                ui.ledgerPanel.raycastTarget = false;
            }
            if (ui.ledgerText != null)
                SetRect(ui.ledgerText.rectTransform, 30f, 38f, 245f, 225f);
            ui.timerText.transform.SetParent(root, false);
            ui.timerText.transform.SetAsLastSibling();
            SetRect(ui.timerText.rectTransform, 700f, 92f, 130f, 70f);
            SetRect(ui.actionArea, 20f, 22f, 360f, 205f);
            ReplaceWithVerticalLayout(ui.actionArea);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = ui.gameObject;
            Debug.Log("[귀신 상담소] 참고 이미지 구도로 UI를 재배치했습니다.");
        }

        private static GhostCounselorUIReferences FindExisting()
        {
            return UnityEngine.Object.FindAnyObjectByType<GhostCounselorUIReferences>();
        }

        private static Button CreateButtonTemplate(Transform parent, Font font)
        {
            RectTransform rect = ImagePanel("Action Button Template", parent, Accent);
            SetRect(rect, 0f, 0f, 250f, 70f);
            Button button = rect.gameObject.AddComponent<Button>();
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 250f;
            layout.preferredHeight = 70f;
            Text label = TextLabel("Label", rect, "버튼", 19, Paper, TextAnchor.MiddleCenter, font);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 19;
            Stretch(label.rectTransform, 12f);
            rect.gameObject.SetActive(false);
            return button;
        }

        private static InputField CreateInputTemplate(Transform parent, Font font)
        {
            RectTransform rect = ImagePanel("Answer Input Template", parent, Color.white);
            SetRect(rect, 0f, 0f, 620f, 70f);
            InputField input = rect.gameObject.AddComponent<InputField>();
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 620f;
            layout.preferredHeight = 70f;
            layout.flexibleWidth = 1f;
            Text typed = TextLabel("Text", rect, "", 22, Ink, TextAnchor.MiddleLeft, font);
            Stretch(typed.rectTransform, 18f);
            Text placeholder = TextLabel("Placeholder", rect, "여기에 답변을 입력하세요...", 20, Hex("887A72"), TextAnchor.MiddleLeft, font);
            Stretch(placeholder.rectTransform, 18f);
            input.textComponent = typed;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 80;
            rect.gameObject.SetActive(false);
            return input;
        }

        private static RectTransform ImagePanel(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image.rectTransform;
        }

        private static RectTransform EmptyRect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Text TextLabel(string name, Transform parent, string text, int size, Color color, TextAnchor alignment, Font font)
        {
            Text label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.text = text;
            label.supportRichText = true;
            return label;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            Type inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule != null)
                eventSystem.AddComponent(inputModule);
            else
                eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void ReplaceWithVerticalLayout(RectTransform actionArea)
        {
            HorizontalLayoutGroup horizontal = actionArea.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
                Undo.DestroyObjectImmediate(horizontal);

            VerticalLayoutGroup layout = actionArea.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = Undo.AddComponent<VerticalLayoutGroup>(actionArea.gameObject);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
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
    }
}
