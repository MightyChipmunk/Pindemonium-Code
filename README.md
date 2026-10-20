# PINDEMONIUM

> 핀볼에 로그라이크 덱빌딩을 얹은 2D 핀볼 게임. Unity 6 / C#으로 1인 개발했습니다.

[Steam 스토어 페이지](https://store.steampowered.com/app/5343970/)

<!-- TODO: 게임플레이 GIF 또는 스크린샷 1~2장 -->

---

## 이 리포에 대해

출시한 게임의 **게임플레이 코드만** 추린 퍼블릭 리포입니다.

- `Assets/Pinball/Scripts`의 C# 약 190개 파일(약 2.7만 줄)과 셰이더가 들어 있고, 커밋 히스토리도 함께 남겨 두었습니다.
- 아트, 사운드, 씬, 프리팹, 데이터 에셋은 넣지 않았습니다.
- 여러 프로젝트에서 같이 쓰는 개인 공용 라이브러리 `GameCore`(UI, 세이브, 로컬라이제이션, 오디오, 풀링, Steam 래퍼)도 넣지 않았습니다. 그래서 **이 리포만으로는 컴파일되지 않습니다.** 코드는 GameCore의 제네릭 베이스를 게임 쪽에서 닫아 쓰는 얇은 파사드 형태로만 등장합니다(예: [`UIViewBase`](Assets/Pinball/Scripts/UI/UIViewBase.cs)).

## 기술 스택

| 분야 | 사용 기술 |
|---|---|
| 엔진 | Unity 6 (6000.3), URP 2D |
| 언어 | C# 9 (record, init 전용 프로퍼티) |
| 비동기 | UniTask (코루틴 미사용) |
| 트윈 | DOTween |
| 입력 | Input System (키 리바인딩) |
| 플랫폼 | Steamworks.NET (도전과제, Steam Cloud 세이브) |
| 에디터 확장 | Alchemy, 커스텀 인스펙터와 에디터 윈도우 |

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

## 주요 구현

| 시스템 | 내용 |
|---|---|
| **플리퍼 타격 판정** [`Flipper.cs`](Assets/Pinball/Scripts/Ingame/Flipper/Flipper.cs) | 볼이 맞은 팔 위의 위치와 버튼 입력 타이밍을 합쳐 Normal / SweetSpot / Perfect 등급을 매깁니다. 스윗스팟과 퍼펙트 타이밍 창에서 멀어질수록 충격량이 부드럽게 줄어들어, 손맛과 실력 차이가 같이 드러납니다. |
| **플런저** [`Spring.cs`](Assets/Pinball/Scripts/Ingame/Spring/Spring.cs) | 충전량이 정점을 찍은 뒤 감쇠하고, 너무 오래 누르면 자동으로 취소됩니다. 발사 충격량은 즉시분과 여러 FixedUpdate에 나눠 주는 분량으로 쪼개고, 스쿼시와 흔들림 연출은 DOTween으로 붙였습니다. |
| **세이커 / 레인** [`SaucerInteractable.cs`](Assets/Pinball/Scripts/Ingame/Map/SaucerInteractable.cs), [`LaneInteractable.cs`](Assets/Pinball/Scripts/Ingame/Map/LaneInteractable.cs) | 볼을 붙잡아 룰렛 연출을 한 뒤 사출하거나, 경로를 따라 운반합니다. 그동안 볼을 Dynamic과 Kinematic 사이에서 전환하고, 시퀀스 ID로 낡은 비동기 작업을 안전하게 버립니다. |
| **맵 기믹** [`Ingame/Map`](Assets/Pinball/Scripts/Ingame/Map) | 범퍼(체력·화상·빙결·표식·재생성), 드롭 타겟 뱅크, 램프 뱅크, 스피너, 슬링샷이 공통 `MapInteractable`과 `IDamageable`을 구현합니다. 광역 폭발과 연쇄 번개는 [`MapRoot`](Assets/Pinball/Scripts/Ingame/Map/MapRoot.cs)가 맡습니다. |
| **점수·콤보·데미지** [`Ingame/Score`](Assets/Pinball/Scripts/Ingame/Score), [`DamageCalculator.cs`](Assets/Pinball/Scripts/Ingame/Stat/DamageCalculator.cs) | 충격량 × 공격력 × 배율 구조로 데미지를 계산하고, 점수 출처(직접 타격 / 패시브 / 파괴)에 따라 콤보 적립 여부가 갈립니다. 큰 수 오버플로는 [`ScoreMath`](Assets/Pinball/Scripts/Util/ScoreMath.cs)로 막습니다. |
| **히트스톱** [`HitStopPlayer.cs`](Assets/Pinball/Scripts/Ingame/Presentation/HitStopPlayer.cs) | UniTask로 `timeScale`을 조절하고 이징 곡선을 따라 복귀합니다. 일시정지나 조준 상태와 겹쳐도 취소와 복원이 꼬이지 않습니다. |
| **튜토리얼 딤** [`TutorialDimmer.cs`](Assets/Pinball/Scripts/Ingame/Tutorial/TutorialDimmer.cs) | `MaskableGraphic`을 상속해 오버레이 메시를 세로 띠로 나누고 구멍 구간을 병합해 직접 생성합니다. 여러 하이라이트 구멍을 스텐실이나 마스크 없이 메시 한 장으로 그립니다. |
| **데이터 기반 튜토리얼** [`Ingame/Tutorial`](Assets/Pinball/Scripts/Ingame/Tutorial) | 블록 단위 트리거, 비동기 대화 진행, 시간 정지와 입력 잠금을 지원합니다. 스텝 편집용 커스텀 인스펙터도 함께 만들었습니다. |
| **도전과제** [`AchievementManager.cs`](Assets/Pinball/Scripts/Steam/AchievementManager.cs) | 한 프레임에 여러 해금이 일어나도 다음 프레임에 `Store()`를 한 번만 호출합니다. 시작할 때는 세이브를 보고 이미 달성한 과제를 소급 해금합니다. |
| **치트 툴** [`Ingame/Cheat`](Assets/Pinball/Scripts/Ingame/Cheat) | 에디터 윈도우에서 돈, 클리어, 아이템, 튜토리얼, 도전과제를 조작합니다. 릴리스 빌드에서는 씬 처리 단계에서 치트 오브젝트를 제거합니다. |

## 코딩 규칙

프로젝트 전체에 아래 규칙을 정해 두고 지켰습니다.

- **의존성**
  - 필드는 자기 GameObject 계층 안만 참조합니다.
  - 계층 밖의 의존성은 주입하거나 델리게이트로 받습니다.
  - 순환 참조는 금지합니다.
- **파라미터 레코드는 데이터만**
  - `InitParam`류 레코드에는 값과 읽기 전용 설정만 넣습니다.
  - 다른 클래스의 기능은 콜백으로 받고, 기능이 많은 클래스는 레코드 밖의 별도 인자로 넘깁니다.
- **생명주기**
  - MonoBehaviour와 static 클래스는 `Init`/`Release`를 쌍으로 둡니다.
  - 순수 C# 클래스는 생성자로 초기화하고 `IDisposable`로 해제합니다.
  - Unity 생명주기 메서드와 `Init`/`Release`는 한 클래스에 섞지 않습니다.
- **런타임 성능**
  - 런타임에 `GetComponent`, `Find` 계열, `AddComponent`를 쓰지 않습니다.
  - 길이 비교에는 `sqrMagnitude`를 씁니다.
  - 비동기는 UniTask로 처리하고 `CancellationToken`과 연동합니다.
- **API 노출**
  - 참조 타입은 public 프로퍼티로 노출하지 않습니다.
  - 노출이 필요하면 메서드로 열어서 의도적인 결정임을 드러냅니다.

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
