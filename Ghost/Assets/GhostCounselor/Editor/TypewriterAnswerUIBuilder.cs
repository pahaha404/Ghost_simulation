using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace GhostCounselor.Editor
{
    /// <summary>
    /// Creates the typewriter-answer UI only when a designer explicitly selects the menu item.
    /// Existing UI is untouched unless the designer explicitly selects the keyboard refit menu.
    /// </summary>
    public static class TypewriterAnswerUIBuilder
    {
        private const string RootName = "Typewriter Answer UI - Edit Here";
        private static readonly Color Ink = Hex("302625");
        private static readonly Color Paper = Hex("F5EDCF");
        private static readonly Color PaperDark = Hex("DAC49C");
        private static readonly Color Wood = Hex("4C2926");
        private static readonly Color WoodDark = Hex("2C191A");
        private static readonly Color Brass = Hex("C8914D");

        [MenuItem("Ghost Counselor/Typewriter Answer UI/Create In Active GameScene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[귀신 상담소] Play Mode를 멈춘 뒤 타자기 입력 UI를 만들어 주세요.");
                return;
            }

            GhostCounselorUIReferences ui = Object.FindAnyObjectByType<GhostCounselorUIReferences>();
            if (ui == null || ui.root == null)
            {
                Debug.LogWarning("[귀신 상담소] GameScene을 열고 Counselor UI를 먼저 불러와 주세요.");
                return;
            }

            Transform existing = ui.root.Find(RootName);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[귀신 상담소] 타자기 입력 UI가 이미 있습니다. Hierarchy에서 직접 수정해 주세요.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ui.root.gameObject, "Create Typewriter Answer UI");
            Font font = GetKoreanFont();

            RectTransform root = CreateImage(RootName, ui.root, Color.clear);
            Stretch(root);
            root.SetAsLastSibling();
            root.GetComponent<Image>().raycastTarget = false;

            Text guide = CreateText("Answer Guide - Edit Text Here", root, font, 30, Paper,
                "귀신에게 적절한 답변을 제시해주세요!");
            guide.alignment = TextAnchor.MiddleCenter;
            Center(guide.rectTransform, new Vector2(860f, 36f), new Vector2(0f, -88f));
            AddOutline(guide.gameObject, WoodDark, new Vector2(2f, -2f));

            RectTransform inputFrame = CreateImage("Answer Blank Frame - Edit Size Here", root, Wood);
            Center(inputFrame, new Vector2(830f, 48f), new Vector2(0f, -155f));
            AddOutline(inputFrame.gameObject, WoodDark, new Vector2(4f, -4f));

            InputField input = CreateInput("Answer Input - Type Here", inputFrame, font);
            Stretch(input.GetComponent<RectTransform>(), 6f);
            Text typedPreview = CreateTypedPreview(inputFrame, font);

            List<TypewriterKeyView> keys = new();
            RectTransform keyboard = CreateImage("Typewriter Keyboard - Edit Size Here", root, Wood);
            Image enterLamp = ConfigureCompactKeyboard(keyboard, font, keys, out Text enterLabel, applyInitialLayout: true);
            GhostTypewriterInputUI controller = ui.GetComponent<GhostTypewriterInputUI>();
            if (controller == null)
                controller = Undo.AddComponent<GhostTypewriterInputUI>(ui.gameObject);
            controller.SetReferences(root.gameObject, guide, input, typedPreview, enterLamp, enterLabel, keys.ToArray());

            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[귀신 상담소] 타자기 입력 UI를 만들었습니다. Typewriter Answer UI - Edit Here 아래에서 위치와 색을 직접 수정할 수 있습니다.");
        }

        [MenuItem("Ghost Counselor/Typewriter Answer UI/Show Preview In Editor")]
        private static void ShowPreview() => SetPreview(true);

        [MenuItem("Ghost Counselor/Typewriter Answer UI/Hide Preview In Editor")]
        private static void HidePreview() => SetPreview(false);

        [MenuItem("Ghost Counselor/Typewriter Answer UI/Refresh Keyboard Keys Only (Preserve Position)")]
        private static void RefitKeyboard()
        {
            GhostCounselorUIReferences ui = Object.FindAnyObjectByType<GhostCounselorUIReferences>();
            Transform root = ui != null && ui.root != null ? ui.root.Find(RootName) : null;
            Transform keyboard = root != null ? root.Find("Typewriter Keyboard - Edit Size Here") : null;
            if (ui == null || root == null || keyboard == null)
            {
                Debug.LogWarning("[귀신 상담소] 먼저 Create In Active GameScene 메뉴로 타자기 UI를 만들어 주세요.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Refit Typewriter Keyboard");
            for (int index = keyboard.childCount - 1; index >= 0; index--)
                Undo.DestroyObjectImmediate(keyboard.GetChild(index).gameObject);

            Font font = GetKoreanFont();
            List<TypewriterKeyView> keys = new();
            Image enterLamp = ConfigureCompactKeyboard(
                keyboard.GetComponent<RectTransform>(), font, keys, out Text enterLabel, applyInitialLayout: false);
            GhostTypewriterInputUI controller = ui.GetComponent<GhostTypewriterInputUI>();
            InputField input = root.GetComponentInChildren<InputField>(true);
            Text guide = FindDirectText(root, "Answer Guide - Edit Text Here");
            Text typedPreview = EnsureTypedPreview(root, font);
            RefreshInputTextStyle(guide, input, typedPreview, font);
            if (controller != null && input != null && guide != null)
                controller.SetReferences(root.gameObject, guide, input, typedPreview, enterLamp, enterLabel, keys.ToArray());

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = keyboard.gameObject;
            Debug.Log("[귀신 상담소] 현재 배치 위치는 유지하고, 타자기 키·한글 폰트·입력 표시만 새로 고쳤습니다.");
        }

        private static void SetPreview(bool visible)
        {
            GhostCounselorUIReferences ui = Object.FindAnyObjectByType<GhostCounselorUIReferences>();
            Transform root = ui != null && ui.root != null ? ui.root.Find(RootName) : null;
            if (root == null)
            {
                Debug.LogWarning("[귀신 상담소] 먼저 Create In Active GameScene 메뉴를 실행해 주세요.");
                return;
            }

            root.gameObject.SetActive(visible);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
        }

        private static Image ConfigureCompactKeyboard(
            RectTransform keyboard,
            Font font,
            List<TypewriterKeyView> keys,
            out Text enterLabel,
            bool applyInitialLayout)
        {
            // Only the first creation receives the default location. Later menu refreshes
            // preserve the designer-authored RectTransform values exactly as they are.
            if (applyInitialLayout)
                SetTopCentered(keyboard, new Vector2(920f, 150f), -210f);
            Image keyboardImage = keyboard.GetComponent<Image>();
            keyboardImage.color = Wood;
            AddOutline(keyboard.gameObject, WoodDark, new Vector2(5f, -5f));

            RectTransform keyboardTop = CreateImage("Typewriter Metal Strip", keyboard, Brass);
            SetRect(keyboardTop, 24f, 139f, 872f, 4f);
            CreateKeyRow(keyboard, font, keys, 110f, new[]
            {
                ("1", Key.Digit1), ("2", Key.Digit2), ("3", Key.Digit3), ("4", Key.Digit4), ("5", Key.Digit5),
                ("6", Key.Digit6), ("7", Key.Digit7), ("8", Key.Digit8), ("9", Key.Digit9), ("0", Key.Digit0)
            });
            Image backspace = CreateKey("Backspace Key", keyboard, font, "지움", new Vector2(64f, 23f), new Vector2(828.5f, 110f), PaperDark);
            keys.Add(new TypewriterKeyView { key = Key.Backspace, keyImage = backspace });
            CreateKeyRow(keyboard, font, keys, 82f, new[]
            {
                ("ㅂ", Key.Q), ("ㅈ", Key.W), ("ㄷ", Key.E), ("ㄱ", Key.R), ("ㅅ", Key.T),
                ("ㅛ", Key.Y), ("ㅕ", Key.U), ("ㅑ", Key.I), ("ㅐ", Key.O), ("ㅔ", Key.P)
            });
            CreateKeyRow(keyboard, font, keys, 54f, new[]
            {
                ("ㅁ", Key.A), ("ㄴ", Key.S), ("ㅇ", Key.D), ("ㄹ", Key.F), ("ㅎ", Key.G),
                ("ㅗ", Key.H), ("ㅓ", Key.J), ("ㅏ", Key.K), ("ㅣ", Key.L)
            });
            CreateKeyRow(keyboard, font, keys, 26f, new[]
            {
                ("ㅋ", Key.Z), ("ㅌ", Key.X), ("ㅊ", Key.C), ("ㅍ", Key.V),
                ("ㅠ", Key.B), ("ㅜ", Key.N), ("ㅡ", Key.M)
            });

            Image space = CreateKey("Space Key", keyboard, font, "공백", new Vector2(330f, 18f), new Vector2(295f, 3f), PaperDark);
            keys.Add(new TypewriterKeyView { key = Key.Space, keyImage = space });
            return CreateEnterLamp(keyboard, font, out enterLabel);
        }

        private static void CreateKeyRow(Transform parent, Font font, List<TypewriterKeyView> keys, float y, (string label, Key key)[] row)
        {
            const float keyWidth = 66f;
            const float keyHeight = 23f;
            const float gap = 7f;
            float totalWidth = row.Length * keyWidth + (row.Length - 1) * gap;
            float x = (920f - totalWidth) * 0.5f;
            foreach ((string label, Key key) in row)
            {
                Image image = CreateKey($"Key {key}", parent, font, label, new Vector2(keyWidth, keyHeight), new Vector2(x, y), PaperDark);
                keys.Add(new TypewriterKeyView { key = key, keyImage = image });
                x += keyWidth + gap;
            }
        }

        private static Image CreateEnterLamp(Transform parent, Font font, out Text label)
        {
            RectTransform rect = CreateImage("Enter Light - Grey Until Text", parent, Hex("484D49"));
            SetRect(rect, 767f, 3f, 125f, 48f);
            AddOutline(rect.gameObject, Brass, new Vector2(2f, -2f));
            label = CreateText("Enter Label", rect, font, 17, Hex("D7D6CB"), "입력");
            label.alignment = TextAnchor.MiddleCenter;
            Stretch(label.rectTransform);
            return rect.GetComponent<Image>();
        }

        private static InputField CreateInput(string name, Transform parent, Font font)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            gameObject.transform.SetParent(parent, false);
            Image background = gameObject.GetComponent<Image>();
            background.color = new Color(1f, 0.99f, 0.95f, 1f);
            InputField input = gameObject.GetComponent<InputField>();
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 100;

            Text typed = CreateText("Text", gameObject.transform, font, 20, Ink, "");
            typed.alignment = TextAnchor.MiddleLeft;
            typed.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(typed.rectTransform, 18f);
            Text placeholder = CreateText("Placeholder", gameObject.transform, font, 18, Hex("8F887D"), "여기에 답변을 입력하세요...");
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.alignment = TextAnchor.MiddleLeft;
            Stretch(placeholder.rectTransform, 18f);
            input.textComponent = typed;
            input.placeholder = placeholder;
            typed.rectTransform.SetAsLastSibling();
            return input;
        }

        private static Image CreateKey(string name, Transform parent, Font font, string caption, Vector2 size, Vector2 position, Color color)
        {
            RectTransform rect = CreateImage(name, parent, color);
            SetRect(rect, position.x, position.y, size.x, size.y);
            AddOutline(rect.gameObject, WoodDark, new Vector2(2f, -2f));
            Text label = CreateText("Label", rect, font, 15, Ink, caption);
            label.alignment = TextAnchor.MiddleCenter;
            label.lineSpacing = 0.72f;
            Stretch(label.rectTransform);
            return rect.GetComponent<Image>();
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

        private static void AddOutline(GameObject gameObject, Color color, Vector2 distance)
        {
            Outline outline = gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(gameObject);
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

        private static void Center(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopCentered(RectTransform rect, Vector2 size, float topY)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, topY);
            rect.sizeDelta = size;
        }

        private static Text FindDirectText(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static void RefreshInputTextStyle(Text guide, InputField input, Text typedPreview, Font font)
        {
            if (guide != null)
            {
                guide.fontSize = 20;
                guide.font = font;
            }

            if (input != null)
            {
                if (input.textComponent != null)
                {
                    input.textComponent.fontSize = 20;
                    input.textComponent.font = font;
                    input.textComponent.color = new Color(0f, 0f, 0f, 0f);
                }
                if (input.placeholder is Text placeholder)
                {
                    placeholder.fontSize = 18;
                    placeholder.font = font;
                }
            }

            if (typedPreview != null)
            {
                typedPreview.font = font;
                typedPreview.fontSize = 20;
                typedPreview.color = Ink;
                typedPreview.rectTransform.SetAsLastSibling();
            }
        }

        private static Text EnsureTypedPreview(Transform root, Font font)
        {
            Transform frame = root.Find("Answer Blank Frame - Edit Size Here");
            if (frame == null)
                return null;

            Transform existing = frame.Find("Typed Answer Preview - Always Visible");
            return existing != null
                ? existing.GetComponent<Text>()
                : CreateTypedPreview(frame, font);
        }

        private static Text CreateTypedPreview(Transform parent, Font font)
        {
            Text preview = CreateText("Typed Answer Preview - Always Visible", parent, font, 20, Ink, "");
            preview.alignment = TextAnchor.MiddleLeft;
            preview.horizontalOverflow = HorizontalWrapMode.Overflow;
            preview.verticalOverflow = VerticalWrapMode.Truncate;
            preview.raycastTarget = false;
            Stretch(preview.rectTransform, 24f);
            preview.rectTransform.SetAsLastSibling();
            return preview;
        }

        private static Font GetKoreanFont()
        {
            return Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "LegacyRuntime.ttf" }, 24);
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
