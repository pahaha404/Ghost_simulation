using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    /// <summary>Creates and upgrades the scene-editable, camera-based opening story.</summary>
    public static class PrologueUIBuilder
    {
        private static bool waitingForEditMode;
        private const string PrologueBackgroundPath = "Assets/Art/Backgrounds/신당_상담실_프롤로그_밤_v1.png";

        [MenuItem("Ghost Counselor/Create Prologue UI")]
        public static void BuildEditablePrologue()
        {
            PrologueUIReferences existing = FindExisting();
            if (existing != null)
            {
                UpgradeExistingPrologue(existing);
                Selection.activeGameObject = existing.contentRoot.gameObject;
                Debug.Log("[귀신 상담소] Prologue UI를 카메라 연출 구조로 갱신했습니다.");
                return;
            }

            EnsureEventSystem();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Camera gameCamera = FindOrCreateGameCamera();
            Camera prologueCamera = CreatePrologueCamera(gameCamera);

            GameObject canvasObject = new GameObject(
                "Prologue UI - Opening Story",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = prologueCamera;
            canvas.planeDistance = 10f;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            PrologueUIReferences ui = canvasObject.AddComponent<PrologueUIReferences>();
            Image black = CreateImage("Black Background", canvasObject.transform, Color.black);
            Stretch(black.rectTransform);
            ApplyPrologueBackground(black);

            RectTransform contentRoot = new GameObject("Prologue Content - Move Me", typeof(RectTransform)).GetComponent<RectTransform>();
            contentRoot.SetParent(black.transform, false);
            contentRoot.anchorMin = contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(1040f, 620f);

            Text story = CreateText("Story Text", contentRoot, "", 25, new Color(0.95f, 0.94f, 0.89f), TextAnchor.MiddleCenter, font);
            story.horizontalOverflow = HorizontalWrapMode.Wrap;
            story.verticalOverflow = VerticalWrapMode.Overflow;
            story.lineSpacing = 1.23f;
            story.fontStyle = FontStyle.Bold;
            story.gameObject.AddComponent<Shadow>().effectColor = new Color(0.34f, 0.23f, 0.12f, 0.8f);
            SetCenteredRect(story.rectTransform, 0f, 60f, 960f, 230f);

            Button button = CreateButton("Continue Button", contentRoot, "다음  >", font);
            SetCenteredRect(button.GetComponent<RectTransform>(), 360f, -230f, 180f, 54f);
            Text buttonText = button.GetComponentInChildren<Text>();

            Button skipButton = CreateButton("Skip Button", black.transform, "SKIP", font);
            skipButton.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.08f, 0.72f);
            SetTopRightRect(skipButton.GetComponent<RectTransform>(), -34f, -28f, 96f, 40f);
            Text skipButtonText = skipButton.GetComponentInChildren<Text>();
            skipButtonText.fontSize = 15;
            skipButtonText.color = new Color(0.9f, 0.84f, 0.7f, 1f);

            ui.canvas = canvas;
            ui.blackBackground = black;
            ui.gameCamera = gameCamera;
            ui.prologueCamera = prologueCamera;
            ui.contentRoot = contentRoot;
            ui.storyText = story;
            ui.continueButton = button;
            ui.continueButtonText = buttonText;
            ui.skipButton = skipButton;
            ui.skipButtonText = skipButtonText;
            canvasObject.AddComponent<PrologueController>();

            SaveAndSelect(contentRoot.gameObject);
            Debug.Log("[귀신 상담소] 카메라 전환형 프롤로그 UI를 만들었습니다.");
        }

        private static void BuildWhenEditorIsReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (!waitingForEditMode)
                {
                    waitingForEditMode = true;
                    EditorApplication.playModeStateChanged += UpgradeAfterLeavingPlayMode;
                }
                return;
            }
            if (!string.Equals(EditorSceneManager.GetActiveScene().name, "SampleScene", StringComparison.Ordinal))
                return;

            PrologueUIReferences existing = FindExisting();
            if (existing == null)
                BuildEditablePrologue();
            else
                UpgradeExistingPrologue(existing);
        }

        private static void UpgradeAfterLeavingPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            waitingForEditMode = false;
            EditorApplication.playModeStateChanged -= UpgradeAfterLeavingPlayMode;
            EditorApplication.delayCall += BuildWhenEditorIsReady;
        }

        [MenuItem("Ghost Counselor/Upgrade Prologue To Camera Layout")]
        private static void UpgradeExistingPrologueMenu()
        {
            PrologueUIReferences existing = FindExisting();
            if (existing == null)
                BuildEditablePrologue();
            else
                UpgradeExistingPrologue(existing);
        }

        private static void UpgradeExistingPrologue(PrologueUIReferences ui)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Camera gameCamera = FindOrCreateGameCamera();
            Camera prologueCamera = ui.prologueCamera != null ? ui.prologueCamera : CreatePrologueCamera(gameCamera);
            ui.gameCamera = gameCamera;
            ui.prologueCamera = prologueCamera;
            ui.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            ui.canvas.worldCamera = prologueCamera;
            ui.canvas.planeDistance = 10f;
            ui.canvas.overrideSorting = true;
            ui.canvas.sortingOrder = 100;
            ApplyPrologueBackground(ui.blackBackground);

            if (ui.contentRoot == null)
            {
                RectTransform contentRoot = new GameObject("Prologue Content - Move Me", typeof(RectTransform)).GetComponent<RectTransform>();
                contentRoot.SetParent(ui.blackBackground.transform, false);
                contentRoot.anchorMin = contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
                contentRoot.pivot = new Vector2(0.5f, 0.5f);
                contentRoot.anchoredPosition = Vector2.zero;
                contentRoot.sizeDelta = new Vector2(1040f, 620f);
                ui.storyText.rectTransform.SetParent(contentRoot, false);
                ui.continueButton.transform.SetParent(contentRoot, false);
                ui.contentRoot = contentRoot;
            }

            ui.storyText.alignment = TextAnchor.MiddleCenter;
            ui.storyText.fontStyle = FontStyle.Bold;
            ui.storyText.color = new Color(0.95f, 0.94f, 0.89f);
            if (ui.storyText.GetComponent<Shadow>() == null)
                ui.storyText.gameObject.AddComponent<Shadow>().effectColor = new Color(0.34f, 0.23f, 0.12f, 0.8f);
            SetCenteredRect(ui.storyText.rectTransform, 0f, 60f, 960f, 230f);
            SetCenteredRect(ui.continueButton.GetComponent<RectTransform>(), 360f, -230f, 180f, 54f);

            if (ui.skipButton == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ui.skipButton = CreateButton("Skip Button", ui.blackBackground.transform, "SKIP", font);
                ui.skipButtonText = ui.skipButton.GetComponentInChildren<Text>();
            }

            ui.skipButton.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.08f, 0.72f);
            SetTopRightRect(ui.skipButton.GetComponent<RectTransform>(), -34f, -28f, 96f, 40f);
            ui.skipButtonText.fontSize = 15;
            ui.skipButtonText.color = new Color(0.9f, 0.84f, 0.7f, 1f);

            SaveAndSelect(ui.contentRoot.gameObject);
        }

        private static Camera FindOrCreateGameCamera()
        {
            Camera gameCamera = Camera.main;
            if (gameCamera == null)
                gameCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();

            if (gameCamera == null)
            {
                GameObject cameraObject = new GameObject("Game Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.GetComponent<Camera>();
                gameCamera.orthographic = true;
                gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            }

            gameCamera.gameObject.name = "Game Camera";
            gameCamera.depth = 0f;
            return gameCamera;
        }

        private static Camera CreatePrologueCamera(Camera gameCamera)
        {
            GameObject cameraObject = new GameObject("Prologue Camera", typeof(Camera));
            Camera prologueCamera = cameraObject.GetComponent<Camera>();
            cameraObject.transform.SetPositionAndRotation(gameCamera.transform.position, gameCamera.transform.rotation);
            prologueCamera.orthographic = gameCamera.orthographic;
            prologueCamera.orthographicSize = gameCamera.orthographicSize;
            prologueCamera.clearFlags = CameraClearFlags.SolidColor;
            prologueCamera.backgroundColor = Color.black;
            prologueCamera.depth = 1f;
            return prologueCamera;
        }

        private static PrologueUIReferences FindExisting()
        {
            return UnityEngine.Object.FindAnyObjectByType<PrologueUIReferences>();
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static void ApplyPrologueBackground(Image background)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PrologueBackgroundPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[귀신 상담소] 프롤로그 배경을 찾지 못했습니다: {PrologueBackgroundPath}");
                return;
            }

            background.sprite = sprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = Color.white;
        }

        private static Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor alignment, Font font)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.text = content;
            text.supportRichText = true;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string labelText, Font font)
        {
            Image image = CreateImage(name, parent, new Color(0.12f, 0.12f, 0.12f, 1f));
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            button.colors = colors;
            Text label = CreateText("Label", image.transform, labelText, 21, Color.white, TextAnchor.MiddleCenter, font);
            Stretch(label.rectTransform, 6f);
            return button;
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

        private static void SetCenteredRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopRightRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
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

        private static void SaveAndSelect(GameObject selection)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = selection;
        }
    }
}
