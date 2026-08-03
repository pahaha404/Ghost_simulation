using GhostCounselor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    /// <summary>
    /// Creates the editable visual shell for the counselling countdown dial.
    /// This is an explicit menu action so it never overwrites manually moved UI.
    /// </summary>
    public static class CounselingTimerDialBuilder
    {
        [MenuItem("Ghost Counselor/Counselling Timer Dial/Create Or Replace In Active GameScene")]
        public static void CreateOrReplace()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            GhostCounselorUIReferences ui = FindUi(scene);
            if (ui == null || ui.root == null)
            {
                Debug.LogWarning("[귀신 상담소] GameScene의 Counselor UI를 먼저 열어 주세요.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ui.root.gameObject, "Create Counselling Timer Dial");
            Transform old = ui.root.Find("Counselling Timer Dial - Edit Position Here");
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            if (ui.timerText != null)
                ui.timerText.gameObject.SetActive(false);

            GhostCounselingTimerDial controller = ui.GetComponent<GhostCounselingTimerDial>();
            if (controller == null)
                controller = Undo.AddComponent<GhostCounselingTimerDial>(ui.gameObject);

            RectTransform root = CreateRect("Counselling Timer Dial - Edit Position Here", ui.root);
            SetRect(root, 1035f, 340f, 166f, 166f);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

            GhostTimerDialGraphic face = CreateGraphic("Parchment Clock Face", root, GhostTimerDialGraphic.DialPart.Face, Hex("F3E4C8"));
            Stretch(face.rectTransform, 10f);
            GhostTimerDialGraphic wedge = CreateGraphic("Red Countdown Area", root, GhostTimerDialGraphic.DialPart.RemainingWedge, new Color(0.67f, 0.12f, 0.13f, 0.80f));
            Stretch(wedge.rectTransform, 12f);
            GhostTimerDialGraphic ticks = CreateGraphic("Clock Tick Marks", root, GhostTimerDialGraphic.DialPart.Ticks, Hex("4A2722"));
            Stretch(ticks.rectTransform, 9f);
            GhostTimerDialGraphic hand = CreateGraphic("Clock Hand", root, GhostTimerDialGraphic.DialPart.Hand, Hex("3A1F1D"));
            Stretch(hand.rectTransform, 12f);
            GhostTimerDialGraphic rim = CreateGraphic("Bronze Rim", root, GhostTimerDialGraphic.DialPart.Rim, Hex("8E552E"));
            Stretch(rim.rectTransform, 0f);

            controller.SetReferences(root.gameObject, wedge, hand, group);
            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[귀신 상담소] 원형 상담 제한시간 초시계를 만들었습니다. 위치는 Counselling Timer Dial에서 직접 조정할 수 있습니다.");
        }

        private static GhostCounselorUIReferences FindUi(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GhostCounselorUIReferences ui = root.GetComponentInChildren<GhostCounselorUIReferences>(true);
                if (ui != null) return ui;
            }
            return null;
        }

        private static GhostTimerDialGraphic CreateGraphic(string name, Transform parent, GhostTimerDialGraphic.DialPart part, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(GhostTimerDialGraphic));
            gameObject.transform.SetParent(parent, false);
            GhostTimerDialGraphic graphic = gameObject.GetComponent<GhostTimerDialGraphic>();
            SerializedObject serialized = new(graphic);
            serialized.FindProperty("part").enumValueIndex = (int)part;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            graphic.color = color;
            graphic.raycastTarget = false;
            return graphic;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
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

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString($"#{hex}", out Color color) ? color : Color.white;
    }
}
