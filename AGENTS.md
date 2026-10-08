# RPGBoilerplate

Unity 6 / 2D / Input System 패키지 전용 프로젝트.
"코딩 없이 게임 만들기" 스터디의 4주차 보일러플레이트다.

`Assets/_Core/`에는 이미 동작하는 시스템들이 들어 있고, 참가자와 에이전트는 `Assets/_Game/`에서 작업한다.
**이 문서의 목적은 `_Core`에 무엇이 있는지 알려서 같은 것을 다시 만들지 않게 하는 것이다.**

## 이 게임

**횡스크롤 액션 RPG**(메이플스토리 계열). 최소 완성 조건은 하나다.

> 몬스터를 잡아 레벨이 오르고, 보스를 잡으면 끝난다.

**그 위는 전부 사용자가 정한다.** 스킬트리·인벤토리·장비 강화·퀘스트 같은 것을 넣을지,
레벨을 몇까지 올릴지, 스탯을 어떻게 굴릴지는 정해져 있지 않다.
**임의로 정하지 말고 사용자에게 물어본다.** 물어보지 않고 크게 만들면 최소 완성 조건이 먼저 무너진다.

## 폴더 구조

```text
AGENTS.md          ← 이 파일. 지시 파일 단일 원본
CLAUDE.md          ← "@AGENTS.md" 한 줄
Assets/
├── _Core/
│   ├── Common/    ← 5주 내내 동일한 공용 시스템
│   │   ├── GameManager.cs
│   │   ├── ScoreManager.cs
│   │   ├── SoundManager.cs
│   │   ├── UIManager.cs
│   │   └── ObjectPool.cs
│   └── RPG/       ← 이번 게임 전용
│       ├── IDamageable.cs
│       ├── CombatEvents.cs
│       ├── SpriteAnimator.cs
│       └── CameraFollow2D.cs
├── _Game/         ← 비어 있다. 새 스크립트·프리팹·에셋은 전부 여기
├── Scenes/        ← RPG.unity (매니저와 UI만 들어 있다. 게임 오브젝트는 없다)
├── Sprites/
│   ├── Character/ ← Swordsman, Archer
│   ├── Enemy/     ← Normal(일반 8종), Boss(2종)
│   ├── Tiles/     ← 타일셋·배경
│   ├── Skills/    ← 스킬 이펙트
│   └── 16x16 Assorted RPG Icons/
└── Audio/         ← 비어 있다
```

**`_Core/RPG/`에 있는 것 = 이 게임에서 쓰라는 것.** 다른 게임용 시스템은 애초에 들어 있지 않다.

## 존재하는 시스템 — 새로 만들지 말 것

| 시스템 | 역할 |
| --- | --- |
| `GameManager` | 게임 상태(Ready/Playing/Paused/GameOver)의 단일 출처. 상태 전환과 알림 |
| `ScoreManager` | 점수·최고점수. 저장까지 포함 |
| `SoundManager` | 효과음/BGM 재생, 볼륨 |
| `UIManager` | 패널 전환·점수 표시. 상태와 점수를 구독해서 **자동으로** 갱신 |
| `ObjectPool` | 오브젝트 재사용. 싱글톤이 아니라 프리팹 종류마다 하나씩 씬에 배치 |
| `IDamageable` | 피해를 받을 수 있는 것의 공통 계약 |
| `CombatEvents` | 피격·사망을 알리는 전역 채널. 매니저가 아니다 |
| `SpriteAnimator` | 스프라이트 프레임 애니메이션과 동작 전환 |
| `CameraFollow2D` | 카메라 추적과 맵 경계 |

"사운드 시스템 만들어줘", "체력 시스템 붙여줘" 같은 요청을 받아도 **새로 만들지 말고 위 시스템을 호출한다.**
필요한 기능이 위에 없다고 판단되면 그때는 만들기 전에 사용자에게 먼저 확인한다.

