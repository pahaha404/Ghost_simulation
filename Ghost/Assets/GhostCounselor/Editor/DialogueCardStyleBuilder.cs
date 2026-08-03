using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    /// <summary>
    /// Applies the visual-novel conversation card without putting any positions in runtime code.
    /// After it has run, every object remains a normal editable GameObject in GameScene.
    /// </summary>
    public static class DialogueCardStyleBuilder
    {
        private static readonly Color Ink = Hex("302625");
        private static readonly Color Paper = Hex("F6EDCF");
        private static readonly Color PaperHighlight = Hex("FFF8DC");
        private static readonly Color Frame = Hex("743B38");
        private static readonly Color Nameplate = Hex("B95552");

        [MenuItem("Ghost Counselor/Apply Dialogue Card UI Style")]
        public static bool ApplyDialogueCardStyle()
        {
            Scene targetScene = EditorSceneManager.GetActiveScene();
            bool openedTemporarily = false;
            GhostCounselorUIReferences ui = FindUiInScene(targetScene);
            if (ui == null)
            {
                targetScene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Additive);
                openedTemporarily = true;
                ui = FindUiInScene(targetScene);
            }

            if (ui == null || !ui.IsConfigured)
            {
                Debug.LogWarning("[Ghost Counselor] GameScene의 Counselor UI를 찾지 못했습니다.");
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(targetScene, true);
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Apply Dialogue Card UI Style");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Card frame / cream dialogue body.
            Image frame = ui.content.GetComponent<Image>();
            frame.color = Frame;
            AddOrGetOutline(frame.gameObject, Hex("422625"), new Vector2(3f, -3f));
            SetRect(ui.content, 20f, 315f, 360f, 250f);

            RectTransform body = FindOrCreateImage("Dialogue Body", ui.content, Paper);
            body.SetAsFirstSibling();
            SetRect(body, 7f, 7f, 346f, 236f);
            Image bodyImage = body.GetComponent<Image>();
            bodyImage.color = Paper;
            bodyImage.raycastTarget = false;

            // This red label is intentionally separate from the dialogue body, so its position
            // and width can be adjusted in the Hierarchy without affecting the text card.
            RectTransform nameplate = FindOrCreateImage("Speaker Name Plate", ui.content, Nameplate);
            SetRect(nameplate, 40f, 228f, 280f, 54f);
            Image nameplateImage = nameplate.GetComponent<Image>();
            nameplateImage.color = Nameplate;
            nameplateImage.raycastTarget = false;
            AddOrGetOutline(nameplate.gameObject, Frame, new Vector2(3f, -3f));
            ui.speakerNamePlate = nameplateImage;

            ui.nameText.transform.SetParent(nameplate, false);
            ui.nameText.font = font;
            ui.nameText.fontSize = 27;
            ui.nameText.color = Hex("FCEFD0");
            ui.nameText.alignment = TextAnchor.MiddleCenter;
            Stretch(ui.nameText.rectTransform, 10f, 4f);

            ui.titleText.transform.SetParent(body, false);
            ui.titleText.font = font;
            ui.titleText.fontSize = 16;
            ui.titleText.color = Hex("A85A51");
            ui.titleText.alignment = TextAnchor.MiddleLeft;
            SetRect(ui.titleText.rectTransform, 20f, 169f, 306f, 28f);

            ui.dialogueText.transform.SetParent(body, false);
            ui.dialogueText.font = font;
            ui.dialogueText.fontSize = 24;
            ui.dialogueText.color = Ink;
            ui.dialogueText.alignment = TextAnchor.UpperLeft;
            ui.dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(ui.dialogueText.rectTransform, 20f, 22f, 306f, 142f);

            // The runtime controller duplicates this template. A VerticalLayoutGroup makes
            // 1, 2, 3, or 4 choices automatically stack from the bottom.
            SetRect(ui.actionArea, 20f, 30f, 360f, 220f);
            ConfigureChoiceStack(ui.actionArea);
            StyleChoiceTemplate(ui.actionButtonTemplate, font);

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            if (openedTemporarily)
                EditorSceneManager.CloseScene(targetScene, true);
            else
                Selection.activeGameObject = ui.content.gameObject;
            Debug.Log("[Ghost Counselor] 대화 카드와 자동 선택지 스택을 적용했습니다.");
            return true;
        }

        private static GhostCounselorUIReferences FindUiInScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GhostCounselorUIReferences ui = root.GetComponentInChildren<GhostCounselorUIReferences>(true);
                if (ui != null)
                    return ui;
            }
            return null;
        }

        private static void StyleChoiceTemplate(Button button, Font font)
        {
            Image image = button.GetComponent<Image>();
            image.color = Paper;
            AddOrGetOutline(button.gameObject, Frame, new Vector2(2f, -2f));

            ColorBlock colors = button.colors;
            colors.normalColor = Paper;
            colors.highlightedColor = PaperHighlight;
            colors.pressedColor = Hex("EAD9B6");
            colors.selectedColor = PaperHighlight;
            colors.disabledColor = Hex("BBAF99");
            colors.colorMultiplier = 1f;
            button.colors = colors;

            LayoutElement layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = 360f;
            layout.minHeight = 46f;
            layout.preferredHeight = 46f;

            Text label = button.GetComponentInChildren<Text>(true);
            label.font = font;
            label.fontSize = 19;
            label.color = Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 19;
        }

        private static void ConfigureChoiceStack(RectTransform area)
        {
            HorizontalLayoutGroup horizontal = area.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
                Undo.DestroyObjectImmediate(horizontal);

            VerticalLayoutGroup stack = area.GetComponent<VerticalLayoutGroup>();
            if (stack == null)
                stack = Undo.AddComponent<VerticalLayoutGroup>(area.gameObject);
            stack.spacing = 8f;
            stack.childAlignment = TextAnchor.LowerCenter;
            stack.childControlWidth = true;
            stack.childControlHeight = false;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
        }

        private static RectTransform FindOrCreateImage(string name, Transform parent, Color color)
        {
            Transform existing = parent.Find(name);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }
            image.color = color;
            return image.rectTransform;
        }

        private static void AddOrGetOutline(GameObject gameObject, Color color, Vector2 distance)
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

        private static void Stretch(RectTransform rect, float horizontalPadding, float verticalPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString($"#{value}", out Color color) ? color : Color.white;
        }
    }
}
