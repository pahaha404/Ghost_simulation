/*
 * 파일 역할: 상담 시스템에서 여러 스크립트가 함께 사용하는 데이터 형식을 정의한다.
 * - GamePhase: 하루 시작부터 방문, 상담, 핵심 답변, 결과, 밤 결산까지의 상태다.
 * - AnswerIntent: 공감/조언/회피/공격/문맥 이탈/시간 초과 의도다.
 * - CounselOutcome: 미해결/부분 해결/해결/특별 해결 결과다.
 * - QuestionData/GhostDefinition/GhostStoryVisitData: 질문, 귀신 성격, 회차별 대사와 보상을 담는다.
 * - GhostProgress/SaveData: 귀신별 관계, 방문 횟수, 스토리 회차, 날짜, 돈, 물건, 업적을 저장한다.
 * - CounselResult: 한 번의 상담 판정 결과와 최종 지급액을 담는다.
 * 이 파일은 게임을 직접 실행하지 않고 다른 시스템이 사용할 자료 구조만 제공한다.
 */
using System;
using System.Collections.Generic;

namespace GhostCounselor
{
    public enum GamePhase
    {
        DayStart,
        Visit,
        Counseling,
        CriticalAnswer,
        Result,
        Night,
        Ending
    }

    public enum AnswerIntent
    {
        Empathy,
        PracticalAdvice,
        Avoidance,
        Aggression,
        OffTopic,
        Timeout
    }

    public enum CounselOutcome
    {
        Unresolved,
        Partial,
        Solved,
        SpecialSolved
    }

    public sealed class QuestionData
    {
        public string prompt;
        public string firstReply;
        public string followUpReply;

        public QuestionData(string prompt, string firstReply, string followUpReply)
        {
            this.prompt = prompt;
            this.firstReply = firstReply;
            this.followUpReply = followUpReply;
        }
    }

    public sealed class GhostDefinition
    {
        public string id;
        public string displayName;
        public string title;
        public string personality;
        public string firstGreeting;
        public string followUpGreeting;
        public string criticalQuestion;
        public string followUpCriticalQuestion;
        public int baseFee;
        public int bonusFee;
        public string rewardItem;
        public int rewardValue;
        public AnswerIntent preferredIntent;
        public List<QuestionData> questions;
        public Dictionary<AnswerIntent, string> reactions;
        // 본편에서는 귀신마다 네 번의 서로 다른 상담 원고를 연결한다.
        public List<GhostStoryVisitData> storyVisits = new();
    }

    /// <summary>
    /// 한 귀신의 특정 상담 회차에 필요한 화면 원고다.
    /// 질문 버튼, 핵심 질문, 의도별 반응, 다음 회차 해금 정보가 한 묶음으로 움직인다.
    /// </summary>
    public sealed class GhostStoryVisitData
    {
        public int stage;
        public string stageTitle;
        public string greeting;
        public string questionGuide;
        public string criticalQuestion;
        public AnswerIntent preferredIntent;
        public List<QuestionData> questions = new();
        public Dictionary<AnswerIntent, string> reactions = new();
        public string successAction;
        public string retryGreeting;
        public string successFlag;
        public string purificationLine;
        public string cinematicId;
        public string cinematicSummary;
    }

    [Serializable]
    public sealed class GhostProgress
    {
        public string ghostId;
        public int visitCount;
        // 해결한 회차 수다. 미해결 상담은 같은 회차를 다시 시도한다.
        public int storyStage;
        public int relationship;
        public bool specialSolved;
        public bool purified;
        public bool cinematicSeen;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int day = 1;
        public int money;
        public string lastGhostId = "";
        public int counselsCompletedToday;
        public List<string> ghostsMetToday = new();
        public List<GhostProgress> ghosts = new();
        public List<string> items = new();
        public List<string> achievements = new();
        public List<string> seenDialogue = new();
        public List<string> storyFlags = new();
        public List<LedgerRecord> ledgerRecords = new();
    }

    [Serializable]
    public sealed class LedgerRecord
    {
        public int day;
        public string ghostId;
        public string ghostName;
        public int money;
        public string summary;
        public CounselOutcome outcome;
    }

    public sealed class CounselResult
    {
        public AnswerIntent intent;
        public CounselOutcome outcome;
        public int basePay;
        public int bonusPay;
        public int itemPay;
        public string itemName;
        public int relationshipDelta;
        public string reaction;

        public int TotalPay => basePay + bonusPay + itemPay;
    }
}
