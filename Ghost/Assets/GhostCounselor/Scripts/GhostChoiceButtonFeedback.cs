using UnityEngine;
using UnityEngine.EventSystems;

namespace GhostCounselor
{
    [DisallowMultipleComponent]
    public sealed class GhostChoiceButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, Range(1.01f, 1.15f)] private float raisedScale = 1.045f;
        [SerializeField, Min(1f)] private float raisedPixels = 6f;

        private RectTransform rect;
        private bool hovered;
        private bool selected;
        private bool raised;
        private Vector2 restingPosition;
        private Vector3 restingScale;

        private void Awake()
        {
            rect = transform as RectTransform;
            CaptureRestingTransform();
        }

        private void OnEnable()
        {
            rect ??= transform as RectTransform;
            if (!raised)
                CaptureRestingTransform();
        }

        private void OnDisable()
        {
            RestoreRestingTransform();
            raised = false;
            hovered = false;
            selected = false;
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; RefreshPresentation(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; RefreshPresentation(); }
        public void OnSelect(BaseEventData eventData) { selected = true; RefreshPresentation(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; RefreshPresentation(); }

        private void RefreshPresentation()
        {
            bool shouldRaise = hovered || selected;
            if (shouldRaise == raised)
                return;

            if (shouldRaise)
            {
                CaptureRestingTransform();
                raised = true;
                rect.anchoredPosition = restingPosition + Vector2.up * raisedPixels;
                rect.localScale = restingScale * raisedScale;
            }
            else
            {
                RestoreRestingTransform();
                raised = false;
            }
        }

        private void CaptureRestingTransform()
        {
            if (rect == null)
                return;
            restingPosition = rect.anchoredPosition;
            restingScale = rect.localScale;
        }

        private void RestoreRestingTransform()
        {
            if (rect == null)
                return;
            rect.anchoredPosition = restingPosition;
            rect.localScale = restingScale;
        }
    }
}
