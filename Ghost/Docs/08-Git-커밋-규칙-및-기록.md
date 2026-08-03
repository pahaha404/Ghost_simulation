# Git 커밋 규칙 및 변경 기록

> 이 문서는 "언제 저장됐는지", "무엇이 바뀌었는지", "되돌리면 어디로 돌아가는지"를 개발자가 쉽게 확인하기 위한 기록이다.

## 기본 원칙

- 기능 하나가 완성되거나, 버그 하나가 해결되거나, 씬·UI·에셋 구조가 안정되면 그 시점에 커밋한다.
- 커밋하기 전에는 Unity Play 모드를 끄고 씬을 저장한다.
- Unity의 `Library`, `Temp`, `Logs` 등 자동 생성 폴더는 커밋하지 않는다.
- 한 커밋에는 가능한 한 하나의 목적만 담는다. 예: "귀신 정보창 추가"와 "배경 교체"는 가능하면 나눈다.
- 코드·씬·프리팹·에셋을 함께 바꿨다면, 서로 어떤 관계인지 기록에 함께 적는다.
- 테스트하지 못한 것은 "확인 필요"라고 적고, 실제로 확인한 것처럼 쓰지 않는다.

## 커밋이 필요한 시점

아래 중 하나에 해당하면 작업이 끝난 시점에 커밋한다.

1. 플레이어가 체감하는 기능을 추가하거나 완성했을 때
2. 실행을 막던 오류, 씬 손상, UI 위치 문제를 복구했을 때
3. `GameScene`, `PrologueScene`, 프리팹, 저장 데이터 구조를 바꿨을 때
4. 새로운 배경·귀신 초상·UI 아트처럼 프로젝트에 계속 남을 에셋을 추가했을 때
5. 기획·스토리·작업 규칙이 이후 개발 방향에 영향을 줄 정도로 바뀌었을 때
6. 여러 작업을 하기 전, 현재 안정 상태를 되돌릴 기준점으로 남기고 싶을 때

글자 오탈자 하나 같은 아주 작은 수정은 다음 관련 작업과 함께 커밋해도 된다.

## 커밋 메시지 형식

아래 형식을 사용한다.

```text
종류: 짧고 명확한 변경 요약
```

| 종류 | 사용할 때 | 예시 |
| --- | --- | --- |
| `feat` | 새 기능 | `feat: 귀신 정보창 열기 추가` |
| `fix` | 오류·문제 해결 | `fix: GameScene Canvas 카메라 연결 복구` |
| `ui` | 화면 배치·디자인 | `ui: 상담 대화 카드 레이아웃 정리` |
| `art` | 배경·초상·사운드 등 에셋 | `art: 거울각시 연화 표정 5종 추가` |
| `docs` | 기획·규칙·기록 문서 | `docs: Git 커밋 규칙과 기록 추가` |
| `chore` | 설정·정리·기준점 저장 | `chore: 안정 상태 기준점 저장` |

## 작업 후 기록 작성 순서

1. Unity에서 Play 모드를 끄고 씬을 저장한다.
2. 변경된 파일을 확인한다.
3. 필요한 경우 컴파일 또는 Play 모드를 확인한다.
4. 이 문서의 **커밋 기록** 맨 위에 새 항목을 작성한다.
5. 기록 파일까지 포함해 커밋한다.
6. 커밋 해시와 메시지를 항목에 적는다.

## 커밋 기록 작성 양식

```md
### YYYY-MM-DD — 한글로 쓴 변경 제목

- 커밋: `짧은해시` — `커밋 메시지`
- 바뀐 점: 플레이어 또는 개발자가 실제로 보게 되는 변화를 한두 문장으로 설명한다.
- 주요 파일: `경로/파일명` — 왜 바뀌었는지 적는다.
- 확인: 컴파일 성공 / Play Mode에서 확인 / 아직 확인 필요 중 실제 상태만 적는다.
- 되돌릴 때: 필요할 때만, 어느 커밋으로 돌아가면 되는지 적는다.
```

## 커밋 기록

### 2026-08-03 — 중앙 안내 모달을 GameScene에 적용

