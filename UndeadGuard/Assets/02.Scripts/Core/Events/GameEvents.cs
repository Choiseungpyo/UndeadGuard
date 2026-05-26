using UnityEngine;

public class UnitSelectedEvent
{
    public UnitBase Unit;
}

public class UnitDeselectedEvent { }

public class UnitMovedEvent
{
    public UnitBase Unit;
    public Vector2Int From;
    public Vector2Int To;
}

public class UnitAttackedEvent
{
    public UnitBase Attacker;
    public UnitBase Target;
    public int Damage;
}

public class AttackModeRequestedEvent
{
    public UnitBase Unit;
    public IUnitAction Action;
}

public class AttackCompletedEvent
{
    public UnitBase Attacker;
    public UnitBase Target;
    public Vector2Int TargetPosition;
}

public class DamageTakenEvent
{
    public IDamageable Target;
    public MonoBehaviour TargetBehaviour;
    public int Damage;
    public int CurrentHp;
    public int MaxHp;
}

public class UnitDiedEvent
{
    public UnitBase Unit;
    public Vector2Int Position;
}

public class TurnChangedEvent
{
    public TurnType CurrentTurn;
}

public class TurnEndedEvent
{
    public TurnType EndedTurn;
    public TurnType NextTurn;
}

public class PlayerTurnStartedEvent { }

public class WaveStartedEvent
{
    public int WaveNumber;
    public int TotalWaves;
}

public class WaveClearedEvent
{
    public int WaveNumber;
    public int DarkEnergyReward;
}

public class WaveClearRewardFinishedEvent
{
    public int WaveNumber;
}

public class ActionCameraFlowFinishedEvent { }

public class EnemyTurnFinishedEvent { }

public class BattleWonEvent
{
    public int ClearedWaveNumber;
    public int TotalWaves;
}

public class VictoryEvent
{
    public int ClearedWaveNumber;
    public int TotalWaves;
}

public class TutorialStepChangedEvent
{
    public TutorialStep Step;
    public string Message;
    public UnitBase TargetUnit;
    public bool HasTargetGridPosition;
    public Vector2Int TargetGridPosition;
}

public class ResourceChangedEvent
{
    public int DarkEnergy;
    public int CommandPoint;
}

public class UnitRevivedEvent
{
    public UnitBase Unit;
    public Vector2Int Position;
}

public class EnemyDiedEvent
{
    public EnemyUnit Unit;
    public int DarkEnergyReward;
    public Vector3 WorldPosition;
}

public class EnemyKillRewardAbsorbedEvent
{
    public EnemyUnit Unit;
}

public class TileClickedEvent
{
    public Vector2Int GridPosition;
}

public class UnitMoveStartedEvent
{
    public UnitBase Unit;
}

public class UnitMoveFinishedEvent
{
    public UnitBase Unit;
}

public class ActionModeRequestedEvent
{
    public UnitBase Unit;
    public IUnitAction Action;
}

public class CoreHealthChangedEvent
{
    public int CurrentHp;
    public int MaxHp;
}

public class CoreDamagedEvent
{
    public CoreHealth Core;
    public int DamageAmount;
    public int CurrentHp;
    public int MaxHp;
    public string AttackerName;
}

public class CoreDestroyedEvent
{
    public CoreHealth Core;
    public MonoBehaviour CoreBehaviour;
    public Vector3 WorldPosition;
}

public class StageChangedEvent
{
    public StageType CurrentStage;
}

// Legacy compatibility with older scripts.
public class PhaseChangedEvent
{
    public StageType CurrentPhase;
}

public class WaveReadyEvent
{
    public int WaveNumber;
}

public class CommandUsedEvent
{
    public UnitBase Target;
    public int Cost;
}

public class ObjectSelectedEvent
{
    public StructureType ObjectType;
    public Vector2Int GridPosition;
}

public class UndeadRelocatedEvent
{
    public UnitBase Unit;
    public Vector2Int From;
    public Vector2Int To;
}

public class EndTurnRequestedEvent { }

public class GameProgressUpdatedEvent
{
    public string DayText;
    public string StageText;
    public string PhaseText;
    public string TurnText;

    public bool ShowPhaseText;
    public bool ShowTurnText;
    public bool IsPreparationStage;
}

public class RequestOpenTitleEvent
{
    public string SceneName;
}

public class RequestPlayIntroCutsceneEvent
{
    public string SceneName;
}

public class RequestOpenLobbyEvent
{
    public string SceneName;
}

public class RequestStartBattleEvent
{
    public string SceneName;
}

public class RequestRestartBattleEvent
{
    public string SceneName;
}

public class RequestOpenResultEvent
{
    public string SceneName;
    public GameOverResultData ResultData;
}

public class RequestOpenSettingsEvent
{
    public string SceneName;
}
