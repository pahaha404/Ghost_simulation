/*
 * 파일 역할: 핵심 고민에 답할 때만 쓰는 타자기 입력 UI를 관리합니다.
 * - Show(): 귀신 이름이 들어간 안내 문구를 띄우고 입력 칸을 비운 뒤 포커스를 줍니다.
 * - 입력 칸: 플레이어가 Windows 한글 IME 또는 일반 키보드로 작성하는 실제 답변입니다.
 * - Enter Lamp: 글자가 없으면 회색, 한 글자 이상이면 초록색으로 바뀌는 전송 가능 표시입니다.
 * - Update(): 실제 키보드의 물리 키(Q, W, Enter 등)를 읽어 타자기 키가 잠깐 눌린 것처럼 보이게 합니다.
 * - Enter: 글자가 있을 때 Enter 또는 숫자패드 Enter를 누르면 GameController에 답변 전송을 요청합니다.
 *
 * 화면 배치와 색은 GameScene의 "Typewriter Answer UI - Edit Here" 아래에서 직접 수정합니다.
 * 이 스크립트는 위치를 바꾸지 않고, 표시/숨김과 눌림 효과만 담당합니다.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace GhostCounselor
{
    [Serializable]
    public sealed class TypewriterKeyView
    {
        [Tooltip("실제 키보드의 물리 키입니다. 한글 IME에서도 이 위치를 기준으로 눌림 효과가 납니다.")]
        public Key key;
        [Tooltip("타자기에서 눌리는 키의 Image입니다.")]
        public Image keyImage;
    }

    public sealed class GhostTypewriterInputUI : MonoBehaviour
    {
        [Header("GameScene에서 직접 수정할 UI 참조")]
        [SerializeField] private GameObject typewriterRoot;
        [SerializeField] private Text guideText;
        [SerializeField] private InputField inputField;
        [SerializeField] private Text typedPreviewText;
        [SerializeField] private Image enterLamp;
        [SerializeField] private Text enterLabel;
        [SerializeField] private TypewriterKeyView[] keyViews = Array.Empty<TypewriterKeyView>();

        [Header("입력 상태 색상")]
        [SerializeField] private Color unavailableColor = new(0.28f, 0.30f, 0.29f, 1f);
        [SerializeField] private Color readyColor = new(0.20f, 0.72f, 0.43f, 1f);
        [SerializeField] private Color pressedColor = new(0.85f, 0.61f, 0.35f, 1f);
        [SerializeField, Min(0.03f)] private float keyPressDuration = 0.09f;
        [SerializeField, Range(0.75f, 1f)] private float pressedScale = 0.91f;

        private readonly Dictionary<Image, Color> keyBaseColors = new();
        private readonly Dictionary<Key, float> pressedUntil = new();
        private Action submitted;

        public bool IsConfigured => typewriterRoot != null && guideText != null && inputField != null && enterLamp != null;
        public bool IsShowing => typewriterRoot != null && typewriterRoot.activeSelf;

        private void Awake()
        {
            DisableFullScreenRaycastBlocker();
            ConfigureVisibleInputText();
            CacheKeyColors();
            if (inputField != null)
                inputField.onValueChanged.AddListener(RefreshEnterLamp);
            Hide();
        }

        private void OnDestroy()
        {
            if (inputField != null)
                inputField.onValueChanged.RemoveListener(RefreshEnterLamp);
        }

        public void SetReferences(
            GameObject root,
            Text guide,
            InputField input,
            Text typedPreview,
            Image lamp,
            Text label,
            TypewriterKeyView[] keys)
        {
            typewriterRoot = root;
            guideText = guide;
            inputField = input;
            typedPreviewText = typedPreview;
            enterLamp = lamp;
            enterLabel = label;
            keyViews = keys ?? Array.Empty<TypewriterKeyView>();
            DisableFullScreenRaycastBlocker();
            ConfigureVisibleInputText();
            CacheKeyColors();
        }

        public InputField Show(string guide, Action submitAction)
        {
            if (!IsConfigured)
                return null;

            submitted = submitAction;
            guideText.text = guide;
            typewriterRoot.SetActive(true);
            inputField.text = string.Empty;
            RefreshEnterLamp(inputField.text);
            inputField.ForceLabelUpdate();
            inputField.ActivateInputField();
            inputField.Select();
            return inputField;
        }

        public void Hide()
        {
            submitted = null;
            pressedUntil.Clear();
            RefreshKeyVisuals();
            if (typewriterRoot != null)
                typewriterRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsShowing)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            foreach (TypewriterKeyView view in keyViews)
            {
                if (view == null || view.key == Key.None)
                    continue;

                KeyControl control = keyboard[view.key];
                if (control != null && control.wasPressedThisFrame)
                    pressedUntil[view.key] = Time.unscaledTime + keyPressDuration;
            }
            RefreshKeyVisuals();

            if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) &&
                !string.IsNullOrWhiteSpace(inputField.text))
                Submit();
        }

        private void Submit()
        {
            Action callback = submitted;
            if (callback == null)
                return;

            submitted = null;
            callback.Invoke();
        }

        private void RefreshEnterLamp(string value)
        {
            bool ready = !string.IsNullOrWhiteSpace(value);
            if (inputField != null)
            {
                // Use the InputField's actual Text component as the one visible answer.
                // The previous separate preview made it easy for a manually moved UI to
                // be hidden behind the white answer frame.
                if (inputField.placeholder != null)
                    inputField.placeholder.gameObject.SetActive(!ready);
                if (inputField.textComponent != null)
                {
                    inputField.textComponent.gameObject.SetActive(true);
                    inputField.textComponent.color = new Color(0.12f, 0.09f, 0.09f, 1f);
                    inputField.textComponent.rectTransform.SetAsLastSibling();
                }
                inputField.ForceLabelUpdate();
            }
            if (typedPreviewText != null)
            {
                // Old scenes can still contain the experimental preview object.
                // Keep it inactive so there is only one reliable visible text layer.
                typedPreviewText.gameObject.SetActive(false);
            }
            if (enterLamp != null)
                enterLamp.color = ready ? readyColor : unavailableColor;
            if (enterLabel != null)
                enterLabel.color = ready ? Color.white : new Color(0.78f, 0.78f, 0.73f, 1f);
        }

        private void EnsureTypedPreview()
        {
            if (typedPreviewText != null || inputField == null)
                return;

            Transform parent = inputField.transform.parent;
            Transform existing = parent != null ? parent.Find("Typed Answer Preview - Always Visible") : null;
            if (existing != null)
            {
                typedPreviewText = existing.GetComponent<Text>();
                return;
            }

            if (parent == null)
                return;

            // This is only a runtime safety net for older GameScene layouts that were
            // created before the preview text existed. It never changes parent positions.
            GameObject previewObject = new("Typed Answer Preview - Always Visible", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            previewObject.transform.SetParent(parent, false);
            typedPreviewText = previewObject.GetComponent<Text>();
            typedPreviewText.font = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "LegacyRuntime.ttf" }, 20);
            typedPreviewText.fontSize = 20;
            typedPreviewText.color = new Color(0.19f, 0.15f, 0.15f, 1f);
            typedPreviewText.alignment = TextAnchor.MiddleLeft;
            typedPreviewText.horizontalOverflow = HorizontalWrapMode.Overflow;
            typedPreviewText.verticalOverflow = VerticalWrapMode.Truncate;
            typedPreviewText.raycastTarget = false;
            RectTransform rect = typedPreviewText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 24f);
            rect.offsetMax = new Vector2(-24f, -24f);
            rect.SetAsLastSibling();
        }

        private void DisableFullScreenRaycastBlocker()
        {
            if (typewriterRoot == null)
                return;

            Image rootImage = typewriterRoot.GetComponent<Image>();
            if (rootImage != null)
                rootImage.raycastTarget = false;
        }

        private void ConfigureVisibleInputText()
        {
            if (inputField == null || inputField.textComponent == null)
                return;

            Text visibleText = inputField.textComponent;
            visibleText.gameObject.SetActive(true);
            visibleText.color = new Color(0.12f, 0.09f, 0.09f, 1f);
            visibleText.supportRichText = false;
            visibleText.raycastTarget = false;
            visibleText.rectTransform.SetAsLastSibling();

            // This is deliberately attached only to the text child, not the whole
            // typewriter. It gives the answer letters their own foreground sorting
            // layer without moving any user-authored RectTransform.
            Canvas foregroundCanvas = visibleText.GetComponent<Canvas>();
            if (foregroundCanvas == null)
                foregroundCanvas = visibleText.gameObject.AddComponent<Canvas>();
            foregroundCanvas.overrideSorting = true;
            foregroundCanvas.sortingOrder = 100;
        }

        private void CacheKeyColors()
        {
            keyBaseColors.Clear();
            foreach (TypewriterKeyView view in keyViews)
            {
                if (view?.keyImage != null && !keyBaseColors.ContainsKey(view.keyImage))
                    keyBaseColors.Add(view.keyImage, view.keyImage.color);
            }
        }

        private void RefreshKeyVisuals()
        {
            foreach (TypewriterKeyView view in keyViews)
            {
                if (view?.keyImage == null)
                    continue;

                bool pressed = pressedUntil.TryGetValue(view.key, out float until) && Time.unscaledTime < until;
                if (!keyBaseColors.TryGetValue(view.keyImage, out Color baseColor))
                    baseColor = view.keyImage.color;

                view.keyImage.color = pressed ? pressedColor : baseColor;
                view.keyImage.rectTransform.localScale = pressed ? Vector3.one * pressedScale : Vector3.one;
            }
        }
    }
}