- 커밋: `6151804` — `ui: 중앙 안내 모달을 GameScene에 적용`
- 바뀐 점: 중앙 안내·속마음 모달의 딤 레이어, 카드, 메시지, 확인 버튼과 `GhostInnerThoughtModal` 참조를 실제 GameScene에 배치했다.
- 주요 파일: `Assets/Scenes/GameScene.unity` — `Counselor UI > Root > Inner Thought Modal - Edit Here` 계층을 추가했다.
- 확인: 씬 저장 완료. Unity Play Mode의 실제 모달 전환 확인 필요.

### 2026-08-03 — 마지막 귀신 반응 중 장부 숨김

- 커밋: `f13f5fe` — `fix: 마지막 귀신 반응 중 장부 숨김`
- 바뀐 점: 귀신이 마지막 반응을 말하는 결과 카드에는 오늘의 장부를 표시하지 않는다. 보상 안내 뒤 밤 정산에서만 장부가 열린다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 결과 단계의 장부 표시 호출을 제거했다.
- 확인: `dotnet build Ghost.slnx --no-restore` 성공(경고 0, 오류 0). Unity Play Mode 확인 필요.

### 2026-08-03 — 상담 보상과 일일 안내 모달 전환

- 커밋: `af73abd` — `feat: 상담 보상과 일일 안내 모달 전환`
- 바뀐 점: 결과 카드의 이름표에는 귀신 이름만 표시한다. 귀신의 마지막 반응 뒤 `다음`을 누르면 복비 또는 특별 보상 물건을 중앙 모달로 알리고, 하루 시작·종료 문구도 같은 모달로 옮겼다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 결과 보상 단계, 한글 조사 처리, 일일 시작/종료 모달 전환을 추가했다.
- 확인: `dotnet build Ghost.slnx --no-restore` 성공(경고 0, 오류 0). Unity Play Mode에서 실제 카드·모달 순서 확인 필요.

### 2026-08-03 — 중앙 안내와 속마음 모달 추가

- 커밋: `4708594` — `feat: 중앙 안내와 속마음 모달 추가`
- 바뀐 점: 배경을 투명 회색으로 어둡게 하고 중앙 크림 카드, 안내/속마음 문구, 확인 버튼을 표시하는 재사용 모달을 추가했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostInnerThoughtModal.cs` — 모달 열기, 닫기, 확인 후 콜백 실행을 담당한다.
- 주요 파일: `Assets/GhostCounselor/Editor/InnerThoughtModalBuilder.cs` — `Ghost Counselor` 메뉴를 눌렀을 때만 GameScene에 편집 가능한 모달 계층을 생성한다.
- 확인: `dotnet build Ghost.slnx --no-restore` 성공(경고 0, 오류 0). Unity 메뉴 실행과 Game View 모양 확인 필요.

### 2026-08-03 — 장부 표시를 전용 코드로 분리

- 커밋: `c4ad932` — `refactor: 장부 표시를 전용 코드로 분리`
- 바뀐 점: 아침의 보유금·남은 영업일과 밤의 누적 현황을 대화 본문 대신 장부에 표시한다. 상담 결과 본문에서는 “장부에 기록됐다”라는 안내 문장을 제거하고 귀신 반응만 남겼다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostLedgerPresenter.cs` — 아침, 상담 결과, 밤 결산의 장부 문구와 표시를 전담한다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 게임 흐름과 귀신 대사만 유지하고 장부 표시를 호출하도록 정리했다.
- 확인: 코드 정적 검토 완료. Unity Play Mode에서 세 장부 화면의 실제 배치 확인 필요.

### 2026-08-03 — 귀신 4회 연속 상담 스토리 초안 추가

- 커밋: `4a9f4e2` — `docs: 귀신 4회 연속 상담 스토리 초안 추가`
- 바뀐 점: 기존 5명과 신규 2명에게 조언 실행, 피드백 재방문, 미련 해소, 성불로 이어지는 4회 상담 사건을 만들었다. 30일 완성판의 28회 상담과 29~30일 특수 사건도 함께 설계했다.
- 주요 파일: `Docs/Story/02-귀신-연속-상담-스토리.md` — 7명의 전체 연속 사건, 유산, 금기, 결산 구조를 작성했다.
- 주요 파일: `Docs/Story/00-게임-스토리.md`, `Docs/Story/README.md`, `Docs/프로젝트-핸드오프.md` — 기존 7일 프로토타입과 30일 확장판의 관계를 기록했다.
- 확인: 문서 검토 완료. Unity 콘텐츠 데이터와 실제 방문 로직 반영은 아직 하지 않았다.

