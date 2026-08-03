/*
 * 파일 역할: PrologueScene에서 수동 배치한 카메라와 UI를 PrologueController에 연결한다.
 * - gameCamera/prologueCamera: 게임 배경 카메라와 프롤로그 카메라를 구분한다.
 * - blackBackground/contentRoot: 배경과 스토리 콘텐츠의 위치를 지정한다.
 * - storyText: 문단별 스트리밍 텍스트를 표시한다.
 * - continueButton/skipButton: 다음 문단, 시작하기, 프롤로그 건너뛰기 입력을 받는다.
 * - IsConfigured: 프롤로그 실행에 필요한 최소 참조가 연결됐는지 검사한다.
 */
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
