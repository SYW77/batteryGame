# 개발 문서

> 프로젝트 소개는 `README.md`, 남은 작업은 `PLAN.md` 참고.

- Unity 6000.5.10f1 / URP / **Input System (New) 전용**
- 플레이 씬: `Assets/Scenes/Workshop.unity`

## 조작

| 입력 | 동작 |
| --- | --- |
| WASD / 방향키 | 이동 |
| Shift | 달리기 |
| Space | 점프 |
| 마우스 | 시점 |
| 좌클릭 | 집기 / 기기에 넣기 / 제출 / 버리기 |
| 우클릭 | 기기에서 마지막 건전지 빼기 |
| G | 손에 든 것 내려놓기 |
| Esc | 커서 잠금 해제 |

## 레벨 재생성

씬은 전부 코드로 만들어진다. 메뉴 **BatteryGame ▸ Build Level** 을 실행하면
`Assets/Editor/LevelBuilder.cs` 의 내용대로 씬을 통째로 다시 만들고 저장한다.

> 주의: 현재 열려 있는 씬의 루트 오브젝트를 **전부 지우고** 다시 만든다.
> 손으로 수정한 내용은 사라지므로, 배치를 바꿀 때는 씬이 아니라 `LevelBuilder.cs`를 고칠 것.

**`LevelBuilder.cs`만 고치고 Play를 누르면 아무것도 안 바뀐다.** 기기 목록, 건전지 수, 방 크기,
소품 위치는 전부 씬 파일에 구워져 있어서 반드시 메뉴를 다시 실행해야 한다.
`PlayerInteractor` 같은 런타임 스크립트만 스크립트 수정으로 바로 반영된다.
빌드하면 콘솔에 `기기 N종 | 건전지 M개` 가 찍히니 그 숫자로 확인할 것.

| 바꾸고 싶은 것 | 고칠 곳 |
| --- | --- |
| 전자기기 종류 / 요구 규격 / 개수 | `LevelBuilder.Devices` 배열 (작업대 3곳에 자동 분배·흩뿌림) |
| 작업대 위치 / 개수 | `LevelBuilder.DeviceStations` (기기를 크게 늘릴 땐 여기에 작업대를 추가) |
| 잘못 버렸을 때 페널티 개수 | `TrashCan`의 `penaltyCount` (기본 5) |
| 방전 건전지 수 (= 더미 부피 + 플레이 타임) | `LevelBuilder.DeadBatteryCount` |
| 방 크기, 책상 위치, 조명 | `LevelBuilder` 상단 상수 및 `BuildEnvironment` |
| 집는 거리 / 조준 보조 | `PlayerInteractor`의 `reach`, `aimAssistRadius` |
| 건전지 규격 치수 / 색상 | `Assets/Scripts/Core/BatterySizes.cs` |
| 건전지 모양 (프리팹) | `Assets/Editor/BatteryPrefabFactory.cs` |

## 게임 흐름

씬은 `Workshop.unity` 하나뿐이고, 메뉴는 그 위에 겹치는 `MenuCanvas`다. `GameFlow`가 상태를 관리한다.

```
Title ──[새 게임]──▶ Playing ──(남은 건전지 0)──▶ 제출대 앞 클리어 버튼 활성화
  ▲   └─[이어하기]──▶                                      │
  │                                                   [버튼 누름]
  └──────────[처음으로]──────────────────────────── Cleared (소요 시간 표시)
```

- **세이브**: `Application.persistentDataPath/battery_save.json`. 20초마다 자동 저장되고,
  처음으로 / 게임 종료 / 앱 종료 시에도 저장한다. 클리어하면 삭제된다.
  세이브가 없으면 타이틀에서 이어하기 버튼이 숨겨진다.
- **저장 내용**: 건전지별 규격·잔량·검사 여부·위치·꽂힌 기기 인덱스, 기기별 제출 여부,
  진행도 집계, 플레이어 위치와 시점, 경과 시간.
- **기기 인덱스**는 계층의 sibling index 순서다(`GameManager.CollectDevices`).
  `FindObjectsByType`은 순서를 보장하지 않아서 정렬해 두었다. **기기 배열 순서를 바꾸면 기존 세이브가 어긋난다.**
- 들고 있던 물건은 그 자리에 놓인 것으로 저장된다. 기기를 들고 있었다면 책상 위 제자리로 돌아간다.

## 게임 규칙

- 건전지 더미는 **기기들이 요구하는 정상 건전지 + 방전 건전지**로만 구성된다.
  → 잉여 정상 건전지가 없으므로 항상 정확히 "남은 건전지 0"으로 끝낼 수 있다.
  현재: 기기 36종, 정상 81개 + 방전 60개 = **141개**.
- **페널티**: 잔량이 남은 건전지를 수거함에 넣으려 하면 폐건전지 5개가 쏟아진다(`TrashCan.penaltyCount`).
  이때 **건전지는 손에 그대로 남는다.** 없애버리면 그 규격을 쓰는 기기를 영영 켤 수 없어
  클리어가 불가능해지기 때문이다. 페널티로 늘어난 만큼 `TotalBatteries`도 함께 올라간다.