### 2026-08-03 — 5초 공포 초상 유지와 얼굴 흔들림 수정

- 커밋: `62de382` — `fix: 5초 공포 초상 유지와 얼굴 흔들림 수정`
- 바뀐 점: 표정 순서가 비어 있는 귀신이 5초 공포 상태에서 사라지지 않고 기본 초상을 유지하게 했다. 흔들림도 투명한 초상 루트 대신 실제 얼굴 이미지에 적용한다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 공포 초상 선택의 숨김 처리와 흔들림 대상, 위치 초기화를 수정했다.
- 주요 파일: `Docs/프로젝트-핸드오프.md` — 현재 동작과 Unity Play Mode 확인 필요 사항을 기록했다.
- 확인: 코드 정적 확인 완료. 로컬 `dotnet build`는 Unity 임시 `Temp/obj` 파일이 없어 실행하지 못했다. Unity Play Mode 확인 필요.

### 2026-07-31 — 상담 결과 공책형 장부 패널 추가

- 커밋: `24a3acb` — `ui: 상담 결과 장부 패널 추가`
- 바뀐 점: 기본 사례비, 상담 보너스, 물건 환전, 오늘 수입을 대화 본문에서 분리해 상담 결과 때만 오른쪽 공책형 장부에 표시한다.
- 주요 파일: `Assets/Scenes/GameScene.unity` — `Ledger Notebook - Edit Position Here`와 제목·금액 목록·붉은 여백선을 추가했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 결과 장부 표시와 단계 전환 시 장부 숨김을 추가했다.
- 확인: C# 빌드 성공. Unity Play Mode에서 상담 결과 화면의 실제 배치는 확인 필요.

### 2026-07-31 — 제목 안내문을 네 페이지로 확장

- 커밋: `1699bd1` — `ui: 제목 안내문 네 페이지로 확장`
- 바뀐 점: 시작 안내문을 네 장으로 나누고 `다음 >`을 누를 때마다 다음 문단으로 넘기도록 했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 제목 문구 목록과 페이지 이동 처리를 추가했다.
- 확인: C# 빌드 성공. Unity Play Mode에서 실제 버튼 흐름은 확인 필요.

### 2026-07-31 — Face 텍스트 대체 제거

- 커밋: `423c77b` — `refactor: Face 텍스트 대체 제거`
- 바뀐 점: `Portrait Root`의 예비 텍스트 `Face`를 씬과 코드에서 완전히 제거했다. 초상 이미지가 없는 경우에는 `( ? )` 같은 글자 대신 초상 영역을 비워 둔다.
- 주요 파일: `Assets/Scenes/GameScene.unity` — `Face` 오브젝트와 연결 참조를 삭제했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 텍스트 얼굴 대체를 없애고 초상 숨김 처리로 변경했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostContentLibrary.cs`, `GhostGameModels.cs` — 더 이상 쓰지 않는 텍스트 표정 데이터를 제거했다.
- 확인: C# 빌드 성공. Unity에서 씬을 다시 불러온 뒤 Hierarchy와 Play Mode 확인이 필요하다.

### 2026-07-31 — 상담 보조 안내 텍스트 완전 제거

- 커밋: `9655698` — `feat: 상담 보조 안내 텍스트 제거`
- 바뀐 점: 딱지 할아버지의 상주형 안내와 단계별 보조 텍스트를 게임에서 완전히 제거했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs`, `GhostCounselorUIReferences.cs` — Helper UI 참조와 모든 안내 문구 출력을 제거했다.
- 주요 파일: `Assets/GhostCounselor/Editor/EditableCounselorUIBuilder.cs`, `DialogueCardStyleBuilder.cs` — 새 UI 및 수동 스타일 도구가 Helper를 만들거나 참조하지 않게 했다.
- 확인: Unity C# 재컴파일 성공.

### 2026-07-31 — 제목 안내문을 다음 화면으로 분리

- 커밋: `0540884` — `ui: 제목 안내문을 다음으로 분리`
- 바뀐 점: 제목 화면의 긴 안내문을 두 장으로 나눴다. 첫 화면에서 `다음 >`을 누르면 “하루 한 명의 귀신을 상담하고…” 문장이 표시되고, 그 뒤에 `새로 시작`과 `이어하기`가 나온다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 제목 화면의 페이지 상태와 `다음 >` 동작을 추가했다.
- 확인: C# 빌드 성공. Unity에서 Play 모드를 다시 시작한 뒤 버튼 배치는 확인 필요.