**몬스터를 여러 마리 스폰하거나 화살·투사체를 쏜다면 `ObjectPool`을 쓴다.** `Instantiate`/`Destroy`를 반복하지 않는다.

## 스탯은 매니저로 만들지 않는다

**`DataManager` 같은 전역 스탯 매니저를 만들지 않는다.** 몬스터 열 마리의 체력을 싱글톤 하나가 들고 있으면
한 마리가 맞을 때 전부 같이 깎인다. **정의와 상태를 나눈다.**

- **정의**(종류별 기본값 — 최대 체력, 공격력, 이동 속도, 주는 경험치)는 **`ScriptableObject` 에셋**에 둔다.
  클래스는 `_Game`에 만들고 **`[CreateAssetMenu]`를 반드시 붙인다.** 안 붙이면 사용자가 에셋을 만들 방법이 없다.
- **상태**(지금 남은 체력, 현재 레벨, 인벤토리 내용)는 그 오브젝트에 붙은 `MonoBehaviour`가 들고 있다.

```csharp
// _Game/EnemyStatsData.cs — 정의. Mushroom.asset, Boss_Golem.asset 처럼 종류마다 에셋 하나
[CreateAssetMenu(menuName = "RPG/Enemy Stats")]
public class EnemyStatsData : ScriptableObject
{
    public int maxHP = 50;
    public int contactDamage = 10;
    public int expReward = 10;
}

// _Game/Enemy.cs — 상태. 씬에 있는 몬스터 열 마리가 각자 CurrentHP를 갖는다
public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyStatsData data;
    public int CurrentHP { get; private set; }
}
```

밸런스를 고칠 때 코드를 고치지 않고 **에셋 파일의 숫자만 바꾸면 되는 것**이 목적이다.
"몇 대에 죽는가"는 공격력 에셋과 체력 에셋의 관계로만 정해져야 한다 — 코드에 그 횟수가 적혀 있으면 안 된다.

## 호출 규약

아래 공개 시그니처는 고정이다. **이름과 인자 형태를 바꾸지 않는다.** 내부 구현은 자유다.

```csharp
// ── GameManager : 상태의 단일 출처
public enum GameState { Ready, Playing, Paused, GameOver }
public static GameManager Instance { get; }
public GameState State { get; }
public bool IsPlaying { get; }          // State == Playing 축약
public void StartGame();
public void TriggerGameOver();          // 멱등. 이미 GameOver면 즉시 반환
public void PauseGame();                // Time.timeScale = 0
public void ResumeGame();
public void RestartGame();
public event Action<GameState> OnStateChanged;

// ── ScoreManager
public static ScoreManager Instance { get; }
public int Score { get; }
public int BestScore { get; }           // PlayerPrefs 키에 Application.productName 포함
public void AddScore(int amount = 1);
public void SetScore(int value);
public void ResetScore();
public event Action<int> OnScoreChanged;

// ── SoundManager
public static SoundManager Instance { get; }
public void PlaySFX(AudioClip clip, float volume = 1f);
public void PlayBGM(AudioClip clip, bool loop = true);
public void StopBGM();
public void SetSFXVolume(float value);
public void SetBGMVolume(float value);

// ── UIManager : 패널 전환은 OnStateChanged 구독으로 자동. 거의 부를 일이 없다
public static UIManager Instance { get; }
public void ShowMessage(string text, float duration = 1.5f);

// ── ObjectPool : 싱글톤 아님. [SerializeField]로 참조해서 쓴다
public GameObject Get();
public GameObject Get(Vector3 position, Quaternion rotation);
public void Release(GameObject obj);    // 중복 반납은 내부에서 방어됨
public void ReleaseAll();

// ── IDamageable : 맞는 쪽이 구현한다. 때리는 쪽은 이것만 본다
public interface IDamageable
{
    void TakeDamage(int amount);
}

// ── CombatEvents : static. 인스턴스가 없다
public static event Action<GameObject, int> OnDamaged;   // (맞은 대상, 깎인 양)
public static event Action<GameObject> OnDied;           // (죽은 대상)
public static void RaiseDamaged(GameObject target, int amount);
public static void RaiseDied(GameObject target);

// ── SpriteAnimator : 싱글톤 아님. 캐릭터마다 하나씩 붙인다
public enum AnimState { Idle, Walk, Jump, Attack, Hit, Death, Skill, Block }
// Skill·Block은 Attack과 같은 우선순위. Block은 반복 동작이라 _Game이 유지 시간 동안 계속 Play한다.
public AnimState Current { get; }
public bool IsBusy { get; }              // 반복하지 않는 동작이 재생 중
public bool IsDeadLocked { get; }        // Death를 끝내고 잠긴 상태
public void Play(AnimState state, bool force = false);
// Death가 끝나면 마지막 프레임에서 잠긴다. 그 뒤로는 force: true만 통한다.
// 풀에서 몬스터를 다시 꺼낼 때 Play(AnimState.Idle, force: true)로 되살린다.
public void SetFacing(float directionX); // 이동 입력값을 그대로 넘긴다. 0이면 방향 유지
public event Action<AnimState> OnClipFinished;

// ── CameraFollow2D : 카메라에 붙인다
public Transform Target { get; }
public bool UseBounds { get; set; }
public void SetTarget(Transform newTarget);
public void SnapToTarget();              // 즉시 이동. 리스폰·순간이동
public void SetBounds(Vector2 min, Vector2 max);
```

