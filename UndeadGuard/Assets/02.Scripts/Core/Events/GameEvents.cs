using UnityEngine;

// 게임에서 사용하는 이벤트 클래스 모음
// 각 이벤트는 EventBus를 통해 발행 및 수신된다

// 유닛이 선택되었을 때 발행한다
public class UnitSelectedEvent
{
    public UnitBase Unit;
}

// 유닛 선택이 해제되었을 때 발행한다
public class UnitDeselectedEvent { }

// 유닛이 이동을 완료했을 때 발행한다
public class UnitMovedEvent
{
    public UnitBase Unit;
    public Vector2Int From;
    public Vector2Int To;
}

// 유닛이 공격을 수행했을 때 발행한다
public class UnitAttackedEvent
{
    public UnitBase Attacker;
    public UnitBase Target;
    public int Damage;
}

// 유닛이 사망했을 때 발행한다
public class UnitDiedEvent
{
    public UnitBase Unit;
    // 사망 당시의 그리드 위치를 기록한다 (부활 위치로 활용)
    public Vector2Int Position;
}

// 턴이 전환되었을 때 발행한다
public class TurnChangedEvent
{
    public TurnType CurrentTurn;
}

// 웨이브(Phase)가 시작될 때 발행한다
public class WaveStartedEvent
{
    // 현재 웨이브 번호 (1부터 시작)
    public int WaveNumber;
    // 이번 전투 단계 전체 웨이브 수
    public int TotalWaves;
}

// 웨이브의 모든 적이 처치되어 웨이브가 종료될 때 발행한다
public class WaveClearedEvent
{
    public int WaveNumber;
}

// 자원 수치가 변경될 때 발행한다
public class ResourceChangedEvent
{
    public int DarkEnergy;
    public int CommandPoint;
}

// 언데드 유닛이 부활했을 때 발행한다
public class UnitRevivedEvent
{
    public UnitBase Unit;
    public Vector2Int Position;
}

// 적 유닛이 사망했을 때 발행한다. 암흑 에너지 보상 정보를 포함한다
public class EnemyDiedEvent
{
    public EnemyUnit Unit;
    public int DarkEnergyReward;
}

// 플레이어가 타일 위치를 클릭했을 때 발행한다
public class TileClickedEvent
{
    public Vector2Int GridPosition;
}

// 유닛이 순차 이동을 시작했을 때 발행한다. 이동 중 입력을 차단하는 데 사용한다
public class UnitMoveStartedEvent
{
    public UnitBase Unit;
}

// 유닛이 순차 이동을 완료했을 때 발행한다
public class UnitMoveFinishedEvent
{
    public UnitBase Unit;
}

// 플레이어가 적 유닛을 공격하도록 요청했을 때 발행한다
public class AttackRequestedEvent
{
    public UnitBase Target;
}

// 코어 체력이 변경될 때 발행한다
public class CoreHealthChangedEvent
{
    public int CurrentHp;
    public int MaxHp;
}

// 코어가 파괴되어 게임 오버가 될 때 발행한다
public class CoreDestroyedEvent { }

// 게임 페이즈가 전환될 때 발행한다 (배치 단계 ↔ 전투 단계)
public class PhaseChangedEvent
{
    public PhaseType CurrentPhase;
}

// 웨이브가 시작될 준비가 되었을 때 발행한다
public class WaveReadyEvent
{
    public int WaveNumber;
}

// 사령 명령이 유닛에게 사용되었을 때 발행한다
public class CommandUsedEvent
{
    public UnitBase Target;
    public int Cost;
}

// 준비 단계에서 게임 오브젝트(코어, 벽 등)가 선택되었을 때 발행한다
public class ObjectSelectedEvent
{
    public StructureType ObjectType;
    public Vector2Int GridPosition;
}

// 준비 단계에서 언데드 유닛의 위치가 변경되었을 때 발행한다
public class UndeadRelocatedEvent
{
    public UnitBase Unit;
    public Vector2Int From;
    public Vector2Int To;
}

// 플레이어가 턴 종료 버튼을 눌렀을 때 발행한다
public class EndTurnRequestedEvent { }

// 게임 진행 정보가 갱신될 때 발행한다
// UI는 이 이벤트를 구독해 표시 데이터를 갱신한다
public class GameProgressUpdatedEvent
{
    // 표시 텍스트
    public string DayText;       // 예: "1일차"
    public string StageText;     // 예: "배치 단계" / "전투 단계"
    public string PhaseText;     // 예: "Phase 2 / 4" (전투 단계에서만 유효)
    public string TurnText;      // 예: "플레이어 턴 3" / "적 턴" (전투 단계에서만 유효)

    // 표시 여부 플래그
    public bool ShowPhaseText;   // 전투 단계이며 Phase가 시작된 경우에만 true
    public bool ShowTurnText;    // 전투 단계이며 턴이 진행 중인 경우에만 true
    public bool IsPreparationStage; // 배치 단계 여부
}
