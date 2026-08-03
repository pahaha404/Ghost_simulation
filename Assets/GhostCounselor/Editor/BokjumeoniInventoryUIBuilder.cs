/*
 * 파일 역할: GameScene에 편집 가능한 복주머니 인벤토리 UI를 명시적으로 생성한다.
 * - 메뉴를 눌렀을 때만 씬을 저장한다. 컴파일이나 임포트만으로 UI를 만들지 않는다.
 * - 복주머니 위치는 `Bokjumeoni Button - Edit Position Here`에서 직접 수정한다.
 */
using GhostCounselor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GhostCounselor.Editor
{
    public static class BokjumeoniInventoryUIBuilder
    {
        private const string IconPath = "Assets/Art/UI/bokjumeoni_inventory_v1.png";
        private static readonly Color Ink = Hex("2D2020");
        private static readonly Color Paper = Hex("F2E3C5");
        private static readonly Color PaperDeep = Hex("D1B587");
        private static readonly Color Accent = Hex("B8564F");
        private static readonly Color Wood = Hex("3A211D");

        [MenuItem("Ghost Counselor/Bokjumeoni Inventory/Create Or Replace In Active GameScene")]
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

            ConfigureIconImporter();
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            if (icon == null)
            {
                Debug.LogWarning($"[Ghost Counselor] 복주머니 아이콘을 찾지 못했습니다: {IconPath}");
                if (openedTemporarily)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ui.root.gameObject, "Create Bokjumeoni Inventory UI");
            Transform old = ui.root.Find("Bokjumeoni Inventory - Edit Here");
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GhostBokjumeoniInventoryUI inventory = ui.GetComponent<GhostBokjumeoniInventoryUI>();
            if (inventory == null)
                inventory = Undo.AddComponent<GhostBokjumeoniInventoryUI>(ui.gameObject);

            RectTransform holder = CreatePanel("Bokjumeoni Inventory - Edit Here", ui.root, Color.clear);
            Stretch(holder, 0f);
            holder.GetComponent<Image>().raycastTarget = false;

            inventory.pouchButton = CreatePouchButton(holder, icon, font, out Text count);
            inventory.itemCountText = count;
            BuildInventoryWindow(holder, inventory, font);
            inventory.inventoryWindow.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
            else
                Selection.activeGameObject = inventory.pouchButton.gameObject;

            Debug.Log("[Ghost Counselor] 우측 하단 복주머니 인벤토리 UI를 생성했습니다.");
        }

        private static Button CreatePouchButton(Transform parent, Sprite icon, Font font, out Text count)
        {
            Button button = new GameObject("Bokjumeoni Button - Edit Position Here", typeof(RectTransform), typeof(Image), typeof(Button))
                .GetComponent<Button>();
            button.transform.SetParent(parent, false);
            Image image = button.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = true;
            SetBottomRight(button.GetComponent<RectTransform>(), -18f, 16f, 118f, 118f);
            AddOutline(button.gameObject, Hex("2B1517"), new Vector2(2f, -2f));

            RectTransform badge = CreatePanel("Item Count Badge", button.transform, Accent);
            SetTopRight(badge, -2f, -4f, 34f, 34f);
            AddOutline(badge.gameObject, Hex("F7E9C9"), new Vector2(1f, -1f));
            count = CreateText("Count", badge, "0", font, 20, Paper, TextAnchor.MiddleCenter);
            Stretch(count.rectTransform, 2f);
            count.raycastTarget = false;
            return button;
        }

        private static void BuildInventoryWindow(Transform parent, GhostBokjumeoniInventoryUI inventory, Font font)
        {
            RectTransform window = CreatePanel("Bokjumeoni Inventory Window", parent, new Color(0f, 0f, 0f, 0.58f));
            Stretch(window, 0f);
            inventory.inventoryWindow = window.gameObject;

            RectTransform card = CreatePanel("Bokjumeoni Inventory Card", window, Paper);
            SetCenter(card, 0f, 0f, 605f, 444f);
            AddOutline(card.gameObject, Wood, new Vector2(5f, -5f));

            RectTransform namePlate = CreatePanel("Title Plate", card, Accent);
            SetRect(namePlate, 118f, 390f, 370f, 66f);
            AddOutline(namePlate.gameObject, Wood, new Vector2(3f, -3f));
            Text title = CreateText("Title", namePlate, "복주머니", font, 33, Paper, TextAnchor.MiddleCenter);
            Stretch(title.rectTransform, 4f);

            Text guide = CreateText("Guide", card, "귀신들이 상담비 대신 남기고 간 물건", font, 19, Accent, TextAnchor.MiddleCenter);
            SetRect(guide.rectTransform, 54f, 335f, 497f, 35f);

            RectTransform listPaper = CreatePanel("Item List Paper", card, Hex("E9D5AB"));
            SetRect(listPaper, 54f, 78f, 497f, 238f);
            AddOutline(listPaper.gameObject, PaperDeep, new Vector2(2f, -2f));
            inventory.inventoryListText = CreateText("Item List", listPaper, "", font, 24, Ink, TextAnchor.UpperLeft);
            inventory.inventoryListText.horizontalOverflow = HorizontalWrapMode.Wrap;
            inventory.inventoryListText.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(inventory.inventoryListText.rectTransform, 24f);

            inventory.closeButton = CreateButton("Close", card, "닫기", font, Wood, Paper);
            SetRect(inventory.closeButton.GetComponent<RectTransform>(), 243f, 20f, 120f, 42f);
        }

        private static void ConfigureIconImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
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
            return text;
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

        private static void SetCenter(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetBottomRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
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

        private static Color Hex(string value) => ColorUtility.TryParseHtmlString($"#{value}", out Color color) ? color : Color.white;
    }
}