전형적인 사용 형태:

```csharp
[SerializeField] private SpriteAnimator animator;

void Update()
{
    if (!GameManager.Instance.IsPlaying) return;   // 게임오버 후에도 조작되는 것을 막는다

    float moveX = ReadMoveInput();
    animator.SetFacing(moveX);
    animator.Play(moveX != 0f ? AnimState.Walk : AnimState.Idle);   // 매 프레임 불러도 된다

    if (attackPressed) animator.Play(AnimState.Attack);             // 끝까지 재생되고 저절로 Idle로 돌아온다
}

// 때리는 쪽 — 구체 타입을 찾지 않는다
if (hit.TryGetComponent<IDamageable>(out var target)) target.TakeDamage(damage);

// 맞는 쪽
public void TakeDamage(int amount)
{
    if (currentHP <= 0) return;            // 죽은 뒤 또 맞으면 경험치가 여러 번 들어간다

    currentHP -= amount;
    CombatEvents.RaiseDamaged(gameObject, amount);
    animator.Play(AnimState.Hit);

    if (currentHP <= 0)
    {
        animator.Play(AnimState.Death);
        CombatEvents.RaiseDied(gameObject);
    }
}

// 반응하는 쪽 — 몬스터를 참조하지 않는다
void OnEnable()  { CombatEvents.OnDied += HandleDied; }
void OnDisable() { CombatEvents.OnDied -= HandleDied; }   // 빠뜨리면 재시작 후 두 번 세어진다

// 플레이어가 죽었을 때
GameManager.Instance.TriggerGameOver();    // 두 번 불려도 안전하다
```

### 이 계약에서 지켜야 할 것

1. **`_Core`는 `_Game`을 참조하지 않는다.** `_Game` → `_Core`는 직접 호출, 반대 방향은 이벤트로만.
2. **`ObjectPool`은 `GameObject` 단위로만 다룬다.** `Rigidbody2D` 같은 물리 타입을 시그니처에 등장시키지 않는다
   (3D 주차에서 같은 파일을 그대로 쓰기 위해서다).
3. **풀 반납 조건은 `_Game` 책임이다.** `_Core`가 정하려 들지 않는다.
4. **`TriggerGameOver()`는 `Time.timeScale`을 건드리지 않는다.** `timeScale` 조작은 `PauseGame()`에서만 한다.
5. **`PlaySFX`는 문자열 키가 아니라 `AudioClip`을 받는다.** 문자열 키 방식으로 바꾸지 않는다.
6. **`IDamageable`에 체력을 넣지 않는다.** 인터페이스는 `TakeDamage(int)` 하나로 유지한다.
   체력·방어력·경험치는 각 구현이 알아서 들고 있는다.
