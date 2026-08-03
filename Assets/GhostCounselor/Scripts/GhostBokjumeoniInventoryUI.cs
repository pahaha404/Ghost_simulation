/*
 * 파일 역할: 우측 하단 복주머니 인벤토리 UI의 열기, 닫기, 물건 목록 표시를 담당한다.
 * - GhostGameController가 현재 SaveData를 Bind()로 전달한다.
 * - SaveData.items에 있는 특별 보상 이름을 복주머니 창에 그대로 표시한다.
 * - 버튼, 창, 글씨의 위치와 색은 GameScene Hierarchy에서 직접 수정한다.
 */
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GhostCounselor
{
    public sealed class GhostBokjumeoniInventoryUI : MonoBehaviour
    {
        [Header("복주머니 버튼")]
        public Button pouchButton;
        public Text itemCountText;

        [Header("보관함 창")]
        public GameObject inventoryWindow;
        public Text inventoryListText;
        public Button closeButton;

        private SaveData save;
        private bool wired;

        public bool IsShowing => inventoryWindow != null && inventoryWindow.activeSelf;

        public void Bind(SaveData gameSave)
        {
            save = gameSave ?? new SaveData();
            save.items ??= new List<string>();
            WireButtons();
            Refresh();
            if (inventoryWindow != null)
                inventoryWindow.SetActive(false);
        }

        public void Refresh()
        {
            if (save == null)
                return;

            save.items ??= new List<string>();
            if (itemCountText != null)
                itemCountText.text = save.items.Count.ToString();

            if (inventoryListText == null)
                return;

            inventoryListText.text = save.items.Count == 0
                ? "아직 복주머니에 든\n특별한 물건이 없습니다."
                : string.Join("\n\n", save.items
                    .Select((item, index) => $"{index + 1}.  {item}"));
        }

        public bool TryCloseFromKeyboard()
        {
            if (!IsShowing)
                return false;

            Close();
            return true;
        }

        private void WireButtons()
        {
            if (wired)
                return;

            wired = true;
            Wire(pouchButton, Open);
            Wire(closeButton, Close);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void Open()
        {
            if (inventoryWindow == null)
                return;

            Refresh();
            inventoryWindow.SetActive(true);
        }

        private void Close()
        {
            if (inventoryWindow != null)
                inventoryWindow.SetActive(false);
        }
    }
}