### 2026-07-31 — UI 자동 씬 변경 방지

- 커밋: `ded7ff5` — `fix: UI 자동 씬 변경 방지`
- 바뀐 점: UI 관련 Editor 도구가 컴파일이나 임포트만으로 씬 레이아웃을 자동 변경하지 않게 했다.
- 주요 파일: `Assets/GhostCounselor/Editor/DialogueCardStyleBuilder.cs`, `EditableCounselorUIBuilder.cs`, `FirstGuestPortraitSetupBuilder.cs` — 자동 UI 생성·재배치·초상 연결을 해제했다.
- 주요 파일: `Assets/GhostCounselor/Editor/PrologueUIBuilder.cs`, `SeparateSceneBuilder.cs` — 프롤로그·씬 분리 자동 갱신을 해제했다.
- 확인: Unity C# 재컴파일 성공. 현재 `GameScene`의 수동 레이아웃 변경은 유지했다.

### 2026-07-31 — 보조 안내 텍스트 없이 상담 실행

- 커밋: `365af75` — `fix: 보조 안내 텍스트 없이 상담 실행`
- 바뀐 점: `Helper` 안내 텍스트를 의도적으로 삭제해도 게임 시작이 중단되지 않고, 상담을 정상 진행할 수 있게 했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostCounselorUIReferences.cs` — Helper 참조를 필수 조건에서 제외했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — Helper가 없을 때 보조 안내 문구 출력을 안전하게 생략한다.
- 확인: Unity C# 재컴파일 성공. 실제 Play Mode 전체 상담 흐름은 확인 필요.

### 2026-07-31 — 5종 귀신 초상과 거울각시 표정 전환

- 커밋: `8c5f71f` — `feat: 귀신 초상과 거울각시 표정 전환 적용`
- 바뀐 점: 상담 손님 5명에게 각각 `Assets/Art/ghost`의 기본 초상을 연결했고, 거울각시는 질문 선택마다 표정이 진행되도록 했다.
- 주요 파일: `Assets/Scenes/GameScene.unity` — 귀신 ID별 초상 및 거울각시 표정 스프라이트를 연결했다.
- 주요 파일: `Assets/GhostCounselor/Scripts/GhostGameController.cs` — 현재 상담 중인 귀신의 초상을 표시하고, 거울각시 질문 선택 때 표정을 전환하도록 변경했다.
- 확인: 씬 직렬화 참조 확인. Unity 컴파일 및 Play Mode 전체 흐름은 확인 필요.

### 2026-07-31 — Git 커밋 규칙과 한글 기록 도입

- 커밋: `5858033` — `docs: Git 커밋 규칙과 한글 기록 추가`
- 바뀐 점: 앞으로 중요한 변경을 마칠 때마다 Git 커밋과 사람이 읽는 한글 변경 기록을 함께 남기도록 프로젝트 규칙을 만들었다.
- 주요 파일: `AGENTS.md` — 모든 후속 작업자가 커밋 규칙 문서를 먼저 읽고 따르도록 작업 순서에 추가했다.
- 주요 파일: `Docs/08-Git-커밋-규칙-및-기록.md` — 커밋 시점, 메시지 형식, 기록 양식을 만들었다.
- 확인: 문서 작성과 Git 커밋 완료. 다음 기능 작업부터 이 양식을 사용한다.

### 2026-07-31 — 복구된 GameScene UI를 첫 기준점으로 저장

- 커밋: `1f27adb` — `chore: save recovered GameScene UI baseline`
- 바뀐 점: 복구된 `GameScene` UI, 프롤로그·본편 분리, 신당 배경, 귀신 초상, 상담 프로토타입을 현재 기준점으로 보관했다.
- 주요 파일: `Assets/Scenes/GameScene.unity` — `Counselor UI`가 `Game Camera`를 사용하는 현재 씬 상태를 저장했다.
- 주요 파일: `.gitignore` — Unity가 자동으로 만드는 캐시·임시 폴더를 Git에서 제외했다.
- 확인: C# 빌드 성공. 사용자가 Unity에서 Canvas와 게임 화면 복구 상태를 확인했다.
- 되돌릴 때: 문제가 생기면 이 커밋 `1f27adb`를 안정 기준점으로 삼는다.