7. **`SpriteAnimator`는 게임 규칙을 모른다.** 공격 판정·무적 시간·넉백은 전부 `_Game`이 정한다.
   공격 판정 타이밍이 필요하면 `OnClipFinished`를 구독하거나 `_Game`에서 코루틴으로 잰다.
8. **`CameraFollow2D`는 누구를 따라갈지 스스로 정하지 않는다.** `SetTarget()`으로 `_Game`이 넣어준다.
9. **풀에서 재사용하는 몬스터는 `OnEnable`에서 초기화한다.** 풀은 오브젝트를 껐다 켤 뿐이라
   두 번째로 꺼낼 때 `Awake`가 다시 돌지 않는다. 체력을 `Awake`에서 채우면 **부활한 몬스터의 체력이 0이다.**
   애니메이션도 죽은 프레임에 잠겨 있으므로 `Play(AnimState.Idle, force: true)`로 같이 풀어준다.

## 작업 규칙

- **새 스크립트는 `Assets/_Game/`에만 만든다.**
- **`_Core`를 수정해야 한다고 판단되면 수정하지 말고 먼저 설명한다.**
  어떤 파일을 왜 어떻게 바꿔야 하는지 사용자에게 말하고, 승인을 받은 뒤에만 수정한다.
  `_Game`에 우회 코드를 만들어 넘어가는 것보다 이쪽이 낫다.
- **공개 시그니처는 변경하지 않는다.** 내부 구현은 바꿔도 되지만 이름과 인자 형태는 유지한다.
- **입력은 `Keyboard.current`를 쓴다.** 이 프로젝트는 Input System 패키지 전용 모드라
  레거시 `Input.GetKey`는 컴파일은 되고 **실행 시 예외가 난다.**
- **`GameManager`가 Space·Esc·R을 이미 쓰고 있다.** Ready에서 Space는 시작, Playing/Paused에서 Esc는
  일시정지·해제, GameOver에서 R은 재시작이다. `_Game`에서 같은 키를 쓰면 겹친다.
  특히 **Space를 점프로 쓰면 Ready에서 시작하는 그 프레임에 같이 점프한다** — `GameManager`의
  실행 순서가 앞이라 같은 프레임에 이미 Playing이 되어 있기 때문이다.
  `OnStateChanged`로 상태가 바뀐 프레임을 기억해 두고 그 프레임의 입력을 무시하면 된다.
  `GameManager`의 `startOnSpace`를 끄면 Ready에서 Space로 시작하지 않는다. 이 프로젝트는 꺼 두고
  `_Game`의 `TitleIntro`가 제목 → 캐릭터 선택 → 조작법을 Space로 넘긴 뒤 `StartGame()`을 부른다.
- **`StartGame()`이 `ScoreManager.ResetScore()`를 대신 불러준다.** `_Game`에서 점수를 따로 0으로 만들 필요가 없다.
- **가만히 서 있는 플레이어는 `OnTriggerStay2D`를 받지 못한다.** `Rigidbody2D`가 잠들면(`IsSleeping`)
  물리 콜백이 멈춰서, **몬스터 위에 서 있어도 피해를 안 받는 상태**가 된다. 콜라이더도 레이어도 멀쩡한데
  아무 일이 안 일어나서 원인을 찾기 어렵다. 플레이어의 `Rigidbody2D`는 `Sleeping Mode`를
  **`Never Sleep`** 으로 둔다.
- **`SpriteRenderer.sprite`를 직접 갈아끼우는 애니메이션 코드를 새로 만들지 않는다.** `SpriteAnimator`를 쓴다.
  유니티 표준 Animator Controller를 쓰고 싶다는 요청을 받으면 그렇게 해도 된다.
  다만 **둘을 같은 오브젝트에 같이 붙이지 않는다** — 서로 스프라이트를 덮어써서 깜빡인다.
