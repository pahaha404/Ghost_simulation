using UnityEngine;

namespace GhostCounselor
{
    /// <summary>핵심 답변 10초 동안 원형 초시계를 표시하고 진행도를 갱신합니다.</summary>
    public sealed class GhostCounselingTimerDial : MonoBehaviour
    {
        [SerializeField] private GameObject dialRoot;
        [SerializeField] private GhostTimerDialGraphic progressWedge;
        [SerializeField] private GhostTimerDialGraphic hand;
        [SerializeField] private CanvasGroup dialCanvasGroup;

        private float duration = 10f;

        public bool IsConfigured => dialRoot != null && progressWedge != null && hand != null;

        private void Awake() => Hide();

        public void SetReferences(GameObject root, GhostTimerDialGraphic wedge, GhostTimerDialGraphic handGraphic, CanvasGroup group)
        {
            dialRoot = root;
            progressWedge = wedge;
            hand = handGraphic;
            dialCanvasGroup = group;
        }

        public void Show(float seconds)
        {
            duration = Mathf.Max(0.01f, seconds);
            if (dialRoot != null) dialRoot.SetActive(true);
            SetRemaining(seconds);
        }

        public void SetRemaining(float seconds)
        {
            if (!IsConfigured) return;
            float remaining = Mathf.Clamp01(seconds / duration);
            float elapsed = 1f - remaining;
            progressWedge.SetState(remaining, elapsed);
            hand.SetState(remaining, elapsed);
            if (dialCanvasGroup != null)
                dialCanvasGroup.alpha = remaining <= 0.5f ? 1f : 0.9f;
        }

        public void Hide()
        {
            if (dialRoot != null) dialRoot.SetActive(false);
        }
    }
}
