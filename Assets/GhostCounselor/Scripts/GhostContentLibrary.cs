/*
 * 파일 역할: 프로토타입에 등장하는 귀신 콘텐츠를 코드로 만든다.
 * - Create(): 5명의 귀신과 첫 방문/후속 상담 데이터를 목록으로 생성한다.
 * - Make(): 성격, 대사, 사례비, 귀신 물건, 선호 답변 의도를 하나의 데이터로 묶는다.
 * - Q(): 질문과 질문 후속 대사를 QuestionData로 만든다.
 * - Find(): 저장된 귀신 ID로 현재 귀신 데이터를 찾는다.
 * 이 파일은 UI를 그리지 않고 상담에 사용할 고정 원고와 보상 데이터만 제공한다.
 */
using System.Collections.Generic;
using System.Linq;

namespace GhostCounselor
{
    public static class GhostContentLibrary
    {
        public static IReadOnlyList<GhostDefinition> Create()
        {
            return new List<GhostDefinition>
            {
                Make(
                    "sticker", "딱지 할아버지", "신당에 눌러앉은 첫 번째 손님",
                    "말이 많고 능청스럽다. 자기 딱지가 세계 최고라고 믿는다.",
                    "저기, 무당 양반. 내 딱지를 아무도 안 받아줘. 이게 얼마나 귀한 건데!",
                    "또 왔네. 지난번 말대로 해봤는데, 아직 하나가 마음에 걸려.",
                    "내 딱지가 낡아서 싫다는 애들한테 뭐라고 해야 해?",
                    "이제 이 딱지를 놓아줘도 내가 사라지지는 않는 거지?",
                    12000, 9000,
                    "백 년 묵은 종이딱지", 8000, AnswerIntent.Empathy,
                    Q("누구에게 딱지를 주고 싶었어요?", "손주 녀석. 살아 있을 땐 한 판도 못 놀아줬지.", "꿈에라도 한 번 더 만나고 싶었어."),
                    Q("왜 그렇게 딱지가 중요해요?", "내가 손으로 접은 마지막 물건이거든.", "중요한 건 종이가 아니라 미안한 마음이었나 봐."),
                    Q("요즘 아이들은 뭘 좋아하는지 알아요?", "딱지 아니면 뭘 좋아한단 말이야?", "그래, 받는 애 마음도 물어봐야겠구먼.")
                ),
                Make(
                    "mirror", "거울각시 연화", "오해받은 소문의 주인",
                    "차갑게 말하지만 남의 시선을 몹시 신경 쓴다.",
                    "사람들이 거울을 보면 제가 저주한다고 해요. 전 그냥 말을 걸고 싶었을 뿐인데.",
                    "당신 조언대로 먼저 인사했더니 비명을 덜 지르더군요. 조금은요.",
                    "제가 무섭다는 사람에게 어떻게 다가가야 하죠?",
                    "제 얼굴이 아니라 제 말을 봐주는 사람이 생길까요?",
                    18000, 12000,
                    "금 간 은거울 조각", 11000, AnswerIntent.PracticalAdvice,
                    Q("거울에 나타나는 이유가 있어요?", "다른 곳에서는 제 모습도 목소리도 남지 않아요.", "거울은 감옥이면서 유일한 창문이에요."),
                    Q("사람들에게 무슨 말을 하고 싶었어요?", "머리 뒤에 먼지가 묻었다고 알려준 것뿐인데요.", "제가 너무 갑자기 나타난 건 인정해요."),
                    Q("저주했다는 소문은 어디서 시작됐어요?", "시험을 망친 학생이 제 탓을 했어요.", "그 아이도 핑계가 필요했겠죠.")
                ),
                Make(
                    "bus", "막차 소년 민우", "정류장을 떠나지 못하는 아이",
                    "조용하고 예의 바르며 기다리는 일에 익숙하다.",
                    "막차가 아직 안 왔어요. 엄마가 집에서 기다리실 텐데.",
                    "어제는 정류장 밖으로 세 걸음 나가 봤어요. 오늘은 더 갈 수 있을까요?",
                    "버스를 기다리지 않으면 엄마에게 어떻게 돌아가죠?",
                    "기다림을 끝내면 엄마를 잊는 게 되는 건가요?",
                    15000, 13000,
                    "빛바랜 버스 회수권", 9000, AnswerIntent.Empathy,
                    Q("얼마나 오래 기다렸어요?", "처음엔 겨울이었는데, 벌써 벚꽃을 백 번은 봤어요.", "시간을 세는 것도 이제 그만하고 싶어요."),
                    Q("마지막으로 기억나는 일이 뭐예요?", "비가 왔고, 길 건너편에서 엄마가 손을 흔들었어요.", "제가 건너가려다 사고가 났어요."),
                    Q("엄마에게 꼭 전하고 싶은 말이 있어요?", "늦어서 미안하다고요. 그래도 집에 가고 싶었다고.", "그 말만 전해진다면 버스는 필요 없을지도 몰라요.")
                ),
                Make(
                    "merchant", "저승상인 만복", "값을 매길 수 없는 것을 파는 장사꾼",
                    "쾌활하고 계산이 빠르지만 손해 보는 일을 두려워한다.",
                    "내 보물들이 왜 안 팔리는지 좀 봐주시오. 전부 저승 명품인데!",
                    "지난번 감정은 제법이었소. 오늘은 정말 아끼는 물건을 가져왔지.",
                    "추억에도 값을 매길 수 있다고 생각하시오?",
                    "이 물건을 팔지 않으면, 나는 장사꾼이 아닌 게 되는 걸까?",
                    22000, 16000,
                    "도깨비 시장 엽전", 15000, AnswerIntent.PracticalAdvice,
                    Q("가장 팔고 싶은 물건은 뭐예요?", "첫 거래에서 받은 녹슨 숟가락이오. 이상하게 아무도 안 사.", "사실 팔고 싶다기보다 자랑하고 싶은 것 같소."),
                    Q("돈을 모아서 어디에 쓰려고요?", "더 큰 가게! 더 비싼 간판! ...그다음은 모르겠군.", "목표가 없다는 걸 들키니 영 쑥스럽소."),
                    Q("손해를 보면 어떻게 되는데요?", "내 가치까지 깎이는 기분이 들지.", "물건값과 내 값은 다른데 말이오.")
                ),
                Make(
                    "bell", "방울무녀 해주", "신당의 과거를 아는 수수께끼 손님",
                    "다정하지만 핵심을 숨긴다. 주인공의 스승과 오래된 인연이 있다.",
                    "이 신당 방울 소리가 아직도 삐걱거리네. 네 스승은 손질도 안 가르쳤니?",
                    "오늘은 네가 얼마나 무당다워졌는지 보러 왔단다.",
                    "빚을 다 갚고 나면, 이 신당에서 무엇을 하고 싶니?",
                    "돈이 없어도 귀신들의 이야기를 계속 들어줄 수 있겠니?",
                    20000, 18000,
                    "이름 없는 무녀의 청동방울", 18000, AnswerIntent.Empathy,
                    Q("제 스승을 알고 계세요?", "알다마다. 그 고집불통이 처음 상담한 귀신이 나였으니.", "이 신당은 퇴마보다 이야기를 듣는 곳이었단다."),
                    Q("왜 신당 방울이 망가졌어요?", "듣지 않으려는 사람이 흔들면 방울도 입을 닫지.", "네가 진심으로 들으면 다시 울릴 거야."),
                    Q("저를 시험하러 온 건가요?", "시험이라기보다 확인이지. 네가 돈만 보고 있는지.", "먹고사는 건 중요해. 다만 손님의 마음도 장부에 적으렴.")
                )
            };
        }