- **카메라를 `Update()`에서 옮기지 않는다.** `CameraFollow2D`를 쓴다. `Update()`에서 옮기면 캐릭터가 미세하게 떤다.
- **`CombatEvents`를 구독했으면 `OnDisable`에서 반드시 해제한다.**
  이건 `static`이라 `RestartGame()`이 씬을 다시 불러도 **초기화되지 않는다.**
  해제하지 않으면 파괴된 오브젝트가 목록에 남아서, 재시작 뒤 몬스터 한 마리를 잡을 때
  킬 카운트가 두 번 오르거나 `MissingReferenceException`이 뜬다.
  `OnStateChanged`, `OnScoreChanged`, `OnClipFinished`도 마찬가지로 해제한다.
- **UI 패널 전환 코드를 `_Game`에 만들지 않는다.** `UIManager`가 `OnStateChanged`를 구독해서 처리한다.
  게임 고유 연출이 필요하면 `ShowMessage()`를 쓴다.
- **점수 UI를 직접 갱신하지 않는다.** `AddScore()`만 부르면 `OnScoreChanged`로 화면이 따라온다.
- UI 외형(색·폰트·앵커·위치)은 인스펙터 작업이다. MCP로 `RectTransform`을 조작하려 들지 말고
  **사용자에게 인스펙터에서 직접 조정하라고 안내한다.**

## `_Core`가 실제로 도는지는 확인해 두었다

`_Game/`은 비어 있지만, 배포 전에 **동작하는 최소 세로 슬라이스를 한 번 만들어서 검증했다.**
아래는 전부 실제로 돌려서 확인한 것이다.

- Swordsman 애니메이션 전환 (Idle ↔ Walk, Attack·Hit이 이동 동작을 덮어씀)
- 공격 판정이 `IDamageable`을 통해 들어가고, 공격력 10 / 체력 50이면 **5대에 죽는다**
- `ObjectPool`로 몬스터 6마리 배치 → 처치 → 반납 → 재사용
- 처치 시 점수 상승, 재시작 뒤에도 **중복 집계 없음**
- 플레이어 사망 → `GameOver` 패널 자동 전환 → R로 재시작

**즉 `_Core`가 안 되는 것 같으면 대개 `_Core` 문제가 아니다.** 호출 방식(구독/해제, 풀 반납,
정의·상태 분리)이나 인스펙터 배선을 먼저 의심한다.

그때 만든 코드는 **`example` 브랜치**에 그대로 남아 있다. 막혔을 때 참고하면 된다.

```sh
git show example:Assets/_Game/PlayerController.cs
git checkout example      # 통째로 돌려보고 싶을 때
```

**이 브랜치는 Use this template에서 "Include all branches"를 켜야 따라온다.** 안 켰으면 `main`만 복사돼서
위 명령이 실패한다. 그때는 원본을 원격으로 추가해서 가져온다.

```sh
git remote add upstream https://github.com/LewisCho7/RPGBoilerplate.git
git fetch upstream example
git show upstream/example:Assets/_Game/PlayerController.cs
```

## 에셋

**이번 주차는 스프라이트가 제공된다.** `Assets/Sprites/`에 실제로 들어 있는 것은 아래가 전부다.

**플레이어 — `Character/`**

| 폴더 | 동작 |
| --- | --- |
| `Swordsman/` | Idle / Walk / Atk1 / Atk2 / Hit / March / Block / IdleAtk |
| `Archer/` | Idle / walk / atk / hit / March, `Arrow.png` |

**몬스터 — `Enemy/`**

