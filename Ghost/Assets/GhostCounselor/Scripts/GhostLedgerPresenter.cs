/*
 * 파일 역할: 상담소 장부 패널에 표시할 숫자와 정산 문구만 관리한다.
 * - ShowDayStart(): 아침의 현재 보유금과 남은 영업일을 장부에 표시한다.
 * - ShowCounselResult(): 사례비, 보너스, 물건 환전, 오늘 수입을 표시한다.
 * - ShowNight(): 하루 종료 뒤 누적 보유금, 만난 귀신, 특별 해결, 업적을 표시한다.
 * - Hide(): 장부를 닫는다.
 * 화면 흐름과 귀신 대사는 GhostGameController가, 장부의 문구 형식은 이 파일이 담당한다.
 */
using UnityEngine.UI;

namespace GhostCounselor
{
    public sealed class GhostLedgerPresenter
    {
        private readonly Image panel;

        public GhostLedgerPresenter(Image panel, Text text)
        {
            this.panel = panel;
        }

        public void Hide()
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
        }

        public void ShowDayStart(int money, int remainingDays)
        {
            Show(
                $"현재 보유금: {money:N0}원\n" +
                $"남은 영업일: {remainingDays}일");
        }

        public void ShowCounselResult(CounselResult result)
        {
            if (result == null)
                return;

            Show(
                $"기본 사례비   {result.basePay:N0}원\n" +
                $"상담 보너스   {result.bonusPay:N0}원\n" +
                (result.itemPay > 0
                    ? $"물건 환전     {result.itemPay:N0}원\n"
                    : "물건 환전     -\n") +
                "────────────\n" +
                $"오늘 수입     {result.TotalPay:N0}원");
        }

        public void ShowNight(int money, int metGhosts, int totalGhosts, int specialSolvedCount, int achievementCount)
        {
            Show(
                $"누적 보유금: {money:N0}원\n" +
                $"만난 귀신: {metGhosts}/{totalGhosts}\n" +
                $"특별 해결: {specialSolvedCount}건\n" +
                $"업적: {achievementCount}개");
        }

        private void Show(string value)
        {
            // The ledger is now opened deliberately through the desk book.  This presenter
            // remains as a compatibility boundary for the existing game-flow calls.
            Hide();
        }
    }
}
