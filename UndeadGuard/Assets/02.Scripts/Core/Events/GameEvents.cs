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

public class WaveStartedEvent
{
    public int WaveNumber;
    public int TotalWaves;
}

public class WaveClearedEvent
{
    public int WaveNumber;
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

public class AttackRequestedEvent
{
    public UnitBase Target;
}

public class AttackModeRequestedEvent { }

public class SkillModeRequestedEvent { }

public class CoreHealthChangedEvent
{
    public int CurrentHp;
    public int MaxHp;
}

public class CoreDestroyedEvent { }

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



