# PINDEMONIUM

> 핀볼에 로그라이크 덱빌딩을 얹은 2D 핀볼 게임. Unity 6 / C#으로 1인 개발했습니다.

[Steam 스토어 페이지](https://store.steampowered.com/app/5343970/)

<!-- TODO: 게임플레이 GIF 또는 스크린샷 1~2장 -->

---

## 이 리포에 대해

출시한 게임의 **게임플레이 코드만** 추린 퍼블릭 리포입니다.

- `Assets/Pinball/Scripts`의 C# 약 190개 파일(약 2.7만 줄)과 셰이더가 들어 있고, 커밋 히스토리도 함께 남겨 두었습니다.
- 아트, 사운드, 씬, 프리팹, 데이터 에셋은 넣지 않았습니다.
- 여러 프로젝트에서 같이 쓰는 공용 라이브러리 서브모듈(UI, 세이브, 로컬라이제이션, 오디오, 풀링, Steam 래퍼)도 넣지 않았습니다. 그래서 **이 리포만으로는 컴파일되지 않습니다.** 코드는 서브모듈의 제네릭 베이스를 게임 쪽에서 닫아 쓰는 얇은 파사드 형태로만 등장합니다(예: [`UIViewBase`](Assets/Pinball/Scripts/UI/UIViewBase.cs)).

## 게임 구조

```
로비 ─▶ 플리퍼 선택 ─▶ ┌─ 스테이지 선택 (기믹 리롤)
                       │      ▼
                       │   플레이 (목표 점수 달성 / 볼 소진·시간 초과 시 게임 오버)
                       │      ▼
                       └─ 상점 (패시브·업그레이드·카드팩·바우처)
                              ▼  3스테이지 = 1라운드
                           결과 ─▶ 무한 모드 (선택)
```

- 한 런은 여러 라운드로 이루어지고, 각 라운드는 3스테이지입니다. 라운드 마지막 스테이지를 깨면 보상 카드팩을 받습니다.
- 스테이지마다 목표 점수 배율, 시간 제한, 기믹 제거 같은 **스테이지 기믹**이 붙고, 돈을 내고 리롤할 수 있습니다.
- 상점에서는 볼·맵·공용 슬롯에 장착하는 **패시브**(약 54종 효과), 중첩되는 **업그레이드**, 런 전체에 적용되는 **바우처**를 삽니다.
- 진행 상황은 스테이지 시작, 상점 변경, 클리어 때마다 저장되고 이어하기를 지원합니다. 저장 대상에는 상점 진열과 개봉 중인 카드팩까지 들어갑니다.

## 아키텍처

### 컴포지션 루트와 해제 스택
[`IngameManager`](Assets/Pinball/Scripts/Ingame/IngameManager.cs)가 인게임의 유일한 조립 지점입니다.

- 하위 시스템은 `InitParam` 레코드(값과 읽기 전용 설정 데이터)와 델리게이트로 연결합니다. 서로 직접 참조하지 않아서 순환 참조가 생기지 않습니다.
- `Init`할 때마다 대응하는 `Release`를 해제 스택에 쌓고, 파괴될 때 역순으로 실행합니다.

### 상태 머신
[`IngameStateMachine`](Assets/Pinball/Scripts/Ingame/State/IngameStateMachine.cs)은 StageSelect → Play → Shop → Result 네 상태를 돌립니다.

- 각 상태는 머신을 참조하지 않고, `Bind` 때 받은 `ChangeState` 델리게이트로만 전이합니다([`IngameStateBase`](Assets/Pinball/Scripts/Ingame/State/IngameStateBase.cs)).

### 읽기 전용 스탯 조회
[`StatProvider`](Assets/Pinball/Scripts/Ingame/Stat/StatProvider.cs)는 런 전역 스탯(장착 패시브, 업그레이드, 플리퍼 튜닝)을 static으로 조회합니다.

- 쓰기는 허용하지 않습니다. 지갑·로드아웃처럼 쓰기가 있는 상태는 static으로 두지 않고 명시적으로 주입합니다.

### 세이브
[`SaveManager`](Assets/Pinball/Scripts/Save/SaveManager.cs)는 Steam Cloud와 로컬 파일 두 곳에 동시에 기록하고, 읽을 때는 더 최신 쪽을 고릅니다.

- 데이터 클래스는 버전을 붙여 마이그레이션합니다.

## 폴더 구성

```
Assets/Pinball/
├─ Scripts/
│  ├─ Core/          부트스트랩, 씬 전환
│  ├─ Lobby/         로비, 런 설정
│  ├─ Ingame/
│  │  ├─ State/      인게임 상태 머신
│  │  ├─ Ball/       볼 흐름, 분신, 패시브 투사체
│  │  ├─ Flipper/  Spring/  Map/   핀볼 물리 오브젝트와 기믹
│  │  ├─ Score/  Stat/             점수, 콤보, 데미지, 스탯
│  │  ├─ Shop/  Reward/  Stage/    로그라이크 메타
│  │  ├─ Tutorial/  Presentation/  튜토리얼, 연출
│  │  └─ Cheat/                    개발용 치트
│  ├─ UI/  Save/  Settings/  Localization/  Audio/  FX/  Steam/
│  └─ Util/
└─ Shaders/          튜토리얼 하이라이트 셰이더
```

## 라이선스

열람 목적으로 공개합니다. 코드의 복제, 수정, 재배포를 허락하지 않습니다. (All rights reserved)
