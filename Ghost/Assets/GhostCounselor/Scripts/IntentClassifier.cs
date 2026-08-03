using System;
using System.Collections.Generic;
using System.Linq;

namespace GhostCounselor
{
    public interface IIntentClassifier
    {
        bool IsAvailable { get; }
        AnswerIntent Classify(string answer);
    }

    /// <summary>
    /// 서버 키 없이도 프로토타입 전체를 플레이할 수 있는 오프라인 판정기입니다.
    /// 실제 AI 서버를 연결할 때에도 서버는 AnswerIntent 값만 반환해야 합니다.
    /// </summary>
    public sealed class LocalIntentClassifier : IIntentClassifier
    {
        private static readonly string[] EmpathyWords =
        {
            "이해", "힘들", "슬펐", "외로", "괜찮", "마음", "미안", "함께", "들어줄", "잊는 게 아니",
            "그랬구나", "그랬군", "안타까", "고생", "당신 탓"
        };

        private static readonly string[] AdviceWords =
        {
            "해봐", "하세요", "하는 게", "방법", "먼저", "말해", "전해", "놓아", "가보", "시도",
            "천천히", "정리", "선택", "물어봐"
        };

        private static readonly string[] AvoidanceWords =
        {
            "모르", "글쎄", "나중", "상관없", "알아서", "말할 수 없", "패스", "됐어"
        };

        private static readonly string[] AggressionWords =
        {
            "꺼져", "닥쳐", "바보", "멍청", "싫어", "귀찮", "죽어", "웃기", "한심", "네 탓"
        };

        public bool IsAvailable => true;

        public AnswerIntent Classify(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return AnswerIntent.Timeout;

            string normalized = answer.Trim().ToLowerInvariant();
            if (normalized.Length < 2)
                return AnswerIntent.OffTopic;

            var scores = new Dictionary<AnswerIntent, int>
            {
                { AnswerIntent.Empathy, Score(normalized, EmpathyWords) },
                { AnswerIntent.PracticalAdvice, Score(normalized, AdviceWords) },
                { AnswerIntent.Avoidance, Score(normalized, AvoidanceWords) },
                { AnswerIntent.Aggression, Score(normalized, AggressionWords) }
            };

            int best = scores.Values.Max();
            if (best == 0)
                return LooksContextual(normalized) ? AnswerIntent.PracticalAdvice : AnswerIntent.OffTopic;

            return scores.First(pair => pair.Value == best).Key;
        }

        private static int Score(string answer, IEnumerable<string> words)
        {
            return words.Count(answer.Contains);
        }

        private static bool LooksContextual(string answer)
        {
            string[] counselingWords =
            {
                "생각", "좋겠", "해도 돼", "하지 않아도", "말해", "전해", "마음",
                "괜찮", "잊지", "기억", "사람", "천천히", "도와", "선택"
            };
            return answer.Length >= 5 && counselingWords.Any(answer.Contains);
        }
    }
}
