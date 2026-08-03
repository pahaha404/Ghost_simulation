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
    /// Existing UI is never moved or recreated, so the GameScene layout remains user-owned.
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
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateImage(RootName, ui.root, Color.clear);
            Stretch(root);
            root.SetAsLastSibling();

            Text guide = CreateText("Answer Guide - Edit Text Here", root, font, 30, Paper,
                "귀신에게 적절한 답변을 제시해주세요!");
            guide.alignment = TextAnchor.MiddleCenter;
            Center(guide.rectTransform, new Vector2(860f, 55f), new Vector2(0f, 248f));
            AddOutline(guide.gameObject, WoodDark, new Vector2(2f, -2f));

            RectTransform inputFrame = CreateImage("Answer Blank Frame - Edit Size Here", root, Wood);
            Center(inputFrame, new Vector2(830f, 76f), new Vector2(0f, 165f));
            AddOutline(inputFrame.gameObject, WoodDark, new Vector2(4f, -4f));

            InputField input = CreateInput("Answer Input - Type Here", inputFrame, font);
            Stretch(input.GetComponent<RectTransform>(), 6f);

            RectTransform keyboard = CreateImage("Typewriter Keyboard - Edit Size Here", root, Wood);
            Center(keyboard, new Vector2(920f, 270f), new Vector2(0f, -38f));
            AddOutline(keyboard.gameObject, WoodDark, new Vector2(5f, -5f));

            RectTransform keyboardTop = CreateImage("Typewriter Metal Strip", keyboard, Brass);
            SetRect(keyboardTop, 30f, 236f, 860f, 12f);
            List<TypewriterKeyView> keys = new();
            CreateKeyboardRows(keyboard, font, keys);

            Image enterLamp = CreateEnterLamp(keyboard, font, out Text enterLabel);
            GhostTypewriterInputUI controller = ui.GetComponent<GhostTypewriterInputUI>();
            if (controller == null)
                controller = Undo.AddComponent<GhostTypewriterInputUI>(ui.gameObject);
            controller.SetReferences(root.gameObject, guide, input, enterLamp, enterLabel, keys.ToArray());

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

        private static void CreateKeyboardRows(Transform keyboard, Font font, List<TypewriterKeyView> keys)
        {
            CreateKeyRow(keyboard, font, keys, 197f, 0f, new[]
            {
                ("1", Key.Digit1), ("2", Key.Digit2), ("3", Key.Digit3), ("4", Key.Digit4), ("5", Key.Digit5),
                ("6", Key.Digit6), ("7", Key.Digit7), ("8", Key.Digit8), ("9", Key.Digit9), ("0", Key.Digit0)
            });
            CreateKeyRow(keyboard, font, keys, 151f, 0f, new[]
            {
                ("ㅂ\nQ", Key.Q), ("ㅈ\nW", Key.W), ("ㄷ\nE", Key.E), ("ㄱ\nR", Key.R), ("ㅅ\nT", Key.T),
                ("ㅛ\nY", Key.Y), ("ㅕ\nU", Key.U), ("ㅑ\nI", Key.I), ("ㅐ\nO", Key.O), ("ㅔ\nP", Key.P)
            });
            CreateKeyRow(keyboard, font, keys, 100f, 30f, new[]
            {
                ("ㅁ\nA", Key.A), ("ㄴ\nS", Key.S), ("ㅇ\nD", Key.D), ("ㄹ\nF", Key.F), ("ㅎ\nG", Key.G),
                ("ㅗ\nH", Key.H), ("ㅓ\nJ", Key.J), ("ㅏ\nK", Key.K), ("ㅣ\nL", Key.L)
            });
            CreateKeyRow(keyboard, font, keys, 49f, 86f, new[]
            {
                ("ㅋ\nZ", Key.Z), ("ㅌ\nX", Key.X), ("ㅊ\nC", Key.C), ("ㅍ\nV", Key.V),
                ("ㅠ\nB", Key.B), ("ㅜ\nN", Key.N), ("ㅡ\nM", Key.M)
            });

            Image space = CreateKey("Space Key", keyboard, font, "공백", new Vector2(340f, 42f), new Vector2(255f, 9f), PaperDark);
            keys.Add(new TypewriterKeyView { key = Key.Space, keyImage = space });
            Image backspace = CreateKey("Backspace Key", keyboard, font, "←", new Vector2(100f, 42f), new Vector2(665f, 9f), PaperDark);
            keys.Add(new TypewriterKeyView { key = Key.Backspace, keyImage = backspace });
        }

        private static void CreateKeyRow(Transform parent, Font font, List<TypewriterKeyView> keys, float y, float startX, (string label, Key key)[] row)
        {
            const float keyWidth = 70f;
            const float gap = 10f;
            float totalWidth = row.Length * keyWidth + (row.Length - 1) * gap;
            float x = (920f - totalWidth) * 0.5f + startX;
            foreach ((string label, Key key) in row)
            {
                Image image = CreateKey($"Key {key}", parent, font, label, new Vector2(keyWidth, 38f), new Vector2(x, y), PaperDark);
                keys.Add(new TypewriterKeyView { key = key, keyImage = image });
                x += keyWidth + gap;
            }
        }

        private static Image CreateEnterLamp(Transform parent, Font font, out Text label)
        {
            RectTransform rect = CreateImage("Enter Light - Grey Until Text", parent, Hex("484D49"));
            SetRect(rect, 767f, 9f, 125f, 84f);
            AddOutline(rect.gameObject, Brass, new Vector2(2f, -2f));
            label = CreateText("Enter Label", rect, font, 17, Hex("D7D6CB"), "ENTER\n↵");
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

            Text typed = CreateText("Text", gameObject.transform, font, 25, Ink, "");
            typed.alignment = TextAnchor.MiddleLeft;
            typed.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(typed.rectTransform, 18f);
            Text placeholder = CreateText("Placeholder", gameObject.transform, font, 22, Hex("8F887D"), "여기에 답변을 입력하세요...");
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.alignment = TextAnchor.MiddleLeft;
            Stretch(placeholder.rectTransform, 18f);
            input.textComponent = typed;
            input.placeholder = placeholder;
            return input;
        }

        private static Image CreateKey(string name, Transform parent, Font font, string caption, Vector2 size, Vector2 position, Color color)
        {
            RectTransform rect = CreateImage(name, parent, color);
            SetRect(rect, position.x, position.y, size.x, size.y);
            AddOutline(rect.gameObject, WoodDark, new Vector2(2f, -2f));
            Text label = CreateText("Label", rect, font, 13, Ink, caption);
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
            Outline outline = Undo.AddComponent<Outline>(gameObject);
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
