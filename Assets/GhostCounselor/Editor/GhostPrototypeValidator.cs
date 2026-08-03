using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GhostCounselor.Editor
{
    public static class GhostPrototypeValidator
    {
        [InitializeOnLoadMethod]
        private static void ValidateAfterReload()
        {
            EditorApplication.delayCall += Validate;
        }

        [MenuItem("Ghost Counselor/Validate Prototype")]
        public static void Validate()
        {
            var ghosts = GhostContentLibrary.Create();
            Require(ghosts.Count == 5, "프로토타입 귀신은 정확히 5명이어야 합니다.");
            Require(ghosts.Select(ghost => ghost.id).Distinct().Count() == ghosts.Count,
                "귀신 ID가 중복되었습니다.");

            foreach (GhostDefinition ghost in ghosts)
            {
                Require(ghost.questions.Count >= 2 && ghost.questions.Count <= 4,
                    $"{ghost.displayName}의 질문은 2~4개여야 합니다.");
                Require(ghost.baseFee > 0, $"{ghost.displayName}의 기본 사례비가 필요합니다.");
                Require(ghost.bonusFee >= 0, $"{ghost.displayName}의 보너스가 올바르지 않습니다.");
                Require(ghost.reactions.Count == Enum.GetValues(typeof(AnswerIntent)).Length,
                    $"{ghost.displayName}의 의도별 반응이 누락되었습니다.");
            }

            var classifier = new LocalIntentClassifier();
            Expect(classifier, "많이 외롭고 힘들었겠네요. 제가 들어줄게요.", AnswerIntent.Empathy);
            Expect(classifier, "먼저 솔직하게 말해보는 게 좋겠어요.", AnswerIntent.PracticalAdvice);
            Expect(classifier, "모르겠어요. 나중에 이야기해요.", AnswerIntent.Avoidance);
            Expect(classifier, "귀찮으니까 당장 꺼져.", AnswerIntent.Aggression);
            Expect(classifier, "나는 냉장고다", AnswerIntent.OffTopic);
            Expect(classifier, "", AnswerIntent.Timeout);

            ValidateSaveSerialization();
            Debug.Log("[귀신 상담소] 콘텐츠, 의도 판정, 저장 데이터 검증 통과: 귀신 5명, 오류 0개");
        }

        private static void ValidateSaveSerialization()
        {
            var source = new SaveData { day = 4, money = 123456, lastGhostId = "sticker" };
            source.ghosts.Add(new GhostProgress
            {
                ghostId = "sticker", visitCount = 2, relationship = 3, specialSolved = true
            });
            source.items.Add("백 년 묵은 종이딱지");
            source.achievements.Add("첫 상담");
            source.seenDialogue.Add("sticker_intro");

            string json = JsonUtility.ToJson(source);
            SaveData restored = JsonUtility.FromJson<SaveData>(json);
            Require(restored.day == source.day && restored.money == source.money,
                "날짜 또는 돈 저장 데이터가 복원되지 않았습니다.");
            Require(restored.ghosts.Count == 1 && restored.ghosts[0].specialSolved,
                "귀신 진행 데이터가 복원되지 않았습니다.");
            Require(restored.items.SequenceEqual(source.items) &&
                    restored.achievements.SequenceEqual(source.achievements) &&
                    restored.seenDialogue.SequenceEqual(source.seenDialogue),
                "수집품, 업적 또는 대사 기록이 복원되지 않았습니다.");
        }

        private static void Expect(
            IIntentClassifier classifier, string answer, AnswerIntent expected)
        {
            AnswerIntent actual = classifier.Classify(answer);
            Require(actual == expected,
                $"“{answer}” 판정이 {expected} 대신 {actual}로 나왔습니다.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"[귀신 상담소 검증 실패] {message}");
        }
    }
}