        public static GhostDefinition Find(IReadOnlyList<GhostDefinition> ghosts, string id)
        {
            return ghosts.FirstOrDefault(ghost => ghost.id == id);
        }

        private static QuestionData Q(string prompt, string first, string followUp)
        {
            return new QuestionData(prompt, first, followUp);
        }

        private static GhostDefinition Make(
            string id, string name, string title, string personality,
            string greeting, string followUpGreeting, string critical, string followUpCritical,
            int baseFee, int bonusFee,
            string item, int itemValue, AnswerIntent preferred, params QuestionData[] questions)
        {
            return new GhostDefinition
            {
                id = id,
                displayName = name,
                title = title,
                personality = personality,
                firstGreeting = greeting,
                followUpGreeting = followUpGreeting,
                criticalQuestion = critical,
                followUpCriticalQuestion = followUpCritical,
                baseFee = baseFee,
                bonusFee = bonusFee,
                rewardItem = item,
                rewardValue = itemValue,
                preferredIntent = preferred,
                questions = questions.ToList(),
                reactions = new Dictionary<AnswerIntent, string>
                {
                    { AnswerIntent.Empathy, "내 마음을 그렇게 봐준 사람은 오랜만이네." },
                    { AnswerIntent.PracticalAdvice, "흠, 살아 있는 사람의 방법도 제법 쓸 만하군." },
                    { AnswerIntent.Avoidance, "대답하기 곤란한가 보네. 오늘은 여기까지만 하지." },
                    { AnswerIntent.Aggression, "상담하러 왔더니 싸움을 거는군. 기억해 두겠어." },
                    { AnswerIntent.OffTopic, "무슨 말인지 모르겠어. 혹시 무당이 아니라 다른 게 들린 건가?" },
                    { AnswerIntent.Timeout, "대답이 없네… 내 얼굴을 보고도 정신을 놓지 말라고." }
                }
            };
        }
    }
}
