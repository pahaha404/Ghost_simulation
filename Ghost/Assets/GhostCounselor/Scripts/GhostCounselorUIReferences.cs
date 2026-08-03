using System;
using UnityEngine;
using UnityEngine.UI;

namespace GhostCounselor
{
    [Serializable]
    public sealed class GhostPortraitSet
    {
        [Tooltip("GhostContentLibrary의 귀신 ID입니다. 예: mirror")]
        public string ghostId;
        [Tooltip("해당 귀신이 방문했을 때 표시할 기본 초상입니다.")]
        public Sprite defaultPortrait;
        [Tooltip("거울각시처럼 질문 선택에 따라 바꿀 표정 순서입니다. 기본 표정도 첫 항목에 넣습니다.")]
        public Sprite[] questionExpressionSequence;
    }

    /// <summary>
    /// Scene-authored UI references. The layout is intentionally kept in the scene so it can
    /// be edited through Unity's Hierarchy and Inspector instead of being hard-coded at runtime.
    /// </summary>
    public sealed class GhostCounselorUIReferences : MonoBehaviour
    {
        [Header("Fixed layout")]
        public Canvas canvas;
        public RectTransform root;
        public Text dayText;
        public Text phaseText;
        public Text moneyText;
        public RectTransform content;
        [Tooltip("대화 카드 위의 화자 이름표 배경입니다. Name 텍스트의 위치와 별도로 수정할 수 있습니다.")]
        public Image speakerNamePlate;
        public Image portraitPanel;
        [Header("Portrait image")]
        [Tooltip("모든 귀신이 공유하는 실제 이미지 초상 슬롯입니다.")]
        public Image portraitImage;
        [Tooltip("귀신 ID별 기본 초상과 선택지 표정입니다.")]
        public GhostPortraitSet[] ghostPortraits;
        public Text nameText;
        public Text titleText;
        public Text dialogueText;
        public Text timerText;
        public RectTransform actionArea;

        [Header("Outcome ledger")]
        [Tooltip("상담 결과 때만 오른쪽에 표시할 공책형 장부 패널입니다.")]
        public Image ledgerPanel;
        public Text ledgerText;

        [Header("Editable templates")]
        [Tooltip("Duplicate this to style every runtime-created answer button.")]
        public Button actionButtonTemplate;
        [Tooltip("Duplicate this to style the natural-language input field.")]
        public InputField answerInputTemplate;

        public bool IsConfigured =>
            canvas != null && root != null && dayText != null && moneyText != null &&
            content != null && portraitPanel != null && nameText != null &&
            titleText != null && dialogueText != null && timerText != null &&
            actionArea != null && actionButtonTemplate != null && answerInputTemplate != null;

        public void ApplyRuntimeFont(Font font)
        {
            if (font == null)
                return;

            foreach (Text text in GetComponentsInChildren<Text>(true))
                text.font = font;
        }
    }
}
