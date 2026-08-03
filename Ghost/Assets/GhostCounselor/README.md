# 《귀신 상담소》 7일 프로토타입

## 실행

1. Unity에서 `Assets/Scenes/SampleScene.unity`를 엽니다.
2. Play 버튼을 누릅니다.
3. 별도 씬 설정 없이 `GhostGameController`가 게임 UI를 자동 생성합니다.

## 구현된 범위

- 7일, 하루 한 명 상담
- 첫 방문을 우선하는 무작위 손님 배정과 6~7일 차 재방문
- 귀신 5명, 각 3개 질문, 첫 방문/후속 방문 대사
- 10초 자유 입력, 5초 이하 공포 표정 및 화면 흔들림
- 공감/현실적 조언/회피/공격/문맥 이탈/시간 초과 판정
- 미해결/부분 해결/해결/특별 해결 결과
- 사례비, 보너스, 특별 물건 자동 환전, 관계 변화, 업적, 최종 등급
- JSON 자동 저장과 이어하기
- 자유 입력 대신 사용할 수 있는 판정 선택지

## AI 연결 경계

현재 빌드는 서버 키 없이 플레이할 수 있도록 `LocalIntentClassifier`를 사용합니다.
실제 AI 서버를 연결할 때는 `IIntentClassifier`를 구현하고, 서버가 승인된
`AnswerIntent` 값만 돌려주도록 유지해야 합니다. 대사, 보상, 관계와 엔딩은
Unity의 `GhostGameController`가 결정합니다.

## 저장 위치

저장 파일은 Unity의 `Application.persistentDataPath` 아래
`ghost_counselor_save.json`으로 생성됩니다. 제목 화면의 `새로 시작`은 기존
저장을 삭제하고 1일 차부터 시작합니다.

## 콘텐츠 검증

Unity 상단 메뉴의 `Ghost Counselor > Validate Prototype`을 누르면 귀신 수,
ID 중복, 질문 수, 보상 데이터, 의도별 반응과 대표 한국어 문장 판정을
한 번에 검사합니다.
