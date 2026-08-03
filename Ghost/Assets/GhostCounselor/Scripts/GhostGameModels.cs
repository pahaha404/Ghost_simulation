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
    }

    [Serializable]
    public sealed class GhostProgress
    {
        public string ghostId;
        public int visitCount;
        public int relationship;
        public bool specialSolved;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int day = 1;
        public int money;
        public string lastGhostId = "";
        public List<GhostProgress> ghosts = new();
        public List<string> items = new();
        public List<string> achievements = new();
        public List<string> seenDialogue = new();
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
