using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    /// <summary>
    /// Creates one scene-authored modal for narration and inner thoughts. It only runs from its menu,
    /// and never repositions an already-created modal so the designer remains in control of the layout.
    /// </summary>
    public static class InnerThoughtModalBuilder
    {
        private const string ModalName = "Inner Thought Modal - Edit Here";
        private static readonly Color Dim = new(0.12f, 0.13f, 0.15f, 0.56f);
        private static readonly Color Frame = Hex("8F4845");
        private static readonly Color FrameDark = Hex("5E302F");
        private static readonly Color Paper = Hex("F5EDCF");
        private static readonly Color Ink = Hex("302625");

        [MenuItem("Ghost Counselor/Inner Thought Modal/Create In Active GameScene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[귀신 상담소] Play Mode를 멈춘 뒤 안내 모달을 만드세요.");
                return;
            }

            GhostCounselorUIReferences ui = Object.FindAnyObjectByType<GhostCounselorUIReferences>();
            if (ui == null || ui.root == null)
            {
                Debug.LogWarning("[귀신 상담소] 먼저 GameScene의 Counselor UI를 열어 주세요.");
                return;
            }

            Transform existing = ui.root.Find(ModalName);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[귀신 상담소] 안내 모달이 이미 있습니다. Hierarchy에서 직접 수정하세요.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ui.root.gameObject, "Create Inner Thought Modal");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform modal = CreateImage(ModalName, ui.root, Color.clear);
            Stretch(modal);
            modal.SetAsLastSibling();

            RectTransform dim = CreateImage("Dim Overlay - Edit Color Here", modal, Dim);
            Stretch(dim);
            dim.GetComponent<Image>().raycastTarget = true;

            RectTransform frame = CreateImage("Thought Card Frame - Edit Size Here", modal, Frame);
            Center(frame, new Vector2(650f, 410f), Vector2.zero);
            AddOutline(frame.gameObject, FrameDark, new Vector2(4f, -4f));

            RectTransform body = CreateImage("Thought Card Body", frame, Paper);
            Stretch(body, 9f);
            body.GetComponent<Image>().raycastTarget = false;

            Text message = CreateText("Message - Edit Text Here", body, font, 34, Ink,
                "여기에 안내 멘트나\n무당의 속마음을 입력합니다.");
            message.alignment = TextAnchor.MiddleCenter;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Overflow;
            Center(message.rectTransform, new Vector2(540f, 220f), new Vector2(0f, 35f));

            Button confirm = CreateButton("Confirm Button", body, font);
            Center(confirm.GetComponent<RectTransform>(), new Vector2(310f, 58f), new Vector2(0f, -130f));
            Text confirmText = confirm.GetComponentInChildren<Text>(true);
            confirmText.text = "확인";

            GhostInnerThoughtModal controller = ui.GetComponent<GhostInnerThoughtModal>();
            if (controller == null)
                controller = Undo.AddComponent<GhostInnerThoughtModal>(ui.gameObject);
            controller.SetReferences(modal.gameObject, message, confirmText, confirm);

            modal.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = modal.gameObject;
            Debug.Log("[귀신 상담소] 중앙 안내/속마음 모달을 만들었습니다. 메뉴에서 Preview를 켜거나 Hierarchy에서 직접 수정하세요.");
        }

        [MenuItem("Ghost Counselor/Inner Thought Modal/Show Preview In Editor")]
        private static void ShowPreview()
        {
            SetPreviewVisible(true);
        }

        [MenuItem("Ghost Counselor/Inner Thought Modal/Hide Preview In Editor")]
        private static void HidePreview()
        {
            SetPreviewVisible(false);
        }

        private static void SetPreviewVisible(bool visible)
        {
            GhostCounselorUIReferences ui = Object.FindAnyObjectByType<GhostCounselorUIReferences>();
            Transform modal = ui != null && ui.root != null ? ui.root.Find(ModalName) : null;
            if (modal == null)
            {
                Debug.LogWarning("[귀신 상담소] 먼저 Create In Active GameScene 메뉴로 안내 모달을 만드세요.");
                return;
            }

            modal.gameObject.SetActive(visible);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = modal.gameObject;
        }

        private static RectTransform CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image.rectTransform;
        }

        private static Text CreateText(string name, Transform parent, Font font, int size, Color color, string value)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Font font)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = Paper;
            AddOutline(gameObject, Frame, new Vector2(3f, -3f));

            Button button = gameObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Paper;
            colors.highlightedColor = Hex("FFF8DC");
            colors.pressedColor = Hex("E8D7B3");
            colors.selectedColor = Hex("FFF8DC");
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Text label = CreateText("Label", gameObject.transform, font, 27, Ink, "확인");
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform);
            return button;
        }

        private static void AddOutline(GameObject gameObject, Color color, Vector2 distance)
        {
            Outline outline = gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(gameObject);
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void Center(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