| 폴더 | 종류 | 동작 |
| --- | --- | --- |
| `Normal/Monsters Creatures Fantasy/Sprites/` | Goblin, Mushroom, Skeleton, Flying eye | Idle / Run / Attack1 / Attack2 / Take Hit / Death |
| `Normal/Monsters Creatures Fantasy 2/Sprites/` | Slime, Bat, Rat, Mimic | idle / walk / attack / hurt / death |
| `Boss/Fire Worm/Sprites/Worm/` | 보스 1 | Idle / Walk / Attack / Get Hit / Death (+ Fire Ball) |
| `Boss/Mecha-stone Golem 0.1/` | 보스 2 | `Character_sheet.png` **한 장에 전부 뭉쳐 있다** — 동작별로 나뉘어 있지 않다 |

**그 외** — `Tiles/`(타일셋·배경), `16x16 Assorted RPG Icons/`(방어구·물약·무기·상자·책), `Skills/`(이펙트 다수)

규칙은 이렇다.

- **있는 것을 먼저 쓴다.** 필요한 게 있으면 `Assets/Sprites/` 안을 먼저 확인한다.
- **없는 것을 코드로 만들어내지 않는다.** 런타임 `Texture2D` 생성 같은 것으로 때우지 말고
  사용자에게 무엇을 쓸지 먼저 묻는다.
- **플레이어(Swordsman·Archer)에는 death 프레임이 없다.** 일반 몬스터와 보스에는 있다.
  플레이어가 죽는 연출이 필요하면 사용자에게 어떻게 할지 묻는다
  (`Hit`에서 멈추기, 알파를 낮추기, 색·각도를 바꿔 쓰러뜨리기, 별도 에셋을 넣기 등).
- **`Enemy/Normal/`의 두 팩에는 `.controller`/`.anim`이 같이 들어 있다.** 이건 유니티 표준 Animator용이다.
  `SpriteAnimator`와 **같은 오브젝트에 같이 붙이지 않는다** — 서로 스프라이트를 덮어써서 깜빡인다.
- **`Tiles/Assets.png`는 자동 슬라이스로 조각이 뒤섞여 있다.** 지형 타일로 바로 쓰기 어렵다.
  Tilemap을 제대로 쓰려면 인스펙터에서 다시 잘라야 하므로, 사용자에게 먼저 확인한다.

### 임포트 설정

**`Assets/Sprites/` 전체(928개)가 이미 Filter Mode `Point` / Compression `None` / Mip Maps 꺼짐이다.**
도트가 흐릿하게 보이는 원인은 여기서 이미 막아두었으니, 흐릿하다면 다른 곳을 봐야 한다.

**Pixels Per Unit과 피벗은 용도마다 달라서 일괄로 맞출 수 없다.** 캐릭터로 쓸 것만 손봐두었다 —
`Swordsman` / `Archer` / `Mushroom`이 그 대상이다.

- Pixels Per Unit **32** — 캐릭터가 약 1유닛 키가 된다. 100으로 두면 31px짜리 기사가 0.3유닛이라 먼지만 해진다.
- **피벗을 발바닥에 맞춰 두었다.** 이게 핵심이다.
  - 캐릭터 시트는 프레임마다 셀 높이가 다르다(28~38px). 피벗이 가운데면
    **걷는 동안 발이 위아래로 떠다닌다.** 그래서 `Bottom Center`로 맞췄다.
  - Mushroom은 150×150 격자 안에서 접지선이 y=49다. 그래서 커스텀 피벗 `(0.5, 0.327)`로 맞췄다.
  - 덕분에 `transform.position`이 곧 발 위치가 되어, 바닥 y좌표에 그대로 놓으면 된다.

**다른 몬스터를 새로 쓸 때는 PPU와 피벗을 같은 방식으로 맞춰야 한다.**
나머지(타일·아이콘·스킬 이펙트)는 PPU 100에 가운데 피벗 그대로다 — 캐릭터가 아니라서 그대로 두었다.

오디오는 들어 있지 않다. 필요하면 사용자에게 묻는다.

## 알려진 함정 (MCP for Unity)

