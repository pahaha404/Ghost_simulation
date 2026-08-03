using UnityEngine;
using UnityEngine.UI;

namespace GhostCounselor
{
    /// <summary>Scene-authored references for the prologue camera and its editable UI.</summary>
    public sealed class PrologueUIReferences : MonoBehaviour
    {
        public Canvas canvas;
        public Image blackBackground;
        public Camera gameCamera;
        public Camera prologueCamera;
        public RectTransform contentRoot;
        public Text storyText;
        public Button continueButton;
        public Text continueButtonText;
        public Button skipButton;
        public Text skipButtonText;

        public bool IsConfigured =>
            canvas != null && blackBackground != null && prologueCamera != null &&
            contentRoot != null && storyText != null &&
            continueButton != null && continueButtonText != null;
    }
}