- 잔량은 **집어야만** 보인다. 한 번 집은 건전지에는 초록(정상)/빨강(방전) 띠가 생겨
  다시 집지 않아도 구분할 수 있다.
- 규격이 다르면 삽입 거부. 방전 건전지는 삽입은 되지만 기기가 켜지지 않는다(우클릭으로 회수).
- 잔량이 남은 건전지는 수거함에 버릴 수 없다. (진행 불가 상태 방지)
- 모든 기기 제출 + 모든 방전 건전지 폐기 → 클리어.

## 구조

```
Assets/Scripts/
  Core/    BatterySizes(규격 테이블) · Prim(프리미티브 헬퍼) · Interactable · Carryable
  Items/   Battery · Device
  World/   TrashCan · SubmitCounter
  Player/  FirstPersonController · PlayerInteractor
  Game/    GameManager (더미 생성 + 진행도 집계)
  UI/      HUD · UIFontApplier · Billboard
Assets/Editor/
  LevelBuilder · BatteryPrefabFactory · MatLib
```

- `Interactable` : 조준 가능한 모든 것. `Interact(player)` = 좌클릭, `AltInteract(player)` = 우클릭.
- `Carryable : Interactable` : 손에 들 수 있는 것(건전지, 기기). 들면 물리/콜라이더를 끄고
  레이어를 Ignore Raycast로 바꿔 시선이 통과하게 한다.
- `GameManager`가 기기 목록을 읽어 필요한 건전지를 역산해 더미를 생성한다.
- `HUD`는 `PlayerInteractor`를 폴링하고 `GameManager` 이벤트를 구독한다.

## 함정 모음 (다시 밟지 않기 위한 기록)

- **한글 폰트**: 유니티 내장 레거시 폰트에 한글 글리프가 없어서, `UIFontApplier`가 런타임에
  OS 폰트(맑은 고딕 등)로 교체한다. 에디터 씬 뷰에서는 네모로 보일 수 있지만 플레이하면 정상이다.
  Windows 외 플랫폼으로 빌드하려면 TMP + 한글 폰트 에셋으로 교체해야 한다.

- **천장 조명**: 포인트 라이트를 천장에 붙이면 천장이 하얗게 탄다. 아래를 향한 스포트라이트를 쓴다.

- **에디터 포커스**: Run In Background를 켜 두었다. 꺼져 있으면 에디터가 포커스를 잃는 순간
  플레이 모드가 멈춰서 "플레이어가 공중에 떠 있는" 것처럼 보인다.

- **도메인 리로드 비활성화 (MCP 연결 끊김 대책)**: Project Settings ▸ Editor ▸
  Enter Play Mode Settings에서 `Enter Play Mode Options` 켜고 `Reload Domain`을 껐다.
  Play/Stop 할 때마다 도메인 리로드가 일어나면 MCP 브리지가 끊기고,
  180초 안에 재접속하지 못하면 릴레이가 자동 종료돼 그 세션에서 MCP를 못 쓰게 된다.
  **대가**: static 필드가 플레이 사이에 초기화되지 않는다. 그래서 `GameManager`와 `HUD`의
  싱글턴에 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)] ResetStatics()`를 넣어 두었다.
  **앞으로 static 필드나 static 이벤트를 추가하면 반드시 여기에도 초기화를 추가할 것.**
  안 그러면 두 번째 플레이부터 이전 플레이의 값이 남아 이상하게 동작한다.

- **조준 판정은 2단계**: `PlayerInteractor.FindFocus`가 먼저 정밀 레이캐스트를 쏘고,
  빗나가면 굵은 스피어캐스트로 한 번 더 잡는다. 스피어캐스트는 **카메라보다 뒤에서** 시작하는데,
  PhysX가 스윕 시작 지점에 겹친 콜라이더를 무시하기 때문이다.
  (수거함에 바짝 붙으면 오히려 인식이 안 되던 원인이 이것)
  부작용: 벽 너머의 물체를 집을 수 있는 이론적 여지가 있다. 지금 레벨 구조에서는 문제가 안 된다.

- **더미가 쌓이는 원리**: 담는 통이 없어서, 매끄러운 원통은 그냥 두면 한 겹으로 퍼진다.
  세 가지가 같이 있어야 더미 모양이 나온다.
  1. `GameManager.SpawnPileRoutine`이 **한 층씩 나눠 떨어뜨린다**(`pileLayerDelay`).
     한 번에 쏟으면 탑이 무너져 퍼진다. 낙하 높이는 `PileTop()`으로 실제 더미 높이를 재서 따라간다
     (고정 증가값을 쓰면 층이 많아질수록 높은 데서 떨어져 튕겨 흩어진다).
  2. `Battery`가 런타임에 고마찰 `PhysicsMaterial`을 콜라이더에 붙인다.
     프리팹 rigidbody의 `angularDamping = 4`도 구르는 걸 막는다.
  3. **개수가 충분해야 한다.** 실측: 45개는 한 겹(높이 0.08m)으로 퍼지고,
     107개가 되어야 높이 0.13m의 더미가 된다.
  `pileAreaSize`를 상판보다 크게 잡으면 바닥으로 쏟아지니 주의.
