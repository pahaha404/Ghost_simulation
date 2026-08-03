using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    /// <summary>
    /// Explicitly creates the desk book/tablet hit areas and the authored archive overlays.
    /// It never runs on import or compilation, so manual GameScene layout stays user-owned.
    /// </summary>
    public static class ArchiveInteractionUIBuilder
    {
        private static readonly Color Ink = Hex("2D2020");
        private static readonly Color Paper = Hex("F2E3C5");
        private static readonly Color PaperDeep = Hex("D1B587");
        private static readonly Color Wood = Hex("3A211D");
        private static readonly Color Tablet = Hex("17161C");
        private static readonly Color Screen = Hex("25313B");
        private static readonly Color Accent = Hex("B8564F");

        [MenuItem("Ghost Counselor/Archive UI/Create Or Replace Desk Book And Tablet")]
        public static void CreateOrReplace()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool openedTemporarily = false;
            GhostCounselorUIReferences ui = FindUi(scene);
            if (ui == null)
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Additive);
                openedTemporarily = true;
                ui = FindUi(scene);
            }

            if (ui == null || ui.root == null)
            {
                Debug.LogWarning("[Ghost Counselor] GameScene의 Counselor UI를 찾지 못했습니다.");
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ui.root.gameObject, "Create Desk Book And Tablet Archive UI");
            // The old right-side notebook is replaced by the desk-book interaction.  Keep the
            // object for an easy manual rollback, but never leave it visible in Edit or Play Mode.
            if (ui.ledgerPanel != null)
                ui.ledgerPanel.gameObject.SetActive(false);
            Transform old = ui.root.Find("Archive Interactions - Edit Here");
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GhostArchiveUI archive = ui.GetComponent<GhostArchiveUI>();
            if (archive == null)
                archive = Undo.AddComponent<GhostArchiveUI>(ui.gameObject);

            RectTransform holder = CreatePanel("Archive Interactions - Edit Here", ui.root, Color.clear);
            Stretch(holder, 0f);
            // This holder fills the screen only to keep the desk hotspots together.
            // It must never intercept clicks intended for the existing counselling buttons.
            holder.GetComponent<Image>().raycastTarget = false;
            archive.ledgerHotspot = CreateHotspot("Desk Book Click Area - Edit Here", holder, new Vector2(555f, 12f), new Vector2(165f, 106f));
            archive.tabletHotspot = CreateHotspot("Desk Tablet Click Area - Edit Here", holder, new Vector2(875f, 10f), new Vector2(142f, 100f));

            BuildLedger(holder, archive, font);
            BuildTablet(holder, archive, font);
            archive.ledgerWindow.SetActive(false);
            archive.tabletWindow.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
            else
                Selection.activeGameObject = holder.gameObject;
            Debug.Log("[Ghost Counselor] 책 장부와 태블릿 도감 클릭 UI를 생성했습니다.");
        }

        private static void BuildLedger(Transform parent, GhostArchiveUI archive, Font font)
        {
            RectTransform window = CreatePanel("Ledger Book Window", parent, new Color(0f, 0f, 0f, 0.58f));
            Stretch(window, 0f);
            archive.ledgerWindow = window.gameObject;

            RectTransform book = CreatePanel("Open Ledger Book", window, Wood);
            SetRect(book, 175f, 75f, 930f, 530f);
            AddOutline(book.gameObject, Hex("160C0A"), new Vector2(5f, -5f));
            RectTransform left = CreatePanel("Ledger Left Page", book, Paper);
            SetRect(left, 24f, 28f, 426f, 474f);
            RectTransform right = CreatePanel("Ledger Right Page", book, Paper);
            SetRect(right, 480f, 28f, 426f, 474f);
            RectTransform spine = CreatePanel("Ledger Spine", book, PaperDeep);
            SetRect(spine, 452f, 26f, 10f, 478f);

            Text heading = CreateText("Ledger Heading", left, "신당 장부", font, 38, Ink, TextAnchor.MiddleCenter);
            SetRect(heading.rectTransform, 50f, 340f, 326f, 70f);
            Text guide = CreateText("Ledger Guide", left,
                "하루가 끝날 때마다\n상담 한 줄이 이 장부에 남습니다.\n\n상태\n😞  😐  😊  ✨", font, 21, Ink, TextAnchor.UpperLeft);
            SetRect(guide.rectTransform, 65f, 120f, 300f, 200f);

            Text title = CreateText("Ledger Record Title", right, "상담 기록", font, 30, Accent, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, 40f, 400f, 346f, 40f);
            ScrollRect scroll = CreateScroll("Ledger Record Scroll", right, new Vector2(34f, 48f), new Vector2(358f, 334f), out RectTransform content);
            Text list = CreateText("Ledger Record List", content, "", font, 22, Ink, TextAnchor.UpperLeft);
            list.horizontalOverflow = HorizontalWrapMode.Wrap;
            list.verticalOverflow = VerticalWrapMode.Overflow;
            list.raycastTarget = false;
            SetTopStretch(list.rectTransform, 8f);
            ContentSizeFitter listFitter = list.gameObject.AddComponent<ContentSizeFitter>();
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ContentSizeFitter contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            archive.ledgerListText = list;

            archive.ledgerCloseButton = CreateButton("Ledger Close", window, "닫기", font, Accent, Paper);
            SetRect(archive.ledgerCloseButton.GetComponent<RectTransform>(), 1015f, 615f, 90f, 44f);
        }

        private static void BuildTablet(Transform parent, GhostArchiveUI archive, Font font)
        {
            RectTransform window = CreatePanel("Tablet Codex Window", parent, new Color(0f, 0f, 0f, 0.63f));
            Stretch(window, 0f);
            archive.tabletWindow = window.gameObject;

            RectTransform tablet = CreatePanel("Tablet Body", window, Tablet);
            SetRect(tablet, 180f, 55f, 920f, 590f);
            AddOutline(tablet.gameObject, Hex("070609"), new Vector2(7f, -7f));
            RectTransform screen = CreatePanel("Tablet Screen", tablet, Screen);
            SetRect(screen, 26f, 42f, 868f, 508f);
            AddOutline(screen.gameObject, Hex("596B78"), new Vector2(2f, -2f));

            Text heading = CreateText("Tablet Heading", screen, "귀신 도감", font, 34, Paper, TextAnchor.MiddleCenter);
            SetRect(heading.rectTransform, 30f, 440f, 808f, 45f);
            archive.tabletCloseButton = CreateButton("Tablet Close", tablet, "닫기", font, Accent, Paper);
            SetRect(archive.tabletCloseButton.GetComponent<RectTransform>(), 780f, 555f, 92f, 34f);

            ScrollRect scroll = CreateScroll("Codex Scroll", screen, new Vector2(37f, 44f), new Vector2(794f, 370f), out RectTransform grid);
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(235f, 158f);
            layout.spacing = new Vector2(28f, 22f);
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            ContentSizeFitter fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            archive.codexGrid = grid;

            archive.ghostCardTemplate = CreateCardTemplate(grid, font);
            archive.ghostCardTemplate.gameObject.SetActive(false);
            Text empty = CreateText("Empty Codex", screen, "아직 만난 귀신이 없습니다.", font, 27, Paper, TextAnchor.MiddleCenter);
            SetRect(empty.rectTransform, 80f, 205f, 708f, 70f);
            archive.emptyCodexText = empty;

            RectTransform detail = CreatePanel("Ghost Detail", screen, new Color(0.08f, 0.12f, 0.16f, 0.98f));
            SetRect(detail, 28f, 24f, 812f, 420f);
            AddOutline(detail.gameObject, PaperDeep, new Vector2(3f, -3f));
            archive.detailWindow = detail.gameObject;
            archive.detailPortrait = CreatePanel("Portrait", detail, Color.white).GetComponent<Image>();
            archive.detailPortrait.preserveAspect = true;
            SetRect(archive.detailPortrait.rectTransform, 42f, 95f, 248f, 250f);
            archive.detailNameText = CreateText("Name", detail, "", font, 29, Paper, TextAnchor.UpperLeft);
            SetRect(archive.detailNameText.rectTransform, 324f, 322f, 390f, 70f);
            archive.detailStatusText = CreateText("Status Icon", detail, "", font, 36, Paper, TextAnchor.MiddleCenter);
            SetRect(archive.detailStatusText.rectTransform, 710f, 326f, 58f, 55f);
            archive.detailBodyText = CreateText("Detail Records", detail, "", font, 20, Paper, TextAnchor.UpperLeft);
            archive.detailBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            archive.detailBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(archive.detailBodyText.rectTransform, 324f, 72f, 440f, 230f);
            archive.detailCloseButton = CreateButton("Detail Close", detail, "목록", font, Accent, Paper);
            SetRect(archive.detailCloseButton.GetComponent<RectTransform>(), 680f, 18f, 84f, 38f);
            detail.gameObject.SetActive(false);
        }

        private static Button CreateCardTemplate(Transform parent, Font font)
        {
            Button card = new GameObject("Ghost Card Template", typeof(RectTransform), typeof(Image), typeof(Button))
                .GetComponent<Button>();
            card.transform.SetParent(parent, false);
            Image background = card.GetComponent<Image>();
            background.color = new Color(0.15f, 0.22f, 0.27f, 1f);
            AddOutline(card.gameObject, Hex("8EA5AE"), new Vector2(2f, -2f));
            Image portrait = CreatePanel("Portrait", card.transform, Color.white).GetComponent<Image>();
            portrait.preserveAspect = true;
            SetRect(portrait.rectTransform, 12f, 35f, 72f, 96f);
            Text name = CreateText("Name", card.transform, "", font, 19, Paper, TextAnchor.MiddleLeft);
            SetRect(name.rectTransform, 94f, 32f, 126f, 102f);
            return card;
        }

        private static ScrollRect CreateScroll(string name, Transform parent, Vector2 position, Vector2 size, out RectTransform content)
        {
            RectTransform root = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0.09f));
            SetRect(root, position.x, position.y, size.x, size.y);
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = CreatePanel("Viewport", root, new Color(1f, 1f, 1f, 0.01f));
            Stretch(viewport, 0f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 1f);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private static Button CreateHotspot(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Button button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            button.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            SetRect(button.GetComponent<RectTransform>(), position.x, position.y, size.x, size.y);
            return button;
        }

        private static Button CreateButton(string name, Transform parent, string label, Font font, Color color, Color textColor)
        {
            Button button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            button.GetComponent<Image>().color = color;
            Text text = CreateText("Label", button.transform, label, font, 19, textColor, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 5f);
            return button;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image.rectTransform;
        }

        private static Text CreateText(string name, Transform parent, string value, Font font, int size, Color color, TextAnchor anchor)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.text = value;
            text.supportRichText = true;
            return text;
        }

        private static GhostCounselorUIReferences FindUi(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GhostCounselorUIReferences ui = root.GetComponentInChildren<GhostCounselorUIReferences>(true);
                if (ui != null)
                    return ui;
            }
            return null;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(target);
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void SetTopStretch(RectTransform rect, float padding)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -padding);
            rect.sizeDelta = new Vector2(-padding * 2f, 0f);
        }

        private static Color Hex(string value) => ColorUtility.TryParseHtmlString($"#{value}", out Color color) ? color : Color.white;
    }
}
