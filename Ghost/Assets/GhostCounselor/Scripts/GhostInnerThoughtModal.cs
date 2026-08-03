/*
 * 파일 역할: 중앙 안내 멘트와 무당의 속마음을 표시하는 재사용 모달을 제어한다.
 * - Show(): 딤 레이어와 카드 UI를 열고 문구, 확인 버튼 문구, 확인 후 실행할 동작을 지정한다.
 * - Hide(): 모달을 닫아 기존 상담 UI 입력을 다시 사용할 수 있게 한다.
 * - Confirm(): 확인 버튼을 누르면 모달을 닫고 등록된 다음 동작을 실행한다.
 * 실제 카드의 위치·크기·색은 GameScene Hierarchy에서 편집하고, 이 파일은 표시 상태만 관리한다.
 */
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GhostCounselor
{
    public sealed class GhostInnerThoughtModal : MonoBehaviour
    {
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Text messageText;
        [SerializeField] private Text confirmText;
        [SerializeField] private Button confirmButton;

        private Action onConfirmed;

        public bool IsShowing => modalRoot != null && modalRoot.activeSelf;

        private void Awake()
        {
            WireConfirmButton();
            Hide();
        }

        public void SetReferences(GameObject root, Text message, Text confirmLabel, Button confirm)
        {
            modalRoot = root;
            messageText = message;
            confirmText = confirmLabel;
            confirmButton = confirm;
            WireConfirmButton();
        }

        public void Show(string message, string confirmLabel = "확인", Action confirmedAction = null)
        {
            if (modalRoot == null)
                return;

            onConfirmed = confirmedAction;
            if (messageText != null)
                messageText.text = message;
            if (confirmText != null)
                confirmText.text = confirmLabel;

            modalRoot.SetActive(true);
        }

        public void Hide()
        {
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void Confirm()
        {
            Action callback = onConfirmed;
            onConfirmed = null;
            Hide();
            callback?.Invoke();
        }

        private void WireConfirmButton()
        {
            if (confirmButton == null)
                return;

            confirmButton.onClick.RemoveListener(Confirm);
            confirmButton.onClick.AddListener(Confirm);
        }
    }
}