- **`manage_gameobject create`의 `component_properties`가 조용히 무시된다.**
  `success: true`가 와도 스프라이트가 `null`이고 `BoxCollider2D` 크기가 `0.0001`로 남는다.
  화면에 아무것도 안 보이고 충돌도 안 되는 상태가 된다.
  → 생성 후 `manage_components set_property`로 다시 넣고, **읽어서 확인한다.**
- **`color`를 배열로 보내면 `success: true`와 함께 조용히 거부된다.** `{r, g, b, a}` 객체만 받는다.
  위치·크기(`Vector2`)는 배열이 되는데 색만 안 되는 비대칭이라 놓치기 쉽다.
- **만든 뒤에는 반드시 다시 읽어서 확인한다.** "만들었습니다"로 끝내지 않는다.
- **플레이 모드 전환 중에는 리소스 읽기가 stale 값을 반환한다.**
  `editor/state`의 `is_stale`을 보고 교차 확인한다.
- `manage_asset`은 `Folder`/`Material`/`PhysicsMaterial`만 생성한다.
  지원되지 않는 것은 해당 YAML을 직접 써서 임포트시킨다.
- `manage_asset action=rename`이 **성공하는데 실패로 응답한다.** 응답을 믿지 말고 디스크를 볼 것.
- `batch_execute`의 개별 `success` 필드가 일관되지 않는다. 배치 상한은 25개다.
- **`Sprite[]` 같은 배열 필드는 `manage_components`로 채우다 자주 어긋난다.**
  대신 **`execute_code`를 쓴다.** 에디터 안에서 진짜 C#이 도는 것이라 `AssetDatabase`로 스프라이트를 읽어
  배열에 직접 넣을 수 있고, 조용히 무시되는 일이 없다. `_Game`의 예제 씬도 전부 이 방법으로 만들었다.
  - `[SerializeField] private` 필드는 리플렉션으로 넣고 `EditorUtility.SetDirty()`를 부른다.
  - **넣은 뒤에는 `SerializedObject`로 다시 읽어서 확인한다.**
- **`execute_code`는 기본이 C# 6(CodeDom)이다.** Roslyn이 없으면 `using` 선언·로컬 함수·튜플·`out var`가
  전부 컴파일 에러가 난다. 타입은 전부 풀네임으로 쓰고, `Object`는 `UnityEngine.Object`로 명시한다
  (`object`와 모호해진다).
- **프레임 정렬을 이름순으로 하면 안 된다.** `Walk_10`이 `Walk_2`보다 앞에 온다.
  이름 끝의 숫자를 파싱해서 정렬한다.
- **플레이 모드에 들어가도 유니티 창에 포커스가 없으면 프레임이 돌지 않는다.**
  `Run In Background`가 꺼져 있으면 `Time.frameCount`가 `1`에서 멈춰 있다.
  게임은 멀쩡한데 "아무 일도 안 일어난다"로 보여서 **없는 버그를 쫓게 된다.**
  → 검증 전에 `Time.frameCount`와 `Time.time`을 먼저 찍어보고,
  멈춰 있으면 `Application.runInBackground = true`를 켠 뒤에 판단한다.
- **시간이 걸리는 것을 한 번 읽고 결론 내지 않는다.** 무적 시간·쿨타임·재배치 지연이 걸린 동작은
  호출 직후에 읽으면 당연히 안 변해 있다. 값이 변할 때까지 두고 여러 번 읽는다.

## 프로젝트 설정 (바꾸지 말 것)

- Active Input Handling: **Input System Package (New)**
- Product Name: **`RPG`** — `ScoreManager`의 `BestScore` 키에 쓰인다.
  이 값을 바꾸면 최고점수 저장 위치가 달라진다.
- 레이어: **8 = Ground, 9 = Player, 10 = Enemy.**
  공격 판정과 발밑 판정이 이 레이어를 `LayerMask`로 걸러 쓴다. 번호를 바꾸면 판정이 조용히 빗나간다.
